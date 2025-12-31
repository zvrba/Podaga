using System.Diagnostics;

namespace Podaga.Interning;

/// <summary>
/// Convenience class for simplifying the implementation of <see cref="IInternable{T}"/>.
/// </summary>
/// <typeparam name="TSelf">
/// The most-derived type.
/// </typeparam>
public abstract class AbstractInternable<TSelf> : IInternable<TSelf>
    where TSelf : AbstractInternable<TSelf>
{
    /// <summary>
    /// Implements <see cref="IInternable.InternCode"/>.
    /// </summary>
    public uint? InternCode {
        get => field;
        private set {
            ThrowIfInterned();
            field = value;
        }
    }
    
    /// <summary>
    /// Must be  implemented by the derived class.  The method must also work on non-interned objects.
    /// </summary>
    public abstract bool Equals(TSelf? other);

    /// <summary>
    /// This is used in the implementation of <see cref="GetHashCode"/> and must also work on non-interned objects.
    /// This method is invoked only when <see cref="InternCode"/> is <c>null</c>.
    /// </summary>
    /// <returns>
    /// An uint value.  This value must be computed deterministically, just as the ordinary hash code.  In addition,
    /// the final value SHOULD be "mixed" using <see cref="TwistedTabulation.Mix(uint)"/>
    /// </returns>
    protected abstract uint GetInternCode();

    /// <summary>
    /// <para>
    /// Override this object to intern internable members.  <see cref="Intern"/> invokes this method only once, when <see cref="InternCode"/>
    /// is <c>null</c>.
    /// </para>
    /// <para>
    /// The default implementation is a no-op.
    /// </para>
    /// </summary>
    protected virtual void InternComposite(IInternator internator) { }

    /// <summary>
    /// Implements <see cref="IInternable{T}.Intern(IInternator)"/>.
    /// To intern composites, override <see cref="InternComposite(IInternator)"/>.
    /// </summary>
    /// <param name="internator">Internator instance.</param>
    /// <returns>An interned object equal to <c>this</c>.</returns>
    public TSelf Intern(IInternator internator)
    {
        if (!InternCode.HasValue) {
            InternComposite(internator);
            InternCode = GetInternCode();
        }
        return (TSelf)internator.Intern(this);
    }

    /// <summary>
    /// The override returns <see cref="InternCode"/> or <see cref="GetInternCode"/>.
    /// </summary>
    public sealed override int GetHashCode() => (int)(InternCode ?? GetInternCode());

    /// <summary>
    /// The override checks for reference equality and <see cref="InternCode"/> inequality (when set on both objects) before
    /// invoking <see cref="Equals(TSelf?)"/>.
    /// </summary>
    public sealed override bool Equals(object? obj)
    {
        if (ReferenceEquals(obj, this))
            return true;
        if (obj is not TSelf other)
            return false;
        if (InternCode.HasValue && other.InternCode.HasValue && InternCode != other.InternCode)
            return false;
        
        var ret = Equals(other);
        Debug.Assert(!ret || GetHashCode() == other.GetHashCode(), "Invalid InternCode/Equality implementation.");
        return ret;
    }

    /// <summary>
    /// Utility method to help with implementing mutable classes.  It should be invoked by every method that
    /// affects equality.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="InternCode"/> has a value.</exception>
    protected void ThrowIfInterned()
    {
        if (InternCode.HasValue)
            throw new InvalidOperationException($"Cannot mutate an interned object {GetType().FullName}.");
    }
}
