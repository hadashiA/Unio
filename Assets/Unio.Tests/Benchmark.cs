using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using MemoryPack;

namespace Unio.Benchmark
{
    [MemoryPackable]
    partial class A
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public DateTime DateTime { get; set; }
        public List<B> NestedValues { get; set; }
    }

    [MemoryPackable]
    partial class B
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public DateTime DateTime { get; set; }
    }

    [MemoryPackable]
    partial class D
    {
        public static D CreateTestData()
        {
            var result = new D();
            for (var i = 0; i < 100; i++)
            {
                result.Values.Add(new A
                {
                    IntValue = i * 100,
                    StringValue =
                        "CAD データや 3D データを、場所を問わずあらゆるデバイスで利用できる没入型のアプリケーションや体験に変えるのに必要な制作ツールやエンタープライズサポートを活用しましょう。",
                    NestedValues = new List<B>
                    {
                        new()
                        {
                            IntValue = 12345678,
                            StringValue =
                                "\n制作、ローンチ、さらにその先までサポートする Unity のエンドツーエンドのツールやサービスを活用して、20 以上のプラットフォームや何十億ものデバイス向けに素晴らしいゲームを制作し、成長させましょう。"
                        },
                        new()
                        {
                            IntValue = 1234567,
                            StringValue = "Unityの強力なツール、サービス、専門知識一式を利用して、アプリを初日から成長させ、ビジネスを成功に導きましょう。"
                        },
                    }
                });
            }
            return result;
        }

        public List<A> Values { get; set; } = new();
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(A))]
    [JsonSerializable(typeof(B))]
    [JsonSerializable(typeof(D))]
    partial class SourceGenerationContext : JsonSerializerContext
    {
    }

    [TestFixture]
    public class Benchmark
    {
        [Test]
        [Performance]
        public void ReadAllBytes()
        {
            var filePath = Path.Combine(Application.dataPath, "Unio.Tests", "image_1mb.gif");

            Measure.Method(() =>
                {
                    File.ReadAllBytes(filePath);
                })
                .SampleGroup("System.IO.File")
                .WarmupCount(10)
                .MeasurementCount(10)
                .IterationsPerMeasurement(5)
                .GC()
                .Run();

            Measure.Method(() =>
                {
                    NativeFile.ReadAllBytes(filePath);
                })
                .SampleGroup("Unio.NativeFile")
                .WarmupCount(10)
                .MeasurementCount(10)
                .IterationsPerMeasurement(5)
                .GC()
                .Run();
        }

        [Test]
        [Performance]
        public void SystemTextJsonSerialize()
        {
            var data = D.CreateTestData();
            Measure.Method(() =>
                {
                    var arrayBufferWriter = new ArrayBufferWriter<byte>(1024);
                    var jsonWriter = new Utf8JsonWriter(arrayBufferWriter);
                    JsonSerializer.Serialize(jsonWriter, data, typeof(D), SourceGenerationContext.Default);
                })
                .SampleGroup("System.Buffers.ArrayBufferWriter")
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(10)
                .GC()
                .Run();

            Measure.Method(() =>
                {
                    using var arrayBufferWriter = new NativeArrayBufferWriter<byte>(1024, Allocator.TempJob);
                    var jsonWriter = new Utf8JsonWriter(arrayBufferWriter);
                    JsonSerializer.Serialize(jsonWriter, data, typeof(D), SourceGenerationContext.Default);
                })
                .SampleGroup("Unio.NativeArrayBufferWriter")
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(10)
                .GC()
                .Run();
        }

        [Test]
        [Performance]
        public void MemoryPackSerialize()
        {
            var data = D.CreateTestData();
            Measure.Method(() =>
                {
                    var arrayBufferWriter = new ArrayBufferWriter<byte>(1024);
                    var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
                    var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(ref arrayBufferWriter, state);
                    MemoryPackSerializer.Serialize(ref writer, in data);
                })
                .SampleGroup("System.Buffers.ArrayBufferWriter")
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(10)
                .GC()
                .Run();

            Measure.Method(() =>
                {
                    var arrayBufferWriter = new NativeArrayBufferWriter<byte>(1024, Allocator.Temp);
                    var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
                    var writer = new MemoryPackWriter<NativeArrayBufferWriter<byte>>(ref arrayBufferWriter, state);
                    MemoryPackSerializer.Serialize(ref writer, in data);
                })
                .SampleGroup("Unio.NativeArrayBufferWriter")
                .WarmupCount(1)
                .MeasurementCount(5)
                .IterationsPerMeasurement(10)
                .GC()
                .Run();

        }
    }
}