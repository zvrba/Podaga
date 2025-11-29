using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Podaga.JoinableTree.CollectionAdapters;

/// <summary>
/// Adapts a joinable tree to <see cref="IDictionary{TKey, TValue}"/> and <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">Dictionary key type.</typeparam>
/// <typeparam name="TValue">Dictionary value type.</typeparam>
public sealed class DictionaryTreeAdapter<TKey, TValue> :
    CollectionTreeAdapter<KeyValuePair<TKey, TValue>>,
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>
{
    /// <inheritdoc/>
    public DictionaryTreeAdapter(TreeJoin<KeyValuePair<TKey, TValue>> join, JoinableTreeNode<KeyValuePair<TKey, TValue>>? root)
        : base(join, root) { }

    /// <inheritdoc/>
    protected override CollectionTreeAdapter<KeyValuePair<TKey, TValue>> CreateInstance(TreeJoin<KeyValuePair<TKey, TValue>> join, JoinableTreeNode<KeyValuePair<TKey, TValue>>? root) =>
        new DictionaryTreeAdapter<TKey, TValue>(join, root);

    /// <inheritdoc/>
    public new DictionaryTreeAdapter<TKey, TValue> Clone(bool immediate) => (DictionaryTreeAdapter<TKey, TValue>)base.Clone(immediate);

    /// <inheritdoc/>
    public TValue this[TKey key] {
        get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"Key `{key}` was not found in the dictionary.");
        set {
            var kv = new KeyValuePair<TKey, TValue>(key, value);
            var n = Transient.Find(Root, kv, out var found);
            if (n != null && found == 0) n.Value = kv;
            else Add(kv);
        }
    }

    /// <inheritdoc/>
    public ICollection<TKey> Keys => new KeyView(this);
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc/>
    public ICollection<TValue> Values => new ValueView(this);
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <inheritdoc/>
    public void Add(TKey key, TValue value) {
        if (!Add(new KeyValuePair<TKey, TValue>(key, value)))
            throw new ArgumentException($"Key `{key}` already exists in the dictionary.");
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => Contains(new KeyValuePair<TKey, TValue>(key, default!));

    /// <inheritdoc/>
    public bool Remove(TKey key) => Remove(new KeyValuePair<TKey, TValue>(key, default!));

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
        var n = Transient.Find(Root, new KeyValuePair<TKey, TValue>(key, default!), out var found);
        if (n != null && found == 0) {
            value = n.Value.Value;
            return true;
        }
        value = default;
        return false;
    }

    private interface IView<TSelf, T> : ICollection<T> where TSelf : IView<TSelf, T>
    {
        private static readonly Exception ReadonlyExn = new NotSupportedException($"{typeof(IView<TSelf, T>).FullName} is read-only.");

        abstract static T Extract(KeyValuePair<TKey, TValue> kv);
        DictionaryTreeAdapter<TKey, TValue> Base { get; }

        int ICollection<T>.Count => Base.Count;
        bool ICollection<T>.IsReadOnly => true;

        void ICollection<T>.Add(T item) => throw ReadonlyExn;
        void ICollection<T>.Clear() => throw ReadonlyExn;
        bool ICollection<T>.Remove(T item) => throw ReadonlyExn;

        bool ICollection<T>.Contains(T item) => Base.Select(TSelf.Extract).Contains(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
            Base.CheckCopyLength(array, arrayIndex);
            foreach (var item in Base)
                array[arrayIndex++] = TSelf.Extract(item);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Base.Select(TSelf.Extract).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // Class instead of struct because we're returning ICollection<T>, so it gets boxed anyway.

    private class KeyView : IView<KeyView, TKey>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TKey Extract(KeyValuePair<TKey, TValue> kv) => kv.Key;

        internal KeyView(DictionaryTreeAdapter<TKey, TValue> @base) => Base = @base;

        public DictionaryTreeAdapter<TKey, TValue> Base { get; }
    }

    private class ValueView : IView<ValueView, TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Extract(KeyValuePair<TKey, TValue> kv) => kv.Value;

        internal ValueView(DictionaryTreeAdapter<TKey, TValue> @base) => Base = @base;

        public DictionaryTreeAdapter<TKey, TValue> Base { get; }
    }
}
