using System;
using Podaga.SimdSort;

namespace Podaga.Test;

class SimdSort_Test
{
    /// <summary>
    /// Validates sorting network by exploiting theorem Z of TAOCOP section 5.3.4: it is
    /// sufficient to check that all 0-1 sequences (2^N of them) are sorted by the network.
    /// Only lengths of up to <c>28</c> are accepted.
    /// </summary>
    public static void CheckInt(int size) {
        if (size < 4 || size > 31)
            throw new ArgumentOutOfRangeException(nameof(size), "Valid range is [4, 31].");

        var sort = SimdSort<int>.Create(size);
        Span<int> bits = new int[size];

        for (uint i = 0; i <= (1 << size) - 1; ++i) {
            int popcnt = 0; // Number of ones in i
            for (uint j = i, k = 0; k < size; ++k, j >>= 1) {
                int b = (int)(j & 1);
                bits[(int)k] = b;
                popcnt += b;
            }

            sort.Sort(bits);

            for (int k = 0; k < size - popcnt; ++k)
                if (bits[k] != 0)
                    throw new NotImplementedException($"Result is not a permutation for bit pattern {i:X8}.");

            for (int k = size - popcnt; k < size; ++k)
                if (bits[k] != 1)
                    throw new NotImplementedException($"Result is not a permutation for bit pattern {i:X8}.");
        }
    }

    /// <summary>
    /// Checker for float arrays.  <see cref="CheckInt(int)"/> for details.
    /// </summary>
    public static void CheckFloat(int size) {
        if (size < 4 || size > 31)
            throw new ArgumentOutOfRangeException(nameof(size), "Valid range is [4, 31].");

        var sort = SimdSort<float>.Create(size);
        Span<float> bits = new float[size];

        for (uint i = 0; i <= (1 << size) - 1; ++i) {
            int popcnt = 0; // Number of ones in i
            for (uint j = i, k = 0; k < size; ++k, j >>= 1) {
                int b = (int)(j & 1);
                bits[(int)k] = b;
                popcnt += b;
            }

            sort.Sort(bits);

            for (int k = 0; k < size - popcnt; ++k)
                if (bits[k] != 0)
                    throw new NotImplementedException($"Result is not a permutation for bit pattern {i:X8}.");

            for (int k = size - popcnt; k < size; ++k)
                if (bits[k] != 1)
                    throw new NotImplementedException($"Result is not a permutation for bit pattern {i:X8}.");
        }
    }

    /// <summary>
    /// Checks whether array <paramref name="data"/> is sorted.
    /// </summary>
    /// <returns>True if the input is sorted, false otherwise.</returns>
    public static bool IsSorted(int[] data) {
        for (int i = 1; i < data.Length; ++i)
            if (data[i] < data[i - 1])
                return false;
        return true;
    }

    /// <summary>
    /// Checks whether array <paramref name="data"/> is sorted.
    /// </summary>
    /// <returns>True if the input is sorted, false otherwise.</returns>
    public static bool IsSorted(float[] data) {
        for (int i = 1; i < data.Length; ++i)
            if (data[i] < data[i - 1])
                return false;
        return true;
    }
}
