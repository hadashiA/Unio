using System;
using System.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unio
{
    public class NativeArrayBufferWriter<T> : IBufferWriter<T>, IDisposable where T : unmanaged
    {
        public int WrittenCount => offset;
        public int Capacity => buffer.Length;
        public int FreeCapacity => buffer.Length - offset;

        public unsafe NativeArray<T> WrittenBuffer
        {
            get
            {
                var ptr = buffer.GetUnsafeReadOnlyPtr();
                var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, offset, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
                return result;
            }
        }

        const int DefaultInitialBufferSize = 256;

        // Copy of Array.MaxLength.
        // Probably not equal to the NativeArray limit, but used as a reference value.
        const int ArrayMaxLength = 0x7FFFFFC7;

        NativeArray<T> buffer;
        int offset;
        readonly Allocator allocator;
        readonly NativeArrayOptions options;

        public NativeArrayBufferWriter(int initialBufferLength, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            buffer = new NativeArray<T>(initialBufferLength, allocator, options);
            this.allocator = allocator;
            this.options = options;
        }

        public void Advance(int count)
        {
            var newIndex = offset + count;
            if (newIndex > buffer.Length)
            {
                ThrowOutOfRange(newIndex);
            }
            offset = newIndex;
        }

        public unsafe Memory<T> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);

            var ptr = (T*)buffer.GetUnsafeReadOnlyPtr();
            ptr += sizeof(T) * offset;
            // TODO: Reduce allocation
            var memoryManager = new NativeArrayMemoryManager<T>(ptr, sizeHint);
            return memoryManager.Memory;
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return buffer.AsSpan().Slice(offset, sizeHint);
        }

        public void ResetWrittenCount()
        {
            offset = 0;
        }

        public void Clear()
        {
            buffer.AsSpan().Clear();
            offset = 0;
        }

        public void Dispose()
        {
            buffer.Dispose();
        }

        void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (sizeHint > FreeCapacity)
            {
                var currentLength = buffer.Length;

                // Attempt to grow by the larger of the sizeHint and double the current size.
                var growBy = Math.Max(sizeHint, currentLength);
                if (currentLength == 0)
                {
                    growBy = Math.Max(growBy, DefaultInitialBufferSize);
                }

                var newSize = currentLength + growBy;
                if (newSize > ArrayMaxLength)
                {
                    ThrowOutOfRange(newSize);
                }
                Expand(newSize);
            }
        }

        unsafe void Expand(int newLength)
        {
            var newArray = new NativeArray<T>(newLength, allocator, options);
            if (offset > 0)
            {
                var bytesToCopy = offset * sizeof(T);
                UnsafeUtility.MemCpy(buffer.GetUnsafePtr(), newArray.GetUnsafePtr(), bytesToCopy);
            }
            buffer.Dispose();
            buffer = newArray;
        }

        void ThrowOutOfRange(int needed)
        {
            throw new NotSupportedException($"Cannot advance past the end of the buffer. {needed}");
        }
    }
}