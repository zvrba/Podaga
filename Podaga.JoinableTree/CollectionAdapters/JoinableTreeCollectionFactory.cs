using System;
using System.Collections.Generic;

namespace Podaga.JoinableTree.CollectionAdapters;

internal static class DictionaryComparerAdapters
{
    private sealed class KVPComparerAdapter<TKey, TValue>(IComparer<TKey> comparer) : ITaggedValueComparer<KeyValuePair<TKey, TValue>>
    {
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => comparer.Compare(x.Key, y.Key);
        public KeyValuePair<TKey, TValue> MZero => default;
        public void MPlus(KeyValuePair<TKey, TValue> left, ref KeyValuePair<TKey, TValue> result, KeyValuePair<TKey, TValue> right) { }
        public KeyValuePair<TKey, TValue> Clone(KeyValuePair<TKey, TValue> value) => value;
    }

    private sealed class KVPTaggedComparerAdapter<TKey, TValue>(ITaggedValueComparer<TKey> comparer) : ITaggedValueComparer<KeyValuePair<TKey, TValue>>
    {
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => comparer.Compare(x.Key, y.Key);
        public KeyValuePair<TKey, TValue> MZero => new(comparer.MZero, default!);
        public void MPlus(KeyValuePair<TKey, TValue> left, ref KeyValuePair<TKey, TValue> result, KeyValuePair<TKey, TValue> right)
        {
            TKey tmp = result.Key;
            comparer.MPlus(left.Key, ref tmp, right.Key);
            result = new(tmp, result.Value);
        }
        public KeyValuePair<TKey, TValue> Clone(KeyValuePair<TKey, TValue> value) => new(comparer.Clone(value.Key), value.Value);
    }

    internal static ITaggedValueComparer<KeyValuePair<TKey, TValue>> Adapt<TKey, TValue>(object comparer) =>
        comparer switch {
            ITaggedValueComparer<KeyValuePair<TKey, TValue>> dictcmp => dictcmp,
            ITaggedValueComparer<TKey> keycmp => new KVPTaggedComparerAdapter<TKey, TValue>(keycmp),
            IComparer<TKey> keycmp => new KVPComparerAdapter<TKey, TValue>(keycmp),
            _ => throw new NotSupportedException($"Object of type {comparer.GetType()} cannot be adapted to a tree dictionary comparer.")
        };
}

/// <summary>
/// Utility methods for creating collections backed by an AVL joinable tree.
/// </summary>
/// <remarks>
/// These extension methods can automatically adapt <see cref="IComparer{T}"/> to <see cref="ITaggedValueComparer{T}"/>.
/// The adaptated comparer defines all additional operations (including value cloning) as no-ops.
/// </remarks>
public static class JoinableAvlTreeCollectionFactory
{
    // TODO: Handle null for comparer: Comparer<T>.Default!

    extension<T>(AvlJoin<T>)
    {
        /// <summary>
        /// Creates a new <see cref="ICollection{T}"/> backed by an AVL tree using <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">An <see cref="IComparer{T}"/> (automatically adapted) or <see cref="ITaggedValueComparer{T}"/> instance (used as is).</param>
        public static CollectionTreeAdapter<T> NewCollection(IComparer<T> comparer) =>
            new(new AvlJoin<T>(comparer), null);

        /// <summary>
        /// Creates a new empty <see cref="ISet{T}"/> backed by an AVL tree using <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">An <see cref="IComparer{T}"/> (automatically adapted) or <see cref="ITaggedValueComparer{T}"/> instance (used as is).</param>
        public static SetTreeAdapter<T> NewSet(IComparer<T> comparer) =>
            new(new AvlJoin<T>(comparer), null);

        /// <summary>
        /// Creates a new <see cref="IDictionary{TKey, TValue}"/> backed by an AVL tree using <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">
        /// An instance of <c>ITaggedValueComparer{KeyValuePair{TKey, TValue}}</c> (used as is), or <c>ITaggedValueComparer{TKey}</c>
        /// or <c>IComparer{TKey}</c> (the latter two being automatically adapted).
        /// </param>
        public static DictionaryTreeAdapter<TKey, TValue> NewDictionary<TKey, TValue>(object comparer)
            => new(new AvlJoin<KeyValuePair<TKey, TValue>>(DictionaryComparerAdapters.Adapt<TKey, TValue>(comparer)), null);
    }
}

/// <summary>
/// Utility methods for creating collections backed by a WB joinable tree.
/// </summary>
/// <remarks>
/// These extension methods can automatically adapt <see cref="IComparer{T}"/> to <see cref="ITaggedValueComparer{T}"/>.
/// The adaptated comparer defines all additional operations (including value cloning) as no-ops.
/// </remarks>
public static class JoinableWBTreeCollectionFactory
{
    extension<T>(WBJoin<T>)
    {
        /// <summary>
        /// Creates a new <see cref="ICollection{T}"/> backed by a WB tree using <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">An <see cref="IComparer{T}"/> (automatically adapted) or <see cref="ITaggedValueComparer{T}"/> instance (used as is).</param>
        public static CollectionTreeAdapter<T> NewCollection(IComparer<T> comparer) =>
            new(new WBJoin<T>(comparer), null);

        /// <summary>
        /// Creates a new empty <see cref="ISet{T}"/> backed by a WB tree using <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">An <see cref="IComparer{T}"/> (automatically adapted) or <see cref="ITaggedValueComparer{T}"/> instance (used as is).</param>
        public static SetTreeAdapter<T> NewSet(IComparer<T> comparer) =>
            new(new WBJoin<T>(comparer), null);

        /// <summary>
        /// Creates a new <see cref="IDictionary{TKey, TValue}"/> backed by a WB tree using <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">
        /// An instance of <c>ITaggedValueComparer{KeyValuePair{TKey, TValue}}</c> (used as is), or <c>ITaggedValueComparer{TKey}</c>
        /// or <c>IComparer{TKey}</c> (the latter two being automatically adapted).
        /// </param>
        public static DictionaryTreeAdapter<TKey, TValue> NewDictionary<TKey, TValue>(IComparer<TKey> comparer)
            => new(new WBJoin<KeyValuePair<TKey, TValue>>(DictionaryComparerAdapters.Adapt<TKey, TValue>(comparer)), null);
    }
}
