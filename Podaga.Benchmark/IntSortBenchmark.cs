using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

using Podaga.SimdSort;

namespace Podaga.Benchmark;

[GcServer(true)]
[XmlExporterAttribute.Brief]
[XmlExporter(fileNameSuffix: "xml", indentXml: true, excludeMeasurements: true)]
[HardwareCounters(HardwareCounter.InstructionRetired, HardwareCounter.CacheMisses, HardwareCounter.BranchInstructionRetired)]
public class IntSortBenchmark
{
    private const int MaxSize = 1048576;

    public static IEnumerable<int> Sizes => [
        4, 6, 8, 13, 16, 27, 32, 47, 64, 101, 128, 177, 256, 365, 512, 748, 1024, 1537, 2048, 3389, 4096, 6793,
        8192, 14289, 16384, 23124, 32768, 53151, 65536, 96317, 131072, 191217, 262144, 398853, 524288, 719289, MaxSize
    ];

    // Obtained from random.org
    private static readonly int[] UnsortedData = new int[MaxSize];

    unsafe static IntSortBenchmark() {
        var rnd = new Random(117257);
        for (var i = 0; i < UnsortedData.Length; ++i)
            UnsortedData[i] = rnd.Next(-(1 << 30), 1 << 30);
    }

    SimdSort<int> n;
    int[] d;

    [ParamsSource(nameof(Sizes))]
    public int Size { get; set; }
    
    // Use AES to deterministically generate random numbers.
    [GlobalSetup]
    public unsafe void GlobalSetup() {
        n = SimdSort<int>.Create(Size);
        d = new int[Size];
        CoreSelector.SetAffinity();
    }

    [Benchmark]
    public void AdditiveBaseline() {
        Array.Copy(UnsortedData, 0, d, 0, Size);
    }

    [Benchmark]
    public void ArraySort() {
        Array.Copy(UnsortedData, 0, d, 0, Size);
        Array.Sort(d, 0, Size);
    }

    [Benchmark]
    public unsafe void NetworkSort() {
        Array.Copy(UnsortedData, 0, d, 0, Size);
        fixed (int* p = d)
            n.Sorter(p, Size);
    }
}
