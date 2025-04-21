using System;

namespace Podaga.SimdSort;

/// <summary>
/// Represents an in-place sorting method.
/// </summary>
/// <remarks>
/// The range of valid values of <paramref name="c"/> for which the result of sorting is guaranteed to be correct is restricted.
/// Behaviour is UNDEFINED when <paramref name="c"/> is larger than the space allocated for <paramref name="data"/>: arbitrary
/// data will be overwritten and the program may crash.
/// </remarks>
/// <typeparam name="T">Type of elements being sorted.</typeparam>
/// <param name="data">Pointer to the beginning of the range to sort.</param>
/// <param name="c">Number of elements in <paramref name="data"/>.  No bounds checking is performed on the value.</param>
/// <seealso cref="SimdSort{T}.MaxLength"/>
public unsafe delegate void UnsafeSort<T>(T* data, int c) where T : unmanaged;

/// <summary>
/// Provides methods for sorting arrays of ints or floats using a periodic sorting network.
/// </summary>
/// <typeparam name="T">The type of array elements.  Only <c>int</c> and <c>float</c> types are currently supported.</typeparam>
public abstract class SimdSort<T> where T : unmanaged
{
    /// <summary>
    /// Creates an instance of <see cref="SimdSort{T}"/> for <typeparamref name="T"/>.
    /// </summary>
    /// <param name="maxLength">
    /// Maximum array length supported by the sorter.  Sorters for sizes of up to 16 are more efficent than the general-length
    /// sorters and should therefore be used for small arrays.
    /// </param>
    /// <returns>
    /// An instance that is valid for sorting arrays of length between 1 and <see cref="MaxLength"/> elements.  The instance
    /// can be cached and used from multiple threads.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxLength"/> exceeds <c>2^24</c>, which is the maximum supported value. - OR - <typeparamref name="T"/>
    /// is not <c>int</c> or <c>float</c>.
    /// </exception>
    public static SimdSort<T> Create(int maxLength) {
        object? ret = null;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxLength, 1 << 24);
        if (typeof(T) == typeof(int))
            ret = new IntSorter(maxLength);
        if (typeof(T) == typeof(float))
            ret = new FloatSorter(maxLength);
        return (SimdSort<T>?)ret ?? throw new ArgumentOutOfRangeException("Unsupported element type: " + typeof(T).Name);
    }

    private protected SimdSort()
    { }

    /// <summary>
    /// Maximum array length supported by this sorter.
    /// </summary>
    public int MaxLength { get; protected init; }

    /// <summary>
    /// Delegate that performs the actual sorting.  It may be invoked directly to avoid the overhead of <see cref="Sort(Span{T})"/>.
    /// The caller is responsible for ensuring the correctness of passed values.
    /// </summary>
    public UnsafeSort<T> Sorter { get; protected init; } = null!;

    /// <summary>
    /// Convenience overload for use in "safe" code.  Checks preconditions and then invokes <see cref="Sorter"/>.
    /// </summary>
    /// <param name="data">Range of elements to sort.</param>
    /// <exception cref="ArgumentOutOfRangeException">The array length is invalid.</exception>
    public unsafe void Sort(Span<T> data) {
        if (data.Length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(data), $"Invalid array length ({data.Length}).");
        if (data.Length > 0) {
            fixed (T* p = data)
                Sorter(p, data.Length);
        }
    }
}
