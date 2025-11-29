using System;
using System.Collections.Generic;
using System.Threading;

namespace Podaga.JoinableTree;

/// <summary>
/// Abstract base for tree join algorithms.  Implements all methods of <see cref="ITaggedValueComparer{T}"/> and adds
/// methods related to essential join mechanics.
/// </summary>
/// <typeparam name="T">Value type stored in the tree.</typeparam>
public abstract class TreeJoin<T> : ITaggedValueComparer<T>, ICloneable
{
    /// <summary>
    /// Constructor for tagged and/or cloneable values.  If <paramref name="comparer"/> is not an instance of <see cref="ITaggedValueComparer{T}"/>,
    /// it is automatically adapted to one which defines all additional operations (including value cloning) as no-ops.
    /// </summary>
    /// <param name="comparer">Comparer implementation.</param>
    protected TreeJoin(IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        Comparer = comparer as ITaggedValueComparer<T> ?? new TaggedValueComparerAdapter(comparer);
    }

    private sealed class TaggedValueComparerAdapter(IComparer<T> comparer) : ITaggedValueComparer<T>
    {
        public int Compare(T? x, T? y) => comparer.Compare(x, y);
        public T MZero => default!;
        public void MPlus(T left, ref T result, T right) { }
        public T Clone(T value) => value;
    }

    /// <summary>
    /// Comparer used for comparing values and manipulating tags.  This is the value passed to ctor.
    /// </summary>
    public ITaggedValueComparer<T> Comparer { get; }

    #region ITaggedValueComparer

    /// <summary>
    /// Delegates to <see cref="Comparer"/>.
    /// </summary>
    public int Compare(T? x, T? y) => Comparer.Compare(x, y);

    /// <summary>
    /// Delegates to <see cref="Comparer"/>.
    /// </summary>
    public T MZero => Comparer.MZero;

    /// <summary>
    /// Delegates to <see cref="Comparer"/>.
    /// </summary>
    public void MPlus(T left, ref T result, T right) => Comparer.MPlus(left, ref result, right);

    /// <summary>
    /// Delegates to <see cref="Comparer"/>.
    /// </summary>
    public T Clone(T value) => Comparer.Clone(value);

    #endregion

    /// <summary>
    /// Clones <c>this</c>.
    /// </summary>
    /// <returns>
    /// A new instance of the same concrete type as <c>this</c> and using the same comparer.
    /// </returns>
    public abstract TreeJoin<T> Clone();

    /// <summary>
    /// Delegates to <see cref="Clone()"/>.
    /// </summary>
    object ICloneable.Clone() => Clone();

    /// <summary>
    /// 3-way join is the core tree algorithm on which all other operations are based.
    /// When the tree is not persistent, this operation is destructive to all inputs.
    /// </summary>
    /// <param name="jd">Join parameters.  All fields must be initialized.</param>
    /// <returns>
    /// Tree that has same entries and inorder traversal as the node <c>(left, middle, right)</c>.
    /// </returns>
    public abstract JoinableTreeNode<T> Join(Section jd);

    /// <summary>
    /// Zero rank, passed to <see cref="RPlus(int, int)"/> when the corresponding node is <c>null</c>.
    /// Default value is 0, but may be set by the protected class' constructor.
    /// </summary>
    public int RZero { get; protected set; }

    /// <summary>
    /// Computes <c>left + right</c> for the node's rank.  The operation must respect <see cref="RZero"/> as the neutral element.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="MPlus(T, ref T, T)"/>, this method does not need the "middle" element because a single node has a
    /// fixed rank.
    /// </remarks>
    /// <returns>Computed result</returns>
    public abstract int RPlus(int left, int right);

    /// <summary>
    /// This method must validate the tree's structure invariant starting from <paramref name="node"/>.
    /// Mainly for use in stress-tests.
    /// </summary>
    /// <param name="node">Node from which to begin validation.</param>
    /// <exception cref="NotImplementedException">Thrown when a violation of the structure invariant is detected.</exception>
    public abstract void ValidateStructure(JoinableTreeNode<T>? node);

    /// <summary>
    /// Describes a "section" of a tree.
    /// </summary>
    public struct Section
    {
        /// <summary>
        /// Left subtree.
        /// </summary>
        public JoinableTreeNode<T>? Left;

        /// <summary>
        /// The middle node.
        /// </summary>
        public JoinableTreeNode<T>? Middle;

        /// <summary>
        /// Right subtree.
        /// </summary>
        public JoinableTreeNode<T>? Right;
    }

    /// <summary>
    /// Splits a tree rooted at <paramref name="node"/> into left and right subtrees 
    /// holding respectively values less than and greater than <paramref name="value"/>.
    /// </summary>
    /// <param name="node">Tree root.</param>
    /// <param name="value">Value used for splitting.</param>
    /// <returns>
    /// A structure containing the left and right subtrees and a flag indicating whether <paramref name="value"/> was
    /// found in the tree under <paramref name="node"/>.
    /// </returns>
    public Section Split(JoinableTreeNode<T>? node, T value)
    {
        if (node == null)
            return default;
        
        var c = Comparer.Compare(value, node.Value);
        if (c == 0)
            return new() { Left = node.Left, Middle = node, Right = node.Right };

        if (c < 0) {
            var s = Split(node.Left, value);
            var jd = new Section { Left = s.Right, Middle = node, Right = node.Right };
            var j = Join(jd);
            return new() { Left = s.Left, Middle = s.Middle, Right = j };
        } else {
            var s = Split(node.Right, value);
            var jd = new Section { Left = node.Left, Middle = node, Right = s.Left };
            var j = Join(jd);
            return new() { Left = j, Middle = s.Middle, Right = s.Right };
        }
    }

    /// <summary>
    /// Clones the middle node anda ttaches left and right nodes as its left and right children.
    /// This method assumes that the middle node is not null and that the result will be properly balanced.
    /// </summary>
    /// <returns>
    /// The updated (possibly cloned) middle node.
    /// </returns>
    public JoinableTreeNode<T> JoinBalanced(Section section)
    {
        var m = section.Middle!.Clone(this);
        m.Left = section.Left;
        m.Right = section.Right;
        m.Update(this);
        return m;
    }

    /// <summary>
    /// Joins left and right balanced subtrees into a single balanced tree.
    /// </summary>
    /// <returns>A root of the joined tree.</returns>
    public JoinableTreeNode<T>? Join2(Section section) {
        if (section.Left is null)
            return section.Right;
        section.Left = SplitLast(section.Left, ref section);
        return Join(section);
    }

    // Sets Middle to the rightmost value.
    private JoinableTreeNode<T>? SplitLast(JoinableTreeNode<T> node, ref Section section) {
        if (node.Right == null) {
            section.Middle = node;  // XXX: Doesn't seem to be used???
            return node.Left;
        }
        var n = SplitLast(node.Right, ref section);
        var jd1 = new Section { Left = node.Left, Middle = node, Right = n };
        return Join(jd1);
    }
}
