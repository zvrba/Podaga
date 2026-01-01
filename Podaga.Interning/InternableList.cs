using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Podaga.Interning;

/// <summary>
/// Internable <see cref="IList{T}"/>.  When <typeparamref name="T"/> implements <see cref="IInternable"/>, the list interns every
/// element before interning itself.
/// </summary>
/// <remarks>
/// <para>
/// The static constructor throws <see cref="ArgumentException"/> when <typeparamref name="T"/> is neither an <see cref="IInternable{TSelf}"/>
/// or a value type.
/// </para>
/// <para>
/// This class implements small-storage optimization: when the number of elements is less than <see cref="MaxSmallCount"/>,
/// they are stored inline in the instance itself.  Otherwise, <see cref="List{T}"/> is used as the underlying storage.
/// </para>
/// </remarks>
/// <typeparam name="T">
/// Type of elements stored in the list.  This type is restricted, at run-time, to either structs or instances of <see cref="IInternable"/>.
/// </typeparam>
public sealed class InternableList<T> : AbstractInternable<InternableList<T>>, IList<T> where T : notnull, IEquatable<T>
{
    private static readonly bool TIsInternable;

    static InternableList()
    {
        TIsInternable = typeof(IInternable<T>).IsAssignableFrom(typeof(T));
        if (!TIsInternable && !typeof(T).IsValueType)
            throw new ArgumentException($"The generic argument is of type {typeof(T).FullName} which is neither IInternable nor a value type.");
    }

    // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/
    // int[] has allocation size 28+Length*4; object[] 32+Length*8
    // 32 = 8 ints or 4 references.  We use 8 to postpone some allocation and allow up to 256 variables with little allocation.

    /// <summary>
    /// Maximum number of elements that are stored inline.  When the count exceeds this number, a <see cref="List{T}"/> instance is allocated
    /// as the backing storage.
    /// </summary>
    public const int MaxSmallCount = 8;
    
    [InlineArray(MaxSmallCount)]
    private struct SmallStorage
    {
        private T _0;
    }

    private int smallCount;
    private SmallStorage smallStorage;
    private List<T>? list;

    /// <summary>
    /// Constructor from existing enumeration.
    /// </summary>
    /// <param name="values">Optional enumeration.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <typeparamref name="T"/> is neither an <see cref="IInternable"/> or a value type.
    /// </exception>
    public InternableList(IEnumerable<T>? values = null)
    {
        if (values is not null) {
            var issmall = values.Take(MaxSmallCount + 1).Count() <= MaxSmallCount;
            if (issmall) {
                foreach (var x in values)
                    Add(x);
            }
            else {
                list = new(values);
            }
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="capacity">Initial preallocated capacity.</param>
    public InternableList(int capacity)
    {
        if (capacity > MaxSmallCount)
            list = new(capacity);
    }

    private Span<T> GetSpan() => list is not null ? CollectionsMarshal.AsSpan(list) : ((Span<T>)smallStorage)[..smallCount];

    #region Interning and equality

    /// <summary>
    /// Implements deep equality: two lists are equal when they have the same number of elements and the elements are equal in sequence.
    /// </summary>
    /// <param name="other">The list to compare with.</param>
    /// <returns>True if the lists are equal.</returns>
    public override bool Equals(InternableList<T>? other)
    {
        if (ReferenceEquals(other, this))
            return true;
        if (other is null)
            return false;

        // Cache spans (saves one conditional per iteration).
        var s1 = GetSpan();
        var s2 = other.GetSpan();

        if (s1.Length != s2.Length)
            return false;

        for (var i = 0; i < s1.Length; ++i)
            if (!s1[i].Equals(s2[i]))
                return false;
        return true;
    }

    /// <summary>
    /// Implements <see cref="AbstractInternable{TSelf}.GetInternCode"/> by combining the elements' individual hashes.
    /// </summary>
    /// <returns>Intern code, mixed by <see cref="TwistedTabulation.Mix(uint)"/>.</returns>
    protected override uint GetInternCode()
    {
        var hc = new HashCode();
        var s = GetSpan();
        hc.Add(s.Length);
        for (var i = 0; i < s.Length; ++i)
            hc.Add(s[i]);
        return TwistedTabulation.Mix((uint)hc.ToHashCode());
    }

    /// <summary>
    /// Interns every individual element when <typeparamref name="T"/> is internable; otherwise is a no-op.
    /// </summary>
    /// <param name="internator">Internator passed to <see cref="AbstractInternable{TSelf}.Intern(IInternator)"/>.</param>
    protected override void InternComposite(IInternator internator)
    {
        if (!TIsInternable)
            return;
        
        var s = GetSpan();
        for (var i = 0; i < s.Length; ++i) {
            s[i] = ((IInternable<T>)s[i]).Intern(internator);
        }
    }

    #endregion

    #region IList

    /// <inheritdoc/>
    public int Count => GetSpan().Length;

    /// <summary>
    /// Becomes true after the list has been interned.  In that case, all mutation operations will throw <see cref="InvalidOperationException"/>.
    /// </summary>
    public bool IsReadOnly => InternCode.HasValue;

    /// <inheritdoc/>
    public T this[int index] {
        get => GetSpan()[index];
        set {
            ThrowIfInterned();
            GetSpan()[index] = value;
        }
    }

    /// <inheritdoc/>
    public int IndexOf(T item) => GetSpan().IndexOf(item);

    /// <inheritdoc/>
    public void Add(T item) => Insert(Count, item);

    /// <inheritdoc/>
    public void Insert(int index, T item)
    {
        ThrowIfInterned();
        if (smallCount == MaxSmallCount) {
            list = [.. this];
            smallCount = 0;
        }

        if (list is not null) {
            Debug.Assert(list.Count >= MaxSmallCount);
            list.Insert(index, item);
        }
        else {
            Span<T> right = smallStorage[index..smallCount];
            right.CopyTo(smallStorage[(index + 1)..]);
            smallStorage[index] = item;
            ++smallCount;
        }
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        ThrowIfInterned();
        if (list is not null) {
            Debug.Assert(list.Count > MaxSmallCount);
            list.RemoveAt(index);
            if (list.Count == MaxSmallCount) {
                smallCount = MaxSmallCount;
                GetSpan().CopyTo(smallStorage);
                list = null;
            }
        }
        else {
            Span<T> right = smallStorage[(index + 1)..smallCount];
            right.CopyTo(smallStorage[index..]);
            --smallCount;
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        ThrowIfInterned();
        smallCount = 0;
        list = null;
    }

    /// <inheritdoc/>
    public bool Contains(T item) => GetSpan().Contains(item);

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex) => GetSpan().CopyTo(array.AsSpan(arrayIndex));

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        ThrowIfInterned();
        var i = IndexOf(item);
        if (i < 0)
            return false;
        RemoveAt(i);
        return true;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
