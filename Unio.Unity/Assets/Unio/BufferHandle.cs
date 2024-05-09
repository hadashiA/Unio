using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unio
{
    public unsafe class BufferHandle : SafeHandle
    {
        public readonly int Length;

        public override bool IsInvalid => handle == IntPtr.Zero;

        public BufferHandle(byte* ptr, int length) : base((IntPtr)ptr, true)
        {
            Length = length;
        }

        public ReadOnlySpan<byte> AsSpan() => new((byte*)handle, Length);

        public NativeArray<byte> AsNativeArray()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>((byte*)handle, Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return array;
        }

        protected override bool ReleaseHandle()
        {
            UnsafeUtility.Free((byte*)handle, Allocator.None);
            return true;
        }
    }
}
