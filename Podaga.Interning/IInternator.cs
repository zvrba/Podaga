namespace Podaga.Interning;

/// <summary>
/// An instance of this object manages <see cref="IInternable"/> instances, ensuring that only one equal copy is present in
/// the interning hash table.
/// </summary>
public interface IInternator
{
    /// <summary>
    /// Interns <paramref name="internable"/>.
    /// </summary>
    /// <param name="internable">
    /// Instance to intern.  The instance must have its <see cref="IInternable.InternCode"/> set (and thus made immutable).
    /// </param>
    /// <returns>A reference to an instance equal to <paramref name="internable"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="internable"/> has no valid hash code.</exception>
    /// <typeparam name="T">
    /// Type of value being interned.  Restricted to class because a struct would get boxed in the current implementation.
    /// </typeparam>
    T Intern<T>(T internable) where T : class, IInternable;
}

