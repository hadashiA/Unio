using NUnit.Framework;
using Unity.Collections;

namespace Unio.Tests
{
    [TestFixture]
    public class NativeArrayBufferWriterTest
    {
        [Test]
        public void WrittenBuffer()
        {
            using var writer = new NativeArrayBufferWriter<byte>(8, Allocator.Temp);
            var span = writer.GetSpan(2);
            span[0] = (byte)'a';
            span[1] = (byte)'b';
            writer.Advance(2);

            var result = writer.WrittenBuffer;
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result[0], Is.EqualTo((byte)'a'));
            Assert.That(result[1], Is.EqualTo((byte)'b'));
        }

        [Test]
        public void Reallocate()
        {
            using var writer = new NativeArrayBufferWriter<byte>(2, Allocator.Temp);
            var span = writer.GetSpan(4);
            span[0] = (byte)'a';
            span[1] = (byte)'b';
            span[2] = (byte)'c';
            span[3] = (byte)'d';
            writer.Advance(4);

            var result = writer.WrittenBuffer;
            Assert.That(result.Length, Is.EqualTo(4));
            Assert.That(result[0], Is.EqualTo((byte)'a'));
            Assert.That(result[1], Is.EqualTo((byte)'b'));
            Assert.That(result[2], Is.EqualTo((byte)'c'));
            Assert.That(result[3], Is.EqualTo((byte)'d'));
        }

    }
}
