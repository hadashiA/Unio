using System;
using System.Threading;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Unio
{
    public struct UnioFixedUpdate {}
    public struct UnioPostFixedUpdate {}
    public struct UnioUpdate {}
    public struct UnioPostUpdate {}
    public struct UnioLateUpdate {}
    public struct UnioPostLateUpdate {}

    enum PlayerLoopTiming
    {
        Initialization = 0,
        PostInitialization = 1,

        Startup = 2,
        PostStartup = 3,

        FixedUpdate = 4,
        PostFixedUpdate = 5,

        Update = 6,
        PostUpdate = 7,

        LateUpdate = 8,
        PostLateUpdate = 9,
    }

    interface IPlayerLoopItem
    {
        bool MoveNext();
    }

    sealed class PlayerLoopRunner
    {
        readonly Queue<IPlayerLoopItem> runningQueue = new Queue<IPlayerLoopItem>();
        readonly Queue<IPlayerLoopItem> waitingQueue = new Queue<IPlayerLoopItem>();

        readonly object runningGate = new object();
        readonly object waitingGate = new object();

        int running;

        public void Dispatch(IPlayerLoopItem item)
        {
            if (Interlocked.CompareExchange(ref running, 1, 1) == 1)
            {
                lock (waitingGate)
                {
                    waitingQueue.Enqueue(item);
                    return;
                }
            }

            lock (runningGate)
            {
                runningQueue.Enqueue(item);
            }
        }

        public void Run()
        {
            Interlocked.Exchange(ref running, 1);

            lock (runningGate)
            lock (waitingGate)
            {
                while (waitingQueue.Count > 0)
                {
                    var waitingItem = waitingQueue.Dequeue();
                    runningQueue.Enqueue(waitingItem);
                }
            }

            IPlayerLoopItem item;
            lock (runningGate)
            {
                item = runningQueue.Count > 0 ? runningQueue.Dequeue() : null;
            }

            while (item != null)
            {
                var continuous = false;
                try
                {
                    continuous = item.MoveNext();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }

                if (continuous)
                {
                    lock (waitingGate)
                    {
                        waitingQueue.Enqueue(item);
                    }
                }

                lock (runningGate)
                {
                    item = runningQueue.Count > 0 ? runningQueue.Dequeue() : null;
                }
            }

            Interlocked.Exchange(ref running, 0);
        }
    }

    static class PlayerLoopHelper
    {
        static readonly PlayerLoopRunner[] Runners = new PlayerLoopRunner[10];
        static long initialized;

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureInitialized()
        {
            if (Interlocked.CompareExchange(ref initialized, 1, 0) != 0)
                return;

            for (var i = 0; i < Runners.Length; i++)
            {
                Runners[i] = new PlayerLoopRunner();
            }

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var copyList = playerLoop.subSystemList;

            ref var updateSystem = ref FindSubSystem(typeof(Update), copyList);
            InsertSubsystem(
                ref updateSystem,
                typeof(Update.ScriptRunBehaviourUpdate),
                new PlayerLoopSystem
                {
                    type = typeof(UnioUpdate),
                    updateDelegate = Runners[(int)PlayerLoopTiming.Update].Run
                });

            playerLoop.subSystemList = copyList;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static void Dispatch(PlayerLoopTiming timing, IPlayerLoopItem item)
        {
            EnsureInitialized();
            Runners[(int)timing].Dispatch(item);
        }

        static ref PlayerLoopSystem FindSubSystem(Type targetType, PlayerLoopSystem[] systems)
        {
            for (var i = 0; i < systems.Length; i++)
            {
                if (systems[i].type == targetType)
                    return ref systems[i];
            }
            throw new InvalidOperationException($"{targetType.FullName} not in systems");
        }

        static void InsertSubsystem(ref PlayerLoopSystem parentSystem, Type beforeType, PlayerLoopSystem newSystem)
        {
            var source = parentSystem.subSystemList;
            var insertIndex = -1;
            if (beforeType == null)
            {
                insertIndex = 0;
            }
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i].type == beforeType)
                {
                    insertIndex = i;
                }
            }

            if (insertIndex < 0)
            {
                throw new ArgumentException($"{beforeType.FullName} not in system {parentSystem} {parentSystem.type.FullName}");
            }

            var dest = new PlayerLoopSystem[source.Length + 2];
            for (var i = 0; i < dest.Length; i++)
            {
                if (i == insertIndex)
                {
                    dest[i] = newSystem;
                }
                else if (i < insertIndex)
                {
                    dest[i] = source[i];
                }
                else
                {
                    dest[i] = source[i - 1];
                }
            }
            parentSystem.subSystemList = dest;
        }
    }
}
