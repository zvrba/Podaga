using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Podaga.JoinableTree;

/// <summary>
/// <para>
/// Provides iterative tree navigation algorithms.  At its core, this struct implements a stack, whereas the actual
/// algorithms are implemented as extension methods on <see cref="TreeIteratorAlgorithms"/> class.  The "current"
/// node being traversed is always on top of the stack.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// WARNING:  This is a mutable struct.
/// </para>
/// <para>
/// WARNING: Iterating over a tree that is simultaneously being modified yields unspecified results.  The modification
/// does not need to be concurrent either, e.g., using <c>Succ</c> or <c>Pred</c> while inserting or deleting elements
/// will also lead to unspecified results.
/// </para>
/// <para>
/// The <see cref="IsNull"/> property on a <c>default</c> instance is true, and
/// no other methods or properties on it may be accessed.  For performance reasons, no methods or properties perform
/// error checking on their own; instead they rely on the runtime throwing <see cref="NullReferenceException"/> or
/// <see cref="IndexOutOfRangeException"/>.
/// </para>
/// <para>
/// Copying of an iterator is shallow: the "original" and its "copy" will share the same underlying stack array.
/// For deep copying, use the copy constructor.
/// </para>
/// </remarks>
/// <typeparam name="T">Value type held by tree nodes.</typeparam>
public struct TreeIterator<T>
{
    /// <summary>
    /// Default capacity allocated by the stack.  This should be sufficient for any balanced tree of up to 4G elements.
    /// </summary>
    public const int DefaultCapacity = 48;

    /// <summary>
    /// Utility method to create a new allocated instance with default capacity.
    /// </summary>
    /// <returns>A new instance with <paramref name="comparer"/> and <see cref="DefaultCapacity"/>.</returns>
    /// <seealso cref="TreeIterator{T}.TreeIterator(IComparer{T}, int)"/>
    public static TreeIterator<T> New(IComparer<T> comparer) => new(comparer, DefaultCapacity);

    /// <summary>
    /// Constructor.  Allocates space for <paramref name="capacity"/> nodes.
    /// </summary>
    /// <param name="comparer">Comparer used to navigate the tree.</param>
    /// <param name="capacity">Maximum capacity supported by this stack.</param>
    public TreeIterator(IComparer<T> comparer, int capacity) {
        Comparer = comparer;
        Path = new JoinableTreeNode<T>[capacity];
        Depth = 0;
    }

    /// <summary>
    /// Copy constructor: copies all fields and the stack array, but not the nodes.  The new
    /// stack has the same capacity as <paramref name="other"/>.
    /// </summary>
    /// <param name="other">
    /// An existing instance from which to initialize <c>this</c>.
    /// </param>
    public TreeIterator(TreeIterator<T> other) {
        Comparer = other.Comparer;
        Depth = other.Depth;
        Path = new JoinableTreeNode<T>[other.Path.Length];
        Array.Copy(other.Path, Path, Depth);
    }

    /// <summary>
    /// Comparer used to navigate the tree.
    /// </summary>
    public readonly IComparer<T> Comparer;

    /// <summary>
    /// Tree traversal stack; root is always at index 0 (bottom) and the current node at <c>Count-1</c>.
    /// </summary>
    /// <seealso cref="Top"/>
    /// <seealso cref="Depth"/>
    public readonly JoinableTreeNode<T>[] Path;

    /// <summary>
    /// Number of elements on the stack.
    /// </summary>
    public int Depth;

    /// <summary>
    /// True when <c>this</c> is <c>default</c> instance.  No methods may be invoked.
    /// </summary>
    public readonly bool IsNull => Path is null;

    /// <summary>
    /// True when there are no elements on the stack.
    /// </summary>
    public readonly bool IsEmpty => Depth == 0;

    /// <summary>
    /// A reference to the top node on the stack which is at index <c>Count - 1</c>.
    /// </summary>
    public readonly ref JoinableTreeNode<T> Top => ref Path[Depth - 1];

    /// <summary>
    /// Removes all elements from the stack.
    /// (NB! Only resets the stack pointer, the contents of <see cref="Path"/> array is unchanged.)
    /// </summary>
    public void Clear() => Depth = 0;

    /// <summary>
    /// Pushes a node onto the stack.  <paramref name="node"/> must not be null (checked only in debug builds).
    /// </summary>
    /// <param name="node"></param>
    public void Push(JoinableTreeNode<T> node) {
        Debug.Assert(node != null);
        Path[Depth++] = node;
    }

    /// <summary>
    /// Attempts to pop the top node from the stack.
    /// </summary>
    /// <returns>
    /// The popped node or <c>null</c> if the stack was empty.
    /// </returns>
    public JoinableTreeNode<T>? TryPop() => Depth > 0 ? Path[--Depth] : null;
}
