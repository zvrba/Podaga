using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Podaga.JoinableTree.CollectionAdapters;

/// <summary>
/// Adapts a joinable tree to <see cref="ICollection{T}"/>.
/// </summary>
/// <remarks>
/// This class does not implement <see cref="IReadOnlyList{T}"/> because it would clash with the dictionary's index
/// when the dictionary's key is <c>int</c>.
/// </remarks>
/// <typeparam name="T">Collection element type.</typeparam>
public class CollectionTreeAdapter<T> :
    ICloneable,
    IAdaptedJoinableTree<T>,
    ICollection<T>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="join">Join strategy.</param>
    /// <param name="root">Optional tree root; used to initialize the collection with existing content.</param>
    public CollectionTreeAdapter(TreeJoin<T> join, JoinableTreeNode<T>? root)
    {
        Transient = join;
        Root = root;
    }

    /// <summary>
    /// Clones the collection, ensuring that modifications to <c>this</c> and the forked version are invisible to each other.
    /// Values are cloned as determined by <see cref="Transient"/>.
    /// </summary>
    /// <param name="immediate">
    /// If true, all nodes are copied into the new instance immediately.  Otherwise, nodes are copied only upon modification.
    /// </param>
    /// <returns>
    /// A cloned instance that contains the same elements as <c>this</c>.
    /// </returns>
    public CollectionTreeAdapter<T> Clone(bool immediate)
    {
        // If this.Transient isn't cloned, then changes to this are visible to the clone.
        Transient = Transient.Clone();
        var j = Transient.Clone();
        var f = Root;
        if (immediate && f != null)
            f = j.Copy(f);
        return CreateInstance(j, f);
    }

    /// <summary>
    /// Invokes <c>Clone(false)</c> and returns the result.
    /// </summary>
    object ICloneable.Clone() => Clone(false);

    /// <summary>
    /// Used by <see cref="Clone(bool)"/> to create a new instance of the same concrete type as <c>this</c>.
    /// </summary>
    protected virtual CollectionTreeAdapter<T> CreateInstance(TreeJoin<T> join, JoinableTreeNode<T>? root) => new(join, root);

    /// <inheritdoc/>
    public TreeJoin<T> Transient { get; private set; }

    /// <inheritdoc/>
    public JoinableTreeNode<T>? Root { get; protected set; }

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public int Count => Root?.Size ?? 0;


    /// <inheritdoc/>
    void ICollection<T>.Add(T item) => Add(item);

    /// <summary>
    /// Adds an item to <c>this</c>.
    /// </summary>
    /// <param name="item">Item to add.</param>
    /// <returns>
    /// True if the item was added, false if it already exists in this collection.
    /// </returns>
    public bool Add(T item) {
        var state = new TreeModifyState<T> { Value = item };
        Root = Transient.Insert(Root, ref state);
        return state.Found == null;
    }

    /// <inheritdoc/>
    public void Clear() => Root = null;

    /// <inheritdoc/>
    public bool Contains(T item) => Transient.Find(Root, item, out var found) != null && found == 0;

    /// <inheritdoc/>
    public bool Remove(T item) {
        var state = new TreeModifyState<T> { Value = item };
        var root = Transient.Delete(Root, ref state);
        if (state.Found == null)
            return false;
        Root = root;
        return true;
    }

    internal void CheckCopyLength(Array array, int arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        if (arrayIndex < 0 || arrayIndex >= array.Length)
            throw new IndexOutOfRangeException();
        if (arrayIndex + Count > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex) {
        CheckCopyLength(array, arrayIndex);
        foreach (var item in this)
            array[arrayIndex++] = item;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => Root is null ? Enumerable.Empty<T>().GetEnumerator() : Root.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
