using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Exile.PoEMemory.MemoryObjects;
using GameOffsets;
using GameOffsets.Native;
using Shared.Interfaces;
using Shared.Enums;
using SharpDX;


namespace PoEMemory
{
    public class ServerInventory : RemoteMemoryObject
    {
        private CachedValue<ServerInventoryOffsets> cachedValue;
        private ServerInventoryOffsets Struct => cachedValue.Value;
        public ServerInventory() => cachedValue = new FrameCache<ServerInventoryOffsets>(() => M.Read<ServerInventoryOffsets>(Address));
        public InventoryTypeE InventType => (InventoryTypeE) M.Read<byte>(Address);
        public InventorySlotE InventSlot => (InventorySlotE) M.Read<byte>(Address + 0x1);
        public int Columns => M.Read<int>(Address + 0xc);
        public int Rows => M.Read<int>(Address + 0x10);
        public bool IsRequested => M.Read<byte>(Address + 0x4) == 1;

        public long CountItems => M.Read<long>(Address + 0x50);
        public int TotalItemsCounts => M.Read<int>(Address + 0x50);

        public int ServerRequestCounter => M.Read<int>(Address + 0xA8);

        //   public IEnumerable<InventSlotItem> InventorySlotItems => ReadHashMap(Address + 0x48).Values.ToList();
        public IList<InventSlotItem> InventorySlotItems => ReadHashMap(Struct.InventorySlotItemsPtr).Values.ToList();

        public long Hash => M.Read<long>(Struct.InventorySlotItemsPtr + 0x10);

        //  public IList<Entity> Items => ReadHashMap(Address + 0x48).Values.Select(x => x.Item).ToList();
        public IList<Entity> Items => ReadHashMap(Struct.InventorySlotItemsPtr).Values.Select(x => x.Item).ToList();
        public InventSlotItem this[int x, int y]
        {
            get
            {
                long invAddr = M.Read<long>(Address + 0x30);
                y = y * Columns;
                long itmAddr = M.Read<long>(invAddr + ((x + y) * 8));
                if (itmAddr <= 0)
                    return null;
                return GetObject<InventSlotItem>(itmAddr);
            }
        }
        public Dictionary<int, InventSlotItem> ReadHashMap(long pointer) {
            var result = new Dictionary<int, InventSlotItem>();

            var stack = new Stack<HashNode>();
            var startNode = GetObject<HashNode>(pointer); // ReadObject<HashNode>(pointer);
            var item = startNode.Root;
            stack.Push(item);

            while (stack.Count != 0)
            {
                var node = stack.Pop();
                if (!node.IsNull)
                    result[node.Key] = node.Value1;

                var prev = node.Previous;
                if (!prev.IsNull)
                    stack.Push(prev);

                var next = node.Next;
                if (!next.IsNull)
                    stack.Push(next);
            }

            return result;
        }


        public class HashNode : RemoteMemoryObject
        {
            private FrameCache<NativeHashNode> frameCache;
            private NativeHashNode NativeHashNode => frameCache.Value;
            public HashNode() => frameCache = new FrameCache<NativeHashNode>(() => M.Read<NativeHashNode>(Address));

            public HashNode Previous => GetObject<HashNode>(NativeHashNode.Previous);
            public HashNode Root => GetObject<HashNode>(NativeHashNode.Root);

            public HashNode Next => GetObject<HashNode>(NativeHashNode.Next);

            //public readonly byte Unknown;
            public bool IsNull => NativeHashNode.IsNull != 0;

            //private readonly byte byte_0;
            //private readonly byte byte_1;
            public int Key => NativeHashNode.Key;

            //public readonly int Useless;
            public InventSlotItem Value1 => GetObject<InventSlotItem>(NativeHashNode.Value1);
            //public readonly long Value2;
        }


        public class InventSlotItem : RemoteMemoryObject
        {
            private struct ItemMinMaxLocation
            {
                private int XMin;
                private int YMin;
                private int XMax;
                private int YMax;

                public RectangleF GetItemRect(float invStartX, float invStartY, float cellsize)
                {
                    return new RectangleF(
                        invStartX + cellsize * XMin,
                        invStartY + cellsize * YMin,
                        (XMax - XMin) * cellsize,
                        (YMax - YMin) * cellsize);
                }
                public Vector2 InventoryPosition => new Vector2(XMin, YMin);
                public override string ToString()
                {
                    return $"({XMin}, {YMin}, {XMax}, {YMax})";
                }
            };
            public Vector2 InventoryPosition => Location.InventoryPosition;
            private ItemMinMaxLocation Location => M.Read<ItemMinMaxLocation>(Address + 0x08, 0x0C * sizeof(int));
            public Entity Item => ReadObject<Entity>(Address);
            public int PosX => M.Read<int>(Address + 0x8);
            public int PosY => M.Read<int>(Address + 0xc);
            public int SizeX => M.Read<int>(Address + 0x10) - PosX;
            public int SizeY => M.Read<int>(Address + 0x14) - PosY;
            private RectangleF ClientRect => GetClientRect();

            public RectangleF GetClientRect()
            {
                var playerInventElement = TheGame.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
                var inventClientRect = playerInventElement.GetClientRect();
                var cellSize = inventClientRect.Width / 12;
                return Location.GetItemRect(inventClientRect.X, inventClientRect.Y, cellSize); 
            }

            public override string ToString() =>$"InventSlotItem: {Location}, Item: {Item}";
        }
    }
}