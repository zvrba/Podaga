using Podaga.JoinableTree.CollectionAdapters;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Podaga.JoinableTree;

/// <summary>
/// Node of a binary search tree; stores a tagged value.
/// Enumerating a node will return its children in in-order traversal.
/// </summary>
/// <remarks>
/// This class implements <see cref="IAdaptedJoinableTree{T}"/> explicitly which allows direct use of conversion methods
/// from <see cref="AdaptedJoinableTreeExtensions"/>.
/// </remarks>
/// <typeparam name="T">Value type stored in the tree.</typeparam>
public sealed class JoinableTreeNode<T> : IEnumerable<T>, IAdaptedJoinableTree<T>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="transient">Transient tag.</param>
    public JoinableTreeNode(TreeJoin<T> transient) => Transient = transient;

    /// <summary>
    /// The transient tag.
    /// </summary>
    public readonly TreeJoin<T> Transient;

    /// <summary>
    /// Tagged value contained in the node.
    /// </summary>
    [AllowNull]
    public T Value = default!;

    /// <summary>
    /// Left child, with key less than <see cref="Value"/>.
    /// </summary>
    public JoinableTreeNode<T>? Left;

    /// <summary>
    /// Right child, with key larger than <see cref="Value"/>.
    /// </summary>
    public JoinableTreeNode<T>? Right;

    /// <summary>
    /// Count of the nodes under, and including, this node.
    /// </summary>
    public int Size;

    /// <summary>
    /// Rank; needed by some tree implementations.
    /// </summary>
    public int Rank;

    /// <summary>
    /// Delegates to <see cref="Transient"/>.
    /// </summary>
    TreeJoin<T> IAdaptedJoinableTree<T>.Transient => Transient;

    /// <summary>
    /// Returns <c>this</c>.
    /// </summary>
    JoinableTreeNode<T>? IAdaptedJoinableTree<T>.Root => this;

    /// <summary>
    /// Conditionally clones <c>this</c>, i.e., when this' transient tag is different from the one in <paramref name="join"/>.
    /// </summary>
    /// <param name="join">Tree join instance used as reference transient value.</param>
    /// <returns>
    /// New instance or <c>this</c>, depending on the transient tag.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining /*| MethodImplOptions.AggressiveOptimization*/)]
    public JoinableTreeNode<T> Clone(TreeJoin<T> join)
        => join == Transient 
        ? this 
        : new(join) { Value = join.Clone(Value), Left = Left, Right = Right, Size = Size, Rank = Rank };

    /// <summary>
    /// Updates <c>this</c> node's tag and rank.
    /// WARNING: The update is in-place, so the node must have been cloned beforehand.
    /// </summary>
    /// <param name="join">Tree join strategy.</param>
    //[MethodImpl(MethodImplOptions.AggressiveInlining /*| MethodImplOptions.AggressiveOptimization*/)]
    public void Update(TreeJoin<T> join)
    {
        if (Left != null && Right != null) {
            Size = 1 + Left.Size + Right.Size;
            Rank = join.RPlus(Left.Rank, Right.Rank);
            join.MPlus(Left.Value, ref Value, Right.Value);
        }
        else if (Left != null) {
            Size = 1 + Left.Size;
            Rank = join.RPlus(Left.Rank, join.RZero);
            join.MPlus(Left.Value, ref Value, join.MZero);
        }
        else if (Right != null) {
            Size = 1 + Right.Size;
            Rank = join.RPlus(join.RZero, Right.Rank);
            join.MPlus(join.MZero, ref Value, Right.Value);
        }
        else {
            Size = 1;
            Rank = join.RPlus(join.RZero, join.RZero);
            // MZero is neutral element, we don't need to update the tag.
        }
    }

    /// <summary>
    /// Returns the n'th element in sorted order in the tree rooted at <c>this</c>.
    /// This method is more efficient than the equivalent iterator methods.
    /// </summary>
    /// <param name="index">Order of the element to retrieve.</param>
    /// <returns>The found element.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="index"/> is outside of range <c>[0, Size-1)</c>, size being the size of the subtree.
    /// </exception>
    public T Nth(int index)
    {
        if (index < 0 || index >= Size)
            throw new IndexOutOfRangeException("Invalid tree element index.");

        var node = this;
        ++index;    // Makes calculations easier.

    loop:
        var l = node.Left is null ? 0 : node.Left.Size;
        if (index == l + 1)
            return node.Value;
        if (index <= l) {
            node = node.Left!;
        } else {
            node = node.Right!;
            index -= l + 1;
        }
        goto loop;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() {
        var it = Transient.GetIterator(this);
        it.First();
        do {
            yield return it.Top.Value;
        } while (it.Succ());
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
