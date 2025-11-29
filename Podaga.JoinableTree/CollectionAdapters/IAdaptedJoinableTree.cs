using System.Collections.Generic;

namespace Podaga.JoinableTree.CollectionAdapters;

/// <summary>
/// Common interface implemented by all adapters to <c>System.Collection.Generic</c> interfaces.
/// </summary>
/// <typeparam name="T">Value type held by the tree.</typeparam>
public interface IAdaptedJoinableTree<T>
{
    /// <summary>
    /// Transient tag to use by the collection.
    /// </summary>
    TreeJoin<T> Transient { get; }

    /// <summary>
    /// Root of the collection's underlying tree.  <c>null</c> for empty collection.
    /// </summary>
    JoinableTreeNode<T>? Root { get; }
}
