namespace Podaga.Interning;

/// <summary>
/// This interface marks "internable" objects: those that have only a single copy in the system.  All internable objects MUST override
/// <see cref="object.Equals(object?)"/> and <see cref="object.GetHashCode"/>.  This interface is used only by <see cref="IInternator"/>
/// implementations; internable classes must implement <see cref="IInternable{TSelf}"/>.
/// </summary>
public interface IInternable
{
    /// <summary>
    /// Cached value of the object's hash code.  When this is non-null, 
    /// <list type="bullet">
    /// <item>
    /// This value MUST be returned by <see cref="object.GetHashCode"/>.
    /// </item>
    /// <item>
    /// The object has been interned and it MUST disallow changes to any property that affects hash code or equality.
    /// </item>
    /// </list>
    /// </summary>
    /// <seealso cref="TwistedTabulation.Mix(uint)"/>
    uint? InternCode { get; }
}

/// <summary>
/// An internable type must implement this interface.
/// </summary>
/// <typeparam name="TSelf">
/// The most-derived type implementing this interface.  This may be a struct.  There is no CRTP constraint to allow
/// arbitrary casts at run-time.
/// </typeparam>
/// <seealso cref="IInternable"/>
public interface IInternable<TSelf> : IInternable, IEquatable<TSelf> // where TSelf : IInternable<TSelf>
{
    /// <summary>
    /// Interns <c>this</c> with <paramref name="internator"/>.  See remarks for implementation notes.
    /// </summary>
    /// <param name="internator">Internator instance.</param>
    /// <returns>
    /// <returns>An interned object equal to <c>this</c>.</returns>
    /// </returns>
    /// <remarks>
    /// Outline for implementing this method:
    /// <list type="bullet">
    /// <item>
    /// If <c>this</c> is a composite of other <see cref="IInternable"/> instances, they SHOULD be recursively interned with <paramref name="internator"/>
    /// and their instances in <c>this</c> replaced with interned ones.
    /// </item>
    /// <item>
    /// The method must set <see cref="IInternable.InternCode"/> to a valid value.
    /// </item>
    /// <item>
    /// The method must invoke <see cref="IInternator.Intern{T}(T)"/> on <c>this</c>.
    /// </item>
    /// </list>
    /// </remarks>
    public TSelf Intern(IInternator internator);
}