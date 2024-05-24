using System;
using System.Threading;
using Unio.Internal;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Unio
{
    // public struct UnioFixedUpdate {}
    // public struct UnioPostFixedUpdate {}
    public struct UnioUpdate {}
    // public struct UnioPostUpdate {}
    // public struct UnioLateUpdate {}
    // public struct UnioPostLateUpdate {}

    enum PlayerLoopTiming
    {
        // Initialization = 0,
        // PostInitialization = 1,
        //
        // Startup = 2,
        // PostStartup = 3,
        //
        // FixedUpdate = 4,
        // PostFixedUpdate = 5,
        //
        Update = 6,
        // PostUpdate = 7,
        //
        // LateUpdate = 8,
        // PostLateUpdate = 9,
    }

    static class PlayerLoopHelper
    {
        static readonly PlayerLoopRunner UpdateRunner = new();
        static long initialized;

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureInitialized()
        {
            if (Interlocked.CompareExchange(ref initialized, 1, 0) != 0)
                return;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var copyList = playerLoop.subSystemList;

            ref var updateSystem = ref FindSubSystem(typeof(Update), copyList);
            InsertSubsystem(
                ref updateSystem,
                typeof(Update.ScriptRunBehaviourUpdate),
                new PlayerLoopSystem
                {
                    type = typeof(UnioUpdate),
                    updateDelegate = UpdateRunner.Run
                });

            playerLoop.subSystemList = copyList;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static void Dispatch(PlayerLoopTiming timing, IPlayerLoopItem item)
        {
            EnsureInitialized();
            switch (timing)
            {
                case PlayerLoopTiming.Update:
                    UpdateRunner.Dispatch(item);
                    break;
                default:
                    throw new NotSupportedException();
            }
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
                    break;
                }
            }

            if (insertIndex < 0)
            {
                throw new ArgumentException($"{beforeType.FullName} not in system {parentSystem} {parentSystem.type.FullName}");
            }

            var dest = new PlayerLoopSystem[source.Length + 1];
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
