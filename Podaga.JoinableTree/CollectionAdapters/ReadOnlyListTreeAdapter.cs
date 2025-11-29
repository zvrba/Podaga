using System;
using System.Collections.Generic;

namespace Podaga.JoinableTree.CollectionAdapters;

/// <summary>
/// Adapts a joinable tree to <see cref="IReadOnlyList{T}"/>.  The collection is nevertheless modifiable
/// through the inherited <see cref="ICollection{T}"/> methods.
/// </summary>
/// <typeparam name="T">Collection element type.</typeparam>
public sealed class ReadOnlyListTreeAdapter<T> : CollectionTreeAdapter<T>, IReadOnlyList<T>
{
    /// <inheritdoc/>
    public ReadOnlyListTreeAdapter(TreeJoin<T> join, JoinableTreeNode<T>? root) : base(join, root) { }

    /// <inheritdoc/>
    protected override CollectionTreeAdapter<T> CreateInstance(TreeJoin<T> join, JoinableTreeNode<T>? root) =>
        new ReadOnlyListTreeAdapter<T>(join, root);

    /// <inheritdoc/>
    public new ReadOnlyListTreeAdapter<T> Clone(bool immediate) => (ReadOnlyListTreeAdapter<T>)base.Clone(immediate);

    /// <inheritdoc/>
    public T this[int index] => Root is null ? throw new IndexOutOfRangeException() : Root.Nth(index);
}
