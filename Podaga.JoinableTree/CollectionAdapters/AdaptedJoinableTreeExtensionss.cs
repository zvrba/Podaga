using System;
using System.Collections.Generic;
using System.Text;

namespace Podaga.JoinableTree.CollectionAdapters;

/// <summary>
/// <para>
/// Utility methods for converting between different "views" of the same joinable tree.  The conversions are O(1) and the converted
/// collection operates on the same underlying tree as the original collection.
/// </para>
/// <para>
/// Note that the methods are also applicable to <see cref="JoinableTreeNode{T}"/>.
/// </para>
/// </summary>
public static class AdaptedJoinableTreeExtensions
{
    extension<T>(IAdaptedJoinableTree<T> @this)
    {
        /// <summary>
        /// Returns the view of <c>this</c> as a readonly list.
        /// </summary>
        public ReadOnlyListTreeAdapter<T> AsReadOnlyList() => new(@this.Transient, @this.Root);

        /// <summary>
        /// Returns the view of <c>this</c> as a collection.
        /// </summary>
        public CollectionTreeAdapter<T> AsCollection() => new(@this.Transient, @this.Root);

        /// <summary>
        /// Returns the view of <c>this</c> as a set.
        /// </summary>
        public SetTreeAdapter<T> AsSet() => new(@this.Transient, @this.Root);

        // TODO: AsDictionary<TKey, TValue>(Func<T, KeyValuePari<TKey, TValue>>) + BULK INSERT!

        /// <summary>
        /// Non-destructive union of <c>this</c> and <paramref name="other"/>.
        /// </summary>
        /// <returns>
        /// A new set with the result.
        /// </returns>
        public SetTreeAdapter<T> SetUnion(IEnumerable<T> other)
        {
            var ret = new SetTreeAdapter<T>(@this.Transient.Clone(), @this.Root);
            ret.UnionWith(other);
            return ret;
        }

        /// <summary>
        /// Non-destructive intersection of <c>this</c> and <paramref name="other"/>.
        /// </summary>
        /// <returns>
        /// A new set with the result.
        /// </returns>
        public SetTreeAdapter<T> SetIntersection(IEnumerable<T> other)
        {
            var ret = new SetTreeAdapter<T>(@this.Transient.Clone(), @this.Root);
            ret.IntersectWith(other);
            return ret;
        }

        /// <summary>
        /// Non-destructive difference of <c>this</c> and <paramref name="other"/>.
        /// Difference is a non-commutative operation and <paramref name="this"/> is treated as the "left" argument, i.e.,
        /// the resulting set wil contain no element present in <paramref name="other"/>.
        /// </summary>
        /// <returns>
        /// A new set with the result.
        /// </returns>
        public SetTreeAdapter<T> SetDifference(IEnumerable<T> other)
        {
            var ret = new SetTreeAdapter<T>(@this.Transient.Clone(), @this.Root);
            ret.ExceptWith(other);
            return ret;
        }
    }
}
