using BenchmarkDotNet.Running;
using ModTek.Benchmarks;

BenchmarkRunner.Run([
    BenchmarkConverter.TypeToBenchmarks(typeof(FileUtilsBenchmarks)),
    BenchmarkConverter.TypeToBenchmarks(typeof(AssemblyUtilsBenchmarks)),
    BenchmarkConverter.TypeToBenchmarks(typeof(DictionaryExtensionsBenchmarks)),
]);
