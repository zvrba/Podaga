using Podaga.JoinableTree;

using System;
using System.Collections.Generic;

namespace Podaga.Test;

public static class Program
{
    const int SequenceSize = 384;

    public static void Main() {

#if false
        for (var i = 4; i < 24; ++i) {
            Console.WriteLine($"Checking sorting network for N={i}");
            SimdSort_Test.CheckInt(i);
        }
#endif

#if true
        // NB! Running time grows at least quadratically with element count.
        var sequences = GetSequences(SequenceSize);
        var avljoin = new AvlJoin<int>(Comparer<int>.Default);
        var wbjoin = new WBJoin<int>(Comparer<int>.Default);

        TreeSet_BasicTest.Run(avljoin, sequences);
        TreeSet_SetTest.Run(avljoin, SequenceSize);
        
        TreeSet_BasicTest.Run(wbjoin, sequences);
        TreeSet_SetTest.Run(wbjoin, SequenceSize);
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