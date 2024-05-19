using System;
using NUnit.Framework;
using Unity.Collections;

namespace Unio.Tests
{
    [TestFixture]
    public class NativeArrayMemoryManagerTest
    {
        [Test]
        public void AsMemory()
        {
            var nativeArray = new NativeArray<byte>(8, Allocator.Temp);
            for (var i = 0; i < nativeArray.Length; i++)
            {
                nativeArray[i] = (byte)(i + 'a');
            }

            var span = nativeArray.AsSpan();
            var memory = nativeArray.AsMemory();
            Assert.That(span.SequenceEqual(memory.Span), Is.True);
        }
    }
}
