using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;

namespace Unio
{
    public struct ReadResult : IDisposable
    {
        public NativeArray<byte> Content;
        public bool FromUnioNative;

        public ReadResult()
        {
        }

        public void Dispose()
        {
            if (FromUnioNative)
            {
                // NativeMethods.free_
                throw new NotImplementedException();
            }
            else
            {

            }
            // TODO release managed resources here
        }
    }

    public static class NativeIO
    {
        public static Task<NativeArray<byte>> ReadAllBytesOnMainThreadAsync(string path, CancellationToken cancellation = default)
        {
        }

        public static Task<FileHandle> ReadAllBytesAsync(string path, CancellationToken cancellation = default)
        {
            return Task.Run()
        }
    }
}