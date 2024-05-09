using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Unio;

namespace UnityNio
{
    public class SampleBehaviour : MonoBehaviour
    {
        [CanBeNull]
        string str;

        void Start()
        {
            UnityEngine.Debug.Log($"11111 {Thread.CurrentThread.ManagedThreadId}");
            Task.Run(() =>
            {
                UnityEngine.Debug.Log($"22222 {Thread.CurrentThread.ManagedThreadId}");
                using var buffer = NativeIO.ReadFile("/Users/hadashi/tmp/log");
                this.str = System.Text.Encoding.UTF8.GetString(buffer.AsSpan());
            });
        }

        void Update()
        {
            if (str != null)
            {
                UnityEngine.Debug.Log(str);
            }
        }
    }
}
