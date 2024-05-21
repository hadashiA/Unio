using System;
using System.IO;
using System.Text;
using System.Threading;
using Unio.Internal;
using Unity.Collections;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_2023_1_OR_NEWER
using UnitTaskType = UnityEngine.Awaitable;
using BytesTaskType = UnityEngine.Awaitable<Unity.Collections.NativeArray<byte>>;
using StringTaskType = UnityEngine.Awaitable<string>;
using BytesCompletionSource = UnityEngine.AwaitableCompletionSource<Unity.Collections.NativeArray<byte>>;
using UnitCompletionSource = UnityEngine.AwaitableCompletionSource;
#else
using UnitTaskType = System.Threading.Tasks.Task;
using BytesTaskType = System.Threading.Tasks.Task<Unity.Collections.NativeArray<byte>>;
using StringTaskType = System.Threading.Tasks.Task<string>;
using BytesCompletionSource = System.Threading.Tasks.TaskCompletionSource<Unity.Collections.NativeArray<byte>>;
using UnitCompletionSource = System.Threading.Tasks.TaskCompletionSource<bool>;
#endif

namespace Unio
{
    public enum SynchronizationStrategy
    {
        BlockOnThreadPool,
        PlayerLoop,
    }

    public static class NativeFile
    {
        public static NativeArray<byte> ReadAllBytes(string filePath)
        {
            var promise = new FileReadPromise(filePath);
            promise.WaitForComplete();
            return promise.GetResult();
        }

        public static string ReadAllText(string filePath)
        {
            return ReadAllText(filePath, Encoding.UTF8);
        }

        public static string ReadAllText(string filePath, Encoding encoding)
        {
            var promise = new FileReadPromise(filePath);
            promise.WaitForComplete();
            using var bytes = promise.GetResult();
            return encoding.GetString(bytes);
        }

        public static BytesTaskType ReadAllBytesAsync(string filePath, SynchronizationStrategy strategy = default, CancellationToken cancellation = default)
        {
            var completionSource = new BytesCompletionSource();
            var promise = new FileReadPromise(filePath, completionSource, cancellation);

            switch (strategy)
            {
                case SynchronizationStrategy.BlockOnThreadPool:
                    ThreadPool.UnsafeQueueUserWorkItem(x =>
                    {
                        ((FileReadPromise)x).WaitForComplete();
                    }, promise);
                    break;
                case SynchronizationStrategy.PlayerLoop:
                {
                    PlayerLoopHelper.EnsureInitialized();
                    PlayerLoopHelper.Dispatch(PlayerLoopTiming.Update, promise);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
            }

#if UNITY_2023_1_OR_NEWER
            return completionSource.Awaitable;
#else
            return completionSource.Task;
#endif
        }

        public static StringTaskType ReadAllTextAsync(string filePath, SynchronizationStrategy strategy = default, CancellationToken cancellation = default)
        {
            return ReadAllTextAsync(filePath, Encoding.UTF8, strategy, cancellation);
        }

        public static async StringTaskType ReadAllTextAsync(string filePath, Encoding encoding, SynchronizationStrategy strategy = default, CancellationToken cancellation = default)
        {
            using var bytes = await ReadAllBytesAsync(filePath, strategy, cancellation);
            return encoding.GetString(bytes);
        }

        public static void WriteAllBytes(string filePath, NativeArray<byte> nativeArray)
        {
            WriteAllBytes(filePath, nativeArray.AsSpan());
        }

        public static void WriteAllBytes(string filePath, ReadOnlySpan<byte> span)
        {
            // sync and immediately
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 1, false);
            fs.Write(span);
            fs.Flush();
        }

        public static UnitTaskType WriteAllBytesAsync(string filePath, NativeArray<byte> bytes)
        {
            var completionSource = new UnitCompletionSource();
            ThreadPool.UnsafeQueueUserWorkItem(x =>
            {
                var tuple = ((string, NativeArray<byte>, UnitCompletionSource))x;
                try
                {
                    WriteAllBytes(tuple.Item1, tuple.Item2);
#if UNITY_2023_1_OR_NEWER
                     tuple.Item3.TrySetResult();
#else
                    tuple.Item3.TrySetResult(true);
#endif
                }
                catch (Exception ex)
                {
                    tuple.Item3.TrySetException(ex);
                }
            }, (filePath, bytes, completionSource));
#if UNITY_2023_1_OR_NEWER
            return completionSource.Awaitable;
#else
            return completionSource.Task;
#endif
        }
    }
}
