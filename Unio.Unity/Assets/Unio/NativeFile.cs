using System;
using System.Threading;
using System.Threading.Tasks;
using Unio.Internal;

namespace Unio
{
    public enum WaitStrategy
    {
        ThreadPool,
        PlayerLoop,
    }

    public static class NativeFile
    {
        public static BufferHandle ReadAllBytes(string filePath)
        {
            var promise = new FileReadPromise(filePath);
            promise.WaitForComplete();
            return promise.GetResult();
        }

        public static Task<BufferHandle> ReadAllBytesAsync(string filePath, WaitStrategy strategy, CancellationToken cancellation = default)
        {
            var completionSource = new TaskCompletionSource<BufferHandle>();
            var promise = new FileReadPromise(filePath, completionSource, cancellation);

            switch (strategy)
            {
                case WaitStrategy.ThreadPool:
                    ThreadPool.UnsafeQueueUserWorkItem(x =>
                    {
                        ((FileReadPromise)x).WaitForComplete();
                    }, promise);
                    break;
                case WaitStrategy.PlayerLoop:
                {
                    PlayerLoopHelper.EnsureInitialized();
                    PlayerLoopHelper.Dispatch(PlayerLoopTiming.Update, promise);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
            }
            return completionSource.Task;
        }
    }
}
