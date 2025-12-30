using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Podaga.Interning;

// Implements simple linear probing, but using twisted tabulation hashing for better hash function independence guarantees.
// See Thorup: Fast and Powerful Hashing Using Tabulation

/// <summary>
/// Implements <see cref="IInternator"/> using hashing with open addressing.  This class is not suitable for interning struct
/// types because they will be boxed.
/// </summary>
/// <remarks>
/// <para>
/// The hash holds a <see cref="WeakGCHandle{T}"/> to every interned element.  When the load factor exceeds 3/4,
/// <see cref="GC.Collect(int)"/> is invoked to collect objects up to gen 1.  Then survived elements are counted and a new hash table
/// is allocated only for them.  Load factor calculation counts all allocated GC handles, regardless of whether their target is
/// still alive.
/// </para>
/// <para>
/// Equality check during search is optimized by first comparing the elements' <see cref="IInternable.InternCode"/>: when they're
/// different, the elements cannot be equal.
/// </para>
/// </remarks>
public sealed class WeakHash : IDisposable, IInternator
{
    private WeakGCHandle<IInternable>[] wrefs;
    private int addcount;           // # of occupied slots

    /// <summary>
    /// Constructor.  Sets up an empty table with initial capacity of 17.
    /// </summary>
    public WeakHash()
    {
        wrefs = new WeakGCHandle<IInternable>[TwistedTabulation.GetPrime(16)];
    }

    /// <summary>
    /// Releases all memory used by this instance.
    /// </summary>
    public void Dispose()
    {
        if (wrefs is null)
            return;
        for (var i = 0; i < wrefs.Length; ++i)
            if (wrefs[i].IsAllocated)
                wrefs[i].Dispose();
        wrefs = null!;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer: only invokes <see cref="Dispose"/>.
    /// </summary>
    ~WeakHash() => Dispose();

    // Ensures ca 75% load factor before deallocating.

    /// <inheritdoc/>
    public T Intern<T>(T value) where T : class, IInternable
    {
        ArgumentNullException.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(wrefs is null, this);
        if (!value.InternCode.HasValue)
            throw new ArgumentException("The value must have its InternCode set.", nameof(value));

        var ret = GetOrAdd(value, default);
        if (checked(addcount > wrefs.Length * 3 / 4))
            Resize();
        return (T)ret;
    }

    private void Resize()
    {
        GC.Collect(1, GCCollectionMode.Optimized);
        
        // Count surviving elements and dispose handles pointing to dead elements.
        var livecount = 0;
        for (var i = 0; i < wrefs.Length; ++i) {
            if (wrefs[i].IsAllocated) {
                if (wrefs[i].TryGetTarget(out var _)) ++livecount;
                else wrefs[i].Dispose();
            }
        }

        // Increase size even if livecount makes up for < load factor of 3/4.  Otherwise, resizings (and GC.Collect) become
        // increasingly frequent as the load factor approaches 3/4.
        var newsize = TwistedTabulation.ExpandPrime(livecount);
        var owrefs = wrefs;
        wrefs = new WeakGCHandle<IInternable>[newsize];
        //Debug.WriteLine($"OLDSIZE={owrefs.Length} ADDCOUNT={addcount} LIVE={livecount}, NEWSIZE={newsize}");
        addcount = 0;
        for (var i = 0; i < livecount; ++i)
            if (owrefs[i].IsAllocated && owrefs[i].TryGetTarget(out var item))
                GetOrAdd(item, owrefs[i]);
    }

    // handle.IsAllocated is true ONLY when this is invoked from Resize()
    private IInternable GetOrAdd(IInternable value, WeakGCHandle<IInternable> handle)
    {
        var ibegin = (int)(value.InternCode!.Value % wrefs.Length);
        var islot = ibegin;
        var useslot = -1;
        do {
            if (!wrefs[islot].IsAllocated) {                // The slot is empty: add the value.
                if (useslot < 0)                            // May already point to a tombstone that we want to reuse.
                    useslot = islot;
                break;
            }

            if (!wrefs[islot].TryGetTarget(out var target)) {   // Must keep searching for equal and can reuse only the first tombstone
                if (useslot < 0)
                    useslot = islot;
            }
            else if (value.InternCode.Value == target.InternCode!.Value && value.Equals(target)) {
                return target;
            }

            if (++islot == wrefs.Length)
                islot = 0;
        } while (islot != ibegin);

        if (useslot < 0)
            throw new NotImplementedException("BUG: The table is full.");

        // When a tombstone is being reused, count is not increased because it is unchanged for the purposes of searching / load factor.
        
        if (wrefs[useslot].IsAllocated) {
            Debug.Assert(!wrefs[useslot].TryGetTarget(out var _));
            wrefs[useslot].SetTarget(value);

            // GC might have kicked in from another thread during resize, so element other than value/handle sarguments
            // might have been freed. We must dispose of the old handle.
            if (handle.IsAllocated)
                handle.Dispose();
        }
        else {
            Debug.Assert(!handle.IsAllocated || (handle.TryGetTarget(out var target) && ReferenceEquals(target, value)));
            if (!handle.IsAllocated)
                handle = new WeakGCHandle<IInternable>(value);
            wrefs[useslot] = handle;
            ++addcount;
        }
        return value;
    }
}
