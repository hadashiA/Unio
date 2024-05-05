using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using System.Threading.Tasks;

namespace UnityNio
{
    public struct ReadResult : IDisposable
    {
        public NativeArray<byte> Buffer;

        public ReadResult(NativeArray<byte> buffer)
        {
            Buffer = buffer;
        }

        public Span<byte> AsSpan() => Buffer.AsSpan();

        public void Dispose()
        {
            // UnsafeUtility.Free(Buffer.GetUnsafePtr(), Allocator.Persistent);
            Buffer.Dispose();
        }
    }

    public struct ReadPromise
    {
        public string Path;
        public TaskCompletionSource<ReadResult> Source;
        public ulong TypeId; // for metric purpose
        public CancellationToken CancellationToken;

        public ReadRequest(string path, TaskCompletionSource<ReadResult> source, CancellationToken cancellationToken)
        {
            Path = path;
            Source = source;
            CancellationToken = cancellationToken;
        }

        public bool Poll()
        {

        }
    }

    public unsafe class AsyncReadLoop : MonoBehaviour
    {
        const int MaxConcurrency = 64;

        ReadHandle readHandle;
        NativeArray<ReadCommand> cmds;
        string assetName = "myfile";
        ulong typeID = 114; // from ClassIDReference
        AssetLoadingSubsystem subsystem = AssetLoadingSubsystem.Scripts;

        NativeArray<ReadCommand> readCommandBuffer;

        readonly ConcurrentQueue<string> requests = new();

        readonly ReadHandle[] ReadHandles = new ReadHandle[MaxConcurrency];
        int currentReadCount;

        void Awake()
        {
            readCommandBuffer = new NativeArray<ReadCommand>(MaxConcurrency, Allocator.Persistent);
        }

        public Task ReadAsync(string filePath, CancellationToken cancellation = default)
        {
            Interlocked.Increment(ref currentReadCount);
            var source = new TaskCompletionSource<ReadResult>();
        }

        void Start()
        {
        }

        void Update()
        {
            if (requests.Count > 0 && currentReadCount < readCommandBuffer.Length)
            {
                if (Interlocked.CompareExchange())
                while (requests.TryDequeue(out var request))
                {
                    cmds = new NativeArray<ReadCommand>(1, Allocator.Persistent);
                    ReadCommand cmd;
                    cmd.Offset = 0;
                    cmd.Size = 1024;
                    cmd.Buffer = (byte*)UnsafeUtility.Malloc(cmd.Size, 16, Allocator.Persistent);
                    cmds[0] = cmd;
                    readHandle = AsyncReadManager.Read(request.FilePath, (ReadCommand*)cmds.GetUnsafePtr(), 1, assetName, typeID, subsystem);
                    var fileHandle = AsyncReadManager.OpenFileAsync();
                    AsyncReadManager.GetFileInfo()
                }
            }

            var completedCount = 0;

            if (readHandle.IsValid() && readHandle.Status != ReadStatus.InProgress)
            {
                Debug.LogFormat("Read {0}", readHandle.Status == ReadStatus.Complete ? "Successful" : "Failed");
                readHandle.Dispose();
                UnsafeUtility.Free(cmds[0].Buffer, Allocator.Persistent);
                cmds.Dispose();
            }

            if (completedCount > 0)
            {
                // 穴埋め。
            }
        }
    }
}
