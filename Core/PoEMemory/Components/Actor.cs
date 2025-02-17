using System.Collections.Generic;
using Exile.PoEMemory.MemoryObjects;
using GameOffsets;
using Shared.Interfaces;
using Shared.Enums;
using SharpDX;

namespace PoEMemory.Components
{
    public class Actor : Component
    {
        private FrameCache<ActorComponentOffsets> cacheValue;
        private ActorComponentOffsets Struct => cacheValue.Value;
        public Actor() => cacheValue = new FrameCache<ActorComponentOffsets>(() => M.Read<ActorComponentOffsets>(Address));

        /// <summary>
        ///     Standing still = 2048 =bit 11 set
        ///     running = 2178 = bit 11 & 7
        ///     Maybe Bit-field : Bit 7 set = running
        /// </summary>
        public short ActionId => Address != 0 ? Struct.ActionId : (short) 0;
        public ActionFlags Action => Address != 0 ? (ActionFlags) Struct.ActionId : ActionFlags.None;

        public bool isMoving => (Action & ActionFlags.Moving) > 0;
        public bool isAttacking => (Action & ActionFlags.UsingAbility) > 0;

        public int AnimationId => Address != 0 ? Struct.AnimationId : 0;
        public AnimationE Animation => Address != 0 ? (AnimationE) Struct.AnimationId : AnimationE.Idle;

        /*public bool HasMinion(Entity entity) {
            if (Address == 0) return false;

            var num = Struct.HasMinionArray.First;
            var num2 = Struct.HasMinionArray.Last;
            for (var i = num; i < num2; i += 8)
            {
                var num3 = M.Read<int>(i);
                if (num3 == (int) (entity.Id >> 32)) return true;
            }

            return false;
        }*/

        //public float TimeSinseLastMove => -M.Read<float>(Address + 0x110);
        //public float TimeSinseLastAction => -M.Read<float>(Address + 0x114);

        /// <summary>
        /// Currently performed action information.
        /// WARNING: This memory location changes a lot,
        /// put try catch if you are accessing this variable and the fields in it.
        /// </summary>
        public ActionWrapper CurrentAction => Struct.ActionPtr > 0 ? GetObject<ActionWrapper>(Struct.ActionPtr) : null;

        // e.g minions, mines
        public long DeployedObjectsCount => Struct.DeployedObjectArray.Size / 8;

        public List<DeployedObject> DeployedObjects
        {
            get
            {
                var result = new List<DeployedObject>();
                for (var addr = Struct.DeployedObjectArray.First; addr < Struct.DeployedObjectArray.Last; addr += 8)
                {
                    var objectId = M.Read<uint>(addr);
                    var objectKey = M.Read<ushort>(addr + 4); //in list of entities
                    result.Add(new DeployedObject(objectId, objectKey));
                }

                return result;
            }
        }

        public List<ActorSkill> ActorSkills
        {
            get
            {
                var skillsStartPointer = Struct.ActorSkillsArray.First;
                var skillsEndPointer = Struct.ActorSkillsArray.Last;
                skillsStartPointer += 8; //Don't ask me why. Just skipping first one
                if ((skillsEndPointer - skillsStartPointer) / 16 > 50)
                    return new List<ActorSkill>();

                var result = new List<ActorSkill>();
                for (var addr = skillsStartPointer;
                    addr < skillsEndPointer;
                    addr += 16) //16 because we are reading each second pointer (pointer vectors)
                    result.Add(ReadObject<ActorSkill>(addr));
                return result;
            }
        }

        /*public List<ActorVaalSkill> ActorVaalSkills
        {
            get
            {
                const int ACTOR_VAAL_SKILLS_SIZE = 0x20;
                var skillsStartPointer = Struct.ActorVaalSkills.First;
                var skillsEndPointer = Struct.ActorVaalSkills.Last;

                var stuckCounter = 0;
                var result = new List<ActorVaalSkill>();
                for (var addr = skillsStartPointer; addr < skillsEndPointer; addr += ACTOR_VAAL_SKILLS_SIZE)
                {
                    result.Add(ReadObject<ActorVaalSkill>(addr));
                    if (stuckCounter++ > 50)
                        return new List<ActorVaalSkill>();
                }

                return result;
            }
        }*/

        public class ActionWrapper : RemoteMemoryObject
        {
            private FrameCache<ActionWrapperOffsets> cacheValue;
            private ActionWrapperOffsets Struct => cacheValue.Value;
            public ActionWrapper() => cacheValue = new FrameCache<ActionWrapperOffsets>(() => M.Read<ActionWrapperOffsets>(Address));

            public float DestinationX => Struct.Destination.X;
            public float DestinationY => Struct.Destination.Y;

            public Vector2 Destination => Struct.Destination;

            public Entity Target => ReadObject<Entity>(Struct.Target);
            public Vector2 CastDestination => new Vector2(DestinationX, DestinationY);

            public ActorSkill Skill => ReadObject<ActorSkill>(Struct.Skill);
        }
    }
}
