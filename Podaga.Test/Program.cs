using System;
using System.Collections.Generic;

using IntTree;

using Podaga.Test;

namespace Podaga.Test;

public static class Program
{
    const int SequenceSize = 384;

    public static void Main() {
        for (var i = 4; i < 32; ++i) {
            Console.WriteLine($"Checking sorting network for N={i}");
            SimdSort_Test.CheckInt(i);
        }
#if false
        // NB! Running time grows at least quadratically with element count.
        var sequences = GetSequences(SequenceSize);

        TreeSet_BasicTest<AvlIntTree>.Run(sequences);
        TreeSet_SetTest<AvlIntTree>.Run(SequenceSize);
        
        TreeSet_BasicTest<WBIntTree>.Run(sequences);
        TreeSet_SetTest<WBIntTree>.Run(SequenceSize);
#endif
    }

    private static List<int[]> GetSequences(int max) {
        var ret = new List<int[]>();
        foreach (var g in Podaga.JoinableTree.PermutationGenerators.Generators) {
            var a = new int[max];
            g(a);
            ret.Add(a);
        }
        return ret;
    }
}