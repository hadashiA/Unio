using System.IO;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Unio.Benchmark
{
    [TestFixture]
    public class Benchmark
    {
        readonly string filePath;

        public Benchmark()
        {
            filePath = Path.Combine(Application.dataPath, "Unio.Benchmark", "image_1mb.gif");
        }

        [Test]
        [Performance]
        public void ReadAllBytes()
        {
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
   }
}