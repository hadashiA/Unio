// using System;
// using System.IO;
// using System.Threading;
// using System.Threading.Tasks;
// using Unity.Collections;
// using Unity.IO.LowLevel.Unsafe;
//
// namespace Unio.Internal
// {
//     public unsafe class ReadPromise : IPlayerLoopItem
//     {
//         readonly string filePath;
//         readonly TaskCompletionSource<BufferHandle> source;
//         readonly CancellationToken cancellation;
//
//         ReadHandle readHandle;
//         int fileSize = -1;
//
//         public ReadPromise(string filePath, TaskCompletionSource<BufferHandle> source, CancellationToken cancellationToken)
//         {
//             this.filePath = filePath;
//             this.source = source;
//             this.cancellation = cancellationToken;
//
//             try
//             {
//                 // TODO: AsyncReadManager.ReadFileInfo
//                 var fileInfo = new FileInfo(this.filePath);
//                 var cmds = new NativeArray<ReadCommand>(1, Allocator.Persistent);
//                 cmds[0] = new ReadCommand
//                 {
//                     Offset = 0,
//                     Size = (int)fileInfo.Length,
//                     Buffer = default,
//                     FileHandle = default,
//                     ReadHandle = default,
//                     ReadType = ReadType.Read,
//                     Status = ReadStatus.InProgress
//                 };
//
//                 FileInfoResult fileInfoResult;
//                 readHandle = AsyncReadManager.GetFileInfo(this.filePath, &fileInfoResult);
//             }
//             catch (Exception e)
//             {
//                 source.TrySetException(e);
//             }
//         }
//
//         public bool MoveNext()
//         {
//             if (cancellation.IsCancellationRequested)
//             {
//                 readHandle.Cancel();
//                 source.TrySetCanceled(cancellation);
//                 return false;
//             }
//
//             switch (readHandle.Status)
//             {
//                 case ReadStatus.Complete:
//                     if (fileSize < 0)
//                     {
//                         fileSize = readHandle.GetBytesRead();
//                     }
//                     break;
//                 case ReadStatus.InProgress:
//                     break;
//                 case ReadStatus.Failed:
//                     source.TrySetException()
//                     break;
//                 case ReadStatus.Truncated:
//                     break;
//                 case ReadStatus.Canceled:
//                     source.TrySetCanceled();
//                     break;
//                 default:
//                     throw new ArgumentOutOfRangeException();
//             }
//
//
//             return true;
//         }
//     }
// }