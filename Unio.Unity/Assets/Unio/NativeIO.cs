using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;

namespace Unio
{
    public enum AllocationType
    {
        UnityUnsafeUtility,
        UnioNative,
    }

    public readonly unsafe struct BufferHandle : IDisposable
    {
        public readonly AllocationType AllocationType;
        public readonly int Length;
        readonly byte* ptr;

        public BufferHandle(byte* ptr, int length, AllocationType allocationType)
        {
            this.ptr = ptr;
            Length = length;
            AllocationType = allocationType;
        }

        public ReadOnlySpan<byte> AsSpan() => new(ptr, Length);

        public NativeArray<byte> AsNativeArray()
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(ptr, Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return array;
        }

        public void Dispose()
        {
            switch (AllocationType)
            {
                case AllocationType.UnityUnsafeUtility:
                    UnsafeUtility.Free(ptr, Allocator.None);
                    break;
                case AllocationType.UnioNative:
                    // NativeMethods.unio_byte_buffer_delete(Content);
                    throw new NotImplementedException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static unsafe class NativeIO
    {
        [ThreadStatic]
        static ReadCommand* readCommandBufferOne;

        public static BufferHandle ReadFile(string filePath)
        {
            // var fileInfo = new FileInfo(filePath);
            // var size = fileInfo.Length;

            FileInfoResult fileInfoResult;
            var fileInfoHandle = AsyncReadManager.GetFileInfo(filePath, &fileInfoResult);
            fileInfoHandle.JobHandle.Complete();

            var size = (int)fileInfoResult.FileSize;

            var readCommand = new ReadCommand
            {
                Buffer = (byte*)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent),
                Offset = 0,
                Size = size
            };

            if (readCommandBufferOne == null)
            {
                readCommandBufferOne = (ReadCommand*)UnsafeUtility.Malloc(
                    sizeof(ReadCommand) * 1,
                    UnsafeUtility.AlignOf<ReadCommand>(),
                    Allocator.Persistent);
            }
            readCommandBufferOne[0] = readCommand;

            var readHandle = AsyncReadManager.Read(filePath, readCommandBufferOne, 1);
            readHandle.JobHandle.Complete();

            return new BufferHandle((byte*)readCommand.Buffer, size, AllocationType.UnityUnsafeUtility);
        }
    }
}
