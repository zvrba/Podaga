using System;
using System.Collections.Generic;

namespace Podaga.JoinableTree;

/// <summary>
/// Any value stored in the tree must implement this interface.
/// </summary>
/// <typeparam name="T">
/// Value stored in the tree; consists of a mutable "tag" (augmentation) part and an immutable value part used for comparisons.
/// </typeparam>
public interface ITaggedValueComparer<T> : IComparer<T>
{
    /// <summary>
    /// Value corresponding to <c>null</c> node; used as left or right argument to <see cref="MPlus(T, ref T, T)"/>.
    /// This must be neutral element of the monoid implemented by <see cref="MPlus(T, ref T, T)"/>.
    /// </summary>
    T MZero { get; }

    /// <summary>
    /// <para>
    /// Computes <c>result = left + result + right</c> on the TAG part of <typeparamref name="T"/>.
    /// </para>
    /// <para>
    /// A correct implementation obeys monoidal laws with <see cref="MZero"/> as the neutral element.
    /// </para>
    /// </summary>
    /// <param name="left">Value corresponding to the left branch.  <see cref="MZero"/> if there is no left branch.</param>
    /// <param name="result">Value corresponding to the current node.</param>
    /// <param name="right">Value corresponding to the right branch.  <see cref="MZero"/> if there is no right branch.</param>
    void MPlus(T left, ref T result, T right);

    /// <summary>
    /// Clones <paramref name="value"/> such that the tag part is safe to mutate independently of the original value.
    /// Optionally, the whole value may be cloned.
    /// </summary>
    /// <param name="value">Value to clone.</param>
    /// <returns>
    /// The cloned value.
    /// </returns>
    T Clone(T value);
}
