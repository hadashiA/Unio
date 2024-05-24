#nullable enable
using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;

#if UNITY_2023_1_OR_NEWER
using BytesCompletionSource = UnityEngine.AwaitableCompletionSource<Unity.Collections.NativeArray<byte>>;
#else
using BytesCompletionSource = System.Threading.Tasks.TaskCompletionSource<Unity.Collections.NativeArray<byte>>;
#endif

namespace Unio.Internal
{
    public class UnioIOException : Exception
    {
        public UnioIOException(string filePath, ReadStatus failedStatus) : base(failedStatus switch
        {
            ReadStatus.Failed => $"Read operation failed. {filePath}",
            ReadStatus.Truncated => $"Read operation was truncated. {filePath}",
            ReadStatus.Canceled => $"Read operation was canceled. {filePath}",
            _ => $"Read operation failed (unknown status). {filePath}"
        }) { }
    }

    public unsafe class FileReadPromise : IPlayerLoopItem
    {
        readonly string filePath;
        readonly BytesCompletionSource? completionSource;
        readonly CancellationToken cancellation;

        NativeArray<ReadCommand> readCommands;
        ReadHandle readHandle;
        ReadStatus status;

        public FileReadPromise(
            string filePath,
            BytesCompletionSource? completionSource = null,
            CancellationToken cancellationToken = default)
        {
            this.filePath = filePath;
            this.completionSource = completionSource;
            this.cancellation = cancellationToken;

            try
            {
                FileInfoResult fileInfoResult;
                var fileInfoHandle = AsyncReadManager.GetFileInfo(filePath, &fileInfoResult);
                fileInfoHandle.JobHandle.Complete();

                readCommands = new NativeArray<ReadCommand>(1, Allocator.Persistent);
                readCommands[0] = new ReadCommand
                {
                    Offset = 0,
                    Size = fileInfoResult.FileSize,
                    Buffer = (ReadCommand*)UnsafeUtility.Malloc(fileInfoResult.FileSize, UnsafeUtility.AlignOf<ReadCommand>(), Allocator.Persistent),
                };

                readHandle = AsyncReadManager.Read(filePath, (ReadCommand*)readCommands.GetUnsafePtr(), 1);
            }
            catch (Exception ex)
            {
                ReleaseHandle();
                ReleaseBuffer();
                completionSource?.TrySetException(ex);
            }
        }

        public NativeArray<byte> GetResult()
        {
            try
            {
                var readCommand = readCommands[0];
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                    (byte*)readCommand.Buffer,
                    (int)readCommand.Size,
                    Allocator.Persistent);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
                return array;
            }
            finally
            {
                readCommands.Dispose();
            }
        }

        public void WaitForComplete()
        {
            try
            {
                readHandle.JobHandle.Complete();
            }
            catch (Exception ex)
            {
                ReleaseHandle();
                ReleaseBuffer();
                if (completionSource != null)
                {
                    completionSource?.TrySetException(ex);
                }
                else
                {
                    throw;
                }
            }

            if (status != ReadStatus.Complete)
            {
                throw new UnioIOException(filePath, status);
            }
        }

        public bool MoveNext()
        {
            status = readHandle.Status;

            if (cancellation.IsCancellationRequested)
            {
                try
                {
                    if (readHandle.IsValid())
                    {
                        readHandle.Cancel();
                    }
                    completionSource?.TrySetCanceled();
                    return false;
                }
                finally
                {
                    ReleaseHandle();
                    ReleaseBuffer();
                }
            }

            switch (readHandle.Status)
            {
                case ReadStatus.Complete:
                {
                    ReleaseHandle();
                    if (completionSource != null)
                    {
                        var result = GetResult();
                        completionSource.TrySetResult(result);
                    }
                    return false;
                }
                case ReadStatus.Failed:
                case ReadStatus.Truncated:
                {
                    ReleaseHandle();
                    ReleaseBuffer();
                    completionSource?.TrySetException(new UnioIOException(filePath, readHandle.Status));
                    return false;
                }
                case ReadStatus.Canceled:
                {
                    ReleaseHandle();
                    ReleaseBuffer();
                    completionSource?.TrySetCanceled();
                    return false;
                }
                case ReadStatus.InProgress:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void ReleaseHandle()
        {
            if (readHandle.IsValid())
            {
                readHandle.Dispose();
            }
        }

        void ReleaseBuffer()
        {
            UnsafeUtility.Free(readCommands[0].Buffer, Allocator.Persistent);
        }
    }
}