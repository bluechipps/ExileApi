using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Shared.SomeMagic
{
    public static class MarshalType<T> where T : struct
    {
        public static Type Type { get; private set; }
        public static TypeCode TypeCode { get; private set; }
        public static int Size { get; private set; }
        public static bool IsIntPtr { get; private set; }
        public static bool HasUnmanagedTypes { get; private set; }

        internal unsafe delegate void* GetPointerDelegate(ref T generic);

        internal static readonly GetPointerDelegate GetPointer;

        static MarshalType() {
            TypeCode = Type.GetTypeCode(typeof(T));

            // Bools = 1 char.
            if (typeof(T) == typeof(bool))
            {
                Size = 1;
                Type = typeof(T);
            }
            else if (typeof(T).IsEnum)
            {
                var underlying = typeof(T).GetEnumUnderlyingType();
                Size = Marshal.SizeOf(underlying);
                Type = underlying;
                TypeCode = Type.GetTypeCode(underlying);
            }
            else
            {
                Size = Marshal.SizeOf(typeof(T));
                Type = typeof(T);
            }

            IsIntPtr = Type == typeof(IntPtr);

            HasUnmanagedTypes = Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                    .Any(m => m.GetCustomAttributes(typeof(MarshalAsAttribute), true).Any());

            var method = new DynamicMethod($"GetPinnedPointer<{Type.FullName.Replace(".", "<>")}>", typeof(void*),
                                           new[] {Type.MakeByRefType()}, typeof(MarshalType<>).Module);

            var gen = method.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Conv_U);
            gen.Emit(OpCodes.Ret);
            GetPointer = (GetPointerDelegate) method.CreateDelegate(typeof(GetPointerDelegate));
        }
    }
}