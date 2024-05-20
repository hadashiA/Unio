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
using UnityEngine.Profiling;

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
        const int Warmup = 5;
        const int Iterations = 1;
        const int InitialBufferSize = 1024;

        string filePath = default!;
        D data = D.CreateTestData();

        [SetUp]
        public void SetUp()
        {
            filePath = Path.Combine(Application.dataPath, "Unio.Tests", "image_1mb.gif");
        }

        [Test]
        [Performance]
        public void ReadAllBytes()
        {
            Measure.Method(() => { File.ReadAllBytes(filePath); })
                .SampleGroup("System.IO.File")
                .WarmupCount(Warmup)
                .IterationsPerMeasurement(Iterations)
                .MeasurementCount(10)
                .GC()
                .Run();

            Measure.Method(() => { NativeFile.ReadAllBytes(filePath); })
                .SampleGroup("Unio.NativeFile")
                .WarmupCount(Warmup)
                .IterationsPerMeasurement(Iterations)
                .MeasurementCount(10)
                .GC()
                .Run();
        }

        [Test]
        [Performance]
        public void SystemTextJsonSerialize()
        {
            Measure.Method(() =>
                {
                    var arrayBufferWriter = new ArrayBufferWriter<byte>(InitialBufferSize);
                    var jsonWriter = new Utf8JsonWriter(arrayBufferWriter);
                    JsonSerializer.Serialize(jsonWriter, data, typeof(D), SourceGenerationContext.Default);
                })
                .SampleGroup("System.Buffers.ArrayBufferWriter")
                .WarmupCount(Warmup)
                .IterationsPerMeasurement(Iterations)
                .MeasurementCount(5)
                .GC()
                .Run();

            Measure.Method(() =>
                {
                    using var arrayBufferWriter = new NativeArrayBufferWriter<byte>(InitialBufferSize, Allocator.Temp);
                    var jsonWriter = new Utf8JsonWriter(arrayBufferWriter);
                    JsonSerializer.Serialize(jsonWriter, data, typeof(D), SourceGenerationContext.Default);
                })
                .SampleGroup("Unio.NativeArrayBufferWriter")
                .WarmupCount(Warmup)
                .IterationsPerMeasurement(Iterations)
                .MeasurementCount(5)
                .GC()
                .Run();
        }

        [Test]
        [Performance]
        public void MemoryPackSerialize()
        {
            Measure.Method(() =>
                {
                    var arrayBufferWriter = new ArrayBufferWriter<byte>(InitialBufferSize);
                    var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
                    var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(ref arrayBufferWriter, state);
                    MemoryPackSerializer.Serialize(ref writer, in data);
                })
                .SampleGroup("System.Buffers.ArrayBufferWriter")
                .WarmupCount(Warmup)
                .IterationsPerMeasurement(Iterations)
                .MeasurementCount(5)
                .GC()
                .Run();

            Measure.Method(() =>
                {
                    var arrayBufferWriter = new NativeArrayBufferWriter<byte>(InitialBufferSize, Allocator.Temp);
                    var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
                    var writer = new MemoryPackWriter<NativeArrayBufferWriter<byte>>(ref arrayBufferWriter, state);
                    MemoryPackSerializer.Serialize(ref writer, in data);
                })
                .SampleGroup("Unio.NativeArrayBufferWriter")
                .WarmupCount(Warmup)
                .IterationsPerMeasurement(Iterations)
                .MeasurementCount(5)
                .GC()
                .Run();
        }


        [Test]
        [Performance]
        public void ReadAllBytes2()
        {
            for (var i = 0; i < Warmup; i++)
            {
                File.ReadAllBytes(filePath);
                NativeFile.ReadAllBytes(filePath);
            }

            {
                GC.Collect();
                var allocBytesBefore = Profiler.GetMonoUsedSizeLong();
                using (Measure.Scope("System.IO.File"))
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        File.ReadAllBytes(filePath);
                    }
                }

                var allocBytesAfter = Profiler.GetMonoUsedSizeLong() - allocBytesBefore;
                var sampleGroup = new SampleGroup("System.IO.File", SampleUnit.Kilobyte, false);
                Measure.Custom(sampleGroup, allocBytesAfter / 1024f);
            }

            {
                GC.Collect();
                var allocBytesBefore = Profiler.GetMonoUsedSizeLong();
                using (Measure.Scope("Unio.NativeFile"))
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        NativeFile.ReadAllBytes(filePath);
                    }
                }

                var allocBytesAfter = Profiler.GetMonoUsedSizeLong() - allocBytesBefore;
                var sampleGroup = new SampleGroup("Unio.NativeFile", SampleUnit.Kilobyte,
                    false);
                Measure.Custom(sampleGroup, allocBytesAfter / 1024f);
            }
        }

        [Test]
        [Performance]
        public void SystemTextJsonSerialize2()
        {
            void JsonSerializeArrayBufferWriter()
            {
                var arrayBufferWriter = new ArrayBufferWriter<byte>(InitialBufferSize);
                var jsonWriter = new Utf8JsonWriter(arrayBufferWriter);
                JsonSerializer.Serialize(jsonWriter, data, typeof(D), SourceGenerationContext.Default);
            }

            void JsonSerializeNativeArrayBufferWriter()
            {
                var arrayBufferWriter = new NativeArrayBufferWriter<byte>(InitialBufferSize, Allocator.Temp);
                var jsonWriter = new Utf8JsonWriter(arrayBufferWriter);
                JsonSerializer.Serialize(jsonWriter, data, typeof(D), SourceGenerationContext.Default);
            }

            for (var i = 0; i < Warmup; i++)
            {
                JsonSerializeArrayBufferWriter();
                JsonSerializeNativeArrayBufferWriter();
            }

            {
                GC.Collect();
                var allocBytesBefore = Profiler.GetMonoUsedSizeLong();
                using (Measure.Scope("ArrayBufferWriter"))
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        JsonSerializeArrayBufferWriter();
                    }
                }

                var allocBytesAfter = Profiler.GetMonoUsedSizeLong() - allocBytesBefore;
                var sampleGroup = new SampleGroup("ArrayBufferWriter.TotalAllocatedMemory", SampleUnit.Kilobyte, false);
                Measure.Custom(sampleGroup, allocBytesAfter / 1024f);
            }

            {
                GC.Collect();
                var allocBytesBefore = Profiler.GetMonoUsedSizeLong();
                using (Measure.Scope("NativeArrayBufferWriter"))
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        JsonSerializeNativeArrayBufferWriter();
                    }
                }

                var allocBytesAfter = Profiler.GetMonoUsedSizeLong() - allocBytesBefore;
                var sampleGroup = new SampleGroup("NativeArrayBufferWriter.TotalAllocatedMemory", SampleUnit.Kilobyte,
                    false);
                Measure.Custom(sampleGroup, allocBytesAfter / 1024f);
            }
        }

        [Test]
        [Performance]
        public void MemoryPackSerialize2()
        {
            void MemoryPackSerializeArrayBufferWriter()
            {
                var arrayBufferWriter = new ArrayBufferWriter<byte>(InitialBufferSize);
                var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
                var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(ref arrayBufferWriter, state);
                MemoryPackSerializer.Serialize(ref writer, in data);
            }

            void MemoryPackSerializeNativeArrayBufferWriter()
            {
                var arrayBufferWriter = new NativeArrayBufferWriter<byte>(InitialBufferSize, Allocator.Temp);
                var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Default);
                var writer = new MemoryPackWriter<NativeArrayBufferWriter<byte>>(ref arrayBufferWriter, state);
                MemoryPackSerializer.Serialize(ref writer, in data);
            }

            for (var i = 0; i < Warmup; i++)
            {
                MemoryPackSerializeArrayBufferWriter();
                MemoryPackSerializeNativeArrayBufferWriter();
            }

            {
                GC.Collect();
                var allocBytesBefore = Profiler.GetMonoUsedSizeLong();
                using (Measure.Scope("ArrayBufferWriter"))
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        MemoryPackSerializeArrayBufferWriter();
                    }
                }

                var allocBytesAfter = Profiler.GetMonoUsedSizeLong() - allocBytesBefore;
                var sampleGroup = new SampleGroup("ArrayBufferWriter.TotalAllocatedMemory", SampleUnit.Kilobyte, false);
                Measure.Custom(sampleGroup, allocBytesAfter / 1024f);
            }

            {
                GC.Collect();
                var allocBytesBefore = Profiler.GetMonoUsedSizeLong();
                using (Measure.Scope("NativeArrayBufferWriter"))
                {
                    for (var i = 0; i < Iterations; i++)
                    {
                        MemoryPackSerializeNativeArrayBufferWriter();
                    }
                }

                var allocBytesAfter = Profiler.GetMonoUsedSizeLong() - allocBytesBefore;
                var sampleGroup = new SampleGroup("NativeArrayBufferWriter.TotalAllocatedMemory", SampleUnit.Kilobyte,
                    false);
                Measure.Custom(sampleGroup, allocBytesAfter / 1024f);
            }
        }
    }
}
