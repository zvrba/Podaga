using System;
using System.Collections.Generic;
using System.Linq;

namespace Podaga.JoinableTree.CollectionAdapters;

/// <summary>
/// Adapts a joinable tree to <see cref="ISet{T}"/> and <see cref="IReadOnlySet{T}"/>.
/// </summary>
/// <remarks>
/// Implementations of set operations check whether the <see cref="IEnumerable{T}"/> argument is actually an instance of
/// <see cref="JoinableTreeNode{T}"/> or <see cref="IAdaptedJoinableTree{T}"/>.  If so, a more efficient join-based recursive
/// strategy is used.  Otherwise, the operation is performed element-wise.  In particular, <see cref="IntersectWith(IEnumerable{T})"/>
/// allocates temporary storage in size proportional with the result.
/// </remarks>
/// <typeparam name="T">Collection element type.</typeparam>
public sealed class SetTreeAdapter<T> : CollectionTreeAdapter<T>, IReadOnlySet<T>, ISet<T>
{
    /// <inheritdoc/>
    public SetTreeAdapter(TreeJoin<T> join, JoinableTreeNode<T>? root) : base(join, root) { }

    /// <inheritdoc/>
    protected override CollectionTreeAdapter<T> CreateInstance(TreeJoin<T> join, JoinableTreeNode<T>? root) =>
        new SetTreeAdapter<T>(join, root);

    /// <inheritdoc/>
    public new SetTreeAdapter<T> Clone(bool immediate) => (SetTreeAdapter<T>)base.Clone(immediate);

    /// <inheritdoc/>
    public bool SetEquals(IEnumerable<T> other) => other switch {
        JoinableTreeNode<T> n => Transient.SetEqual(Root, n),
        IAdaptedJoinableTree<T> t => Transient.SetEqual(Root, t.Root),
        _ => other.Count() == Count && other.Count(Contains) == Count,
    };

    /// <inheritdoc/>
    public bool IsSubsetOf(IEnumerable<T> other) => Count == 0 || other.Count(Contains) == Count;

    /// <inheritdoc/>
    public bool IsProperSubsetOf(IEnumerable<T> other) => IsSubsetOf(other) && other.Count() > Count;

    /// <inheritdoc/>
    public bool IsSupersetOf(IEnumerable<T> other) => other.Count() == 0 || other.All(Contains);

    /// <inheritdoc/>
    public bool IsProperSupersetOf(IEnumerable<T> other) => IsSupersetOf(other) && Count > other.Count();

    /// <inheritdoc/>
    public bool Overlaps(IEnumerable<T> other) => other.Any(Contains);

    /// <inheritdoc/>
    public void UnionWith(IEnumerable<T> other) {
        switch (other) {
            case JoinableTreeNode<T> n:
                Root = Transient.SetUnion(Root, n);
                break;
            case IAdaptedJoinableTree<T> t:
                Root = Transient.SetUnion(Root, t.Root);
                break;
            default:
                foreach (var v in other)
                    Add(v);
                break;
        }
    }

    /// <inheritdoc/>
    public void IntersectWith(IEnumerable<T> other) {
        switch (other) {
            case JoinableTreeNode<T> n:
                Root = Transient.SetIntersection(Root, n);
                break;
            case IAdaptedJoinableTree<T> t:
                Root = Transient.SetIntersection(Root, t.Root);
                break;
            default:
                var isect = other.Where(Contains).ToList();
                Clear();
                foreach (var x in isect)
                    Add(x);
                break;
        }
    }

    /// <inheritdoc/>
    public void ExceptWith(IEnumerable<T> other) {
        switch (other) {
            case JoinableTreeNode<T> n:
                Root = Transient.SetDifference(Root, n);
                break;
            case IAdaptedJoinableTree<T> t:
                Root = Transient.SetDifference(Root, t.Root);
                break;
            default:
                foreach (var x in other)
                    Remove(x);
                break;
        }
    }

    /// <inheritdoc/>
    public void SymmetricExceptWith(IEnumerable<T> other) {
        foreach (var x in other) {
            if (Contains(x)) Remove(x);
            else Add(x);
        }
    }
}
