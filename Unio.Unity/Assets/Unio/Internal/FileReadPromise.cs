#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;

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
        readonly TaskCompletionSource<BufferHandle>? completionSource;
        readonly CancellationToken cancellation;

        NativeArray<ReadCommand> readCommands;
        ReadHandle readHandle;

        public FileReadPromise(
            string filePath,
            TaskCompletionSource<BufferHandle>? completionSource = null,
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
                    Buffer = (ReadCommand*)UnsafeUtility.Malloc(sizeof(ReadCommand) * 1, UnsafeUtility.AlignOf<ReadCommand>(), Allocator.Persistent),
                };

                readHandle = AsyncReadManager.Read(filePath, (ReadCommand*)readCommands.GetUnsafePtr(), 1);
            }
            catch (Exception ex)
            {
                completionSource?.TrySetException(ex);
            }
        }

        public BufferHandle GetResult()
        {
            var readCommand = readCommands[0];
            return new BufferHandle((byte*)readCommand.Buffer, (int)readCommand.Size);
        }

        public void WaitForComplete()
        {
            try
            {
                readHandle.JobHandle.Complete();
                if (completionSource != null)
                {
                    var result = GetResult();
                    completionSource.TrySetResult(result);
                }
            }
            catch (Exception ex)
            {
                completionSource?.TrySetException(ex);
                ReleaseBuffer();
            }
            finally
            {
                ReleaseHandle();
            }
        }

        public bool MoveNext()
        {
            if (cancellation.IsCancellationRequested)
            {
                try
                {
                    readHandle.Cancel();
                    completionSource?.TrySetCanceled(cancellation);
                }
                finally
                {
                    ReleaseHandle();
                    ReleaseBuffer();
                }
                return false;
            }

            switch (readHandle.Status)
            {
                case ReadStatus.Complete:
                {
                    ReleaseHandle();
                    var result = GetResult();
                    completionSource?.TrySetResult(result);
                    break;
                }
                case ReadStatus.Failed:
                case ReadStatus.Truncated:
                {
                    ReleaseHandle();
                    ReleaseBuffer();
                    completionSource?.TrySetException(new UnioIOException(filePath, readHandle.Status));
                    break;
                }
                case ReadStatus.Canceled:
                {
                    ReleaseHandle();
                    ReleaseBuffer();
                    completionSource?.TrySetCanceled();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        void ReleaseHandle()
        {
            readHandle.Dispose();
            readCommands.Dispose();
        }

        void ReleaseBuffer()
        {
            UnsafeUtility.Free(readCommands[0].Buffer, Allocator.Persistent);
        }
    }
}
