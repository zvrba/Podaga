using System;
using System.Collections.Generic;
using Podaga.JoinableTree;
using Podaga.JoinableTree.CollectionAdapters;

namespace Podaga.Test;

internal class TreeSet_BasicTest
{
    public static void Run(TreeJoin<int> join, IList<int[]> sequences) {
        for (int i = 0; i < sequences.Count; ++i) {
            for (int j = 0; j < sequences.Count; ++j) {
                var test = new TreeSet_BasicTest(join, sequences[i], sequences[j]);
                test.Run();
            }
        }
    }

    private readonly TreeJoin<int> join;
    private readonly int[] insert;
    private readonly int[] remove;
    private readonly SortedSet<int> contents;
    private JoinableTreeNode<int>? tree;

    private TreeSet_BasicTest(TreeJoin<int> join, int[] insert, int[] remove)
    {
        this.join = join;
        this.insert = insert;
        this.remove = remove;
        this.contents = new();
    }

    public void Run() {
        Assert.True(tree is null);

        // TODO: Traversal during modifications.  Also Fork().
        CheckInsert();
        CheckIndexAccess();
        CheckDelete();

        Assert.True(tree is null);

        CheckDeleteRoot();
        CheckPersistence();
    }

    private void CheckInsert() {
        bool b;
        for (int i = 0; i < insert.Length; ++i) {
            b = Insert(insert[i]);
            Assert.True(b);
            Verify();
        }
        int v = insert.Length / 2;
        b = TryAdd(ref v);
        Assert.True(!b && v == insert.Length / 2);
    }

    private void CheckIndexAccess() {
        for (int i = 0; i < insert.Length; ++i) {
            var j = tree!.Nth(i);
            Assert.True(i == j);
        }
    }

    private void CheckDelete() {
        bool b;
        for (int i = 0; i < remove.Length; ++i) {
            b = Remove(remove[i]);
            Assert.True(b);
            Verify();
        }
    }

    // Deleting a root node is a special case.
    private void CheckDeleteRoot() {
        Assert.True(tree == null);
        for (int i = 0; i < insert.Length; ++i)
            Insert(insert[i]);
        Assert.True(tree!.Size == insert.Length);


        while (tree != null) {
            var b = Remove(tree.Value);
            Assert.True(b);
            Verify();
        }
        Assert.True(tree == null);
    }

    private void CheckPersistence() {
        var original = new CollectionTreeAdapter<int>(join.Clone(), tree);
        for (int i = 0; i < insert.Length; ++i)
            original.Add(insert[i]);
        Assert.True(original.Count == insert.Length);

        var copy = original.Clone(false);
        for (int i = 0; i < remove.Length; ++i) {
            original.Remove(remove[i]);
            Assert.True(copy.Count == insert.Length);
            for (int j = 0; j < insert.Length; ++j)
                Assert.True(copy.Contains(insert[j]));
        }
        Assert.True(original.Count == 0);
    }

    private bool TryAdd(ref int v) {
        var state = new TreeModifyState<int> { Value = v };
        tree = join.Insert(tree, ref state);
        if (state.Found == null)
            return true;
        v = state.Found.Value;
        return false;
    }

    private bool Insert(int v) {
        var original = v;
        if (TryAdd(ref v)) {
            Assert.True(v == original);
            contents.Add(v);
            return true;
        }
        return false;
    }

    private bool TryRemove(ref int v) {
        var state = new TreeModifyState<int> { Value = v };
        var root = join.Delete(tree, ref state);
        if (state.Found == null)
            return false;

        v = state.Found.Value;
        tree = root;
        return true;
    }

    private bool Remove(int v) {
        var removed = v;
        if (!TryRemove(ref removed))
            return false;
        Assert.True(removed == v);
        contents.Remove(v);
        return true;
    }

    private void Verify() {
        Assert.True((tree?.Size ?? 0) == contents.Count);
        join.ValidateStructure(tree);

        VerifyOrder(tree, out var traverseCount, contents.Min, contents.Max);
        Assert.True(traverseCount == (tree?.Size ?? 0));

        foreach (var i in contents) {
            var n = join.Find(tree, i, out int found);
            Assert.True(found == 0 && n!.Value == i);
        }

        var iterator = join.GetIterator(tree);
        iterator.First();
        VerifyIteration(iterator, false, contents);

        iterator = join.GetIterator(tree);
        iterator.Last();
        VerifyIteration(iterator, true, contents.Reverse());
    }

    private static void VerifyOrder(JoinableTreeNode<int>? node, out int count, int min, int max) {
        if (node == null) {
            count = 0;
            return;
        }

        Assert.True(min <= max);
        Assert.True(node.Value >= min);
        Assert.True(node.Value <= max);

        VerifyOrder(node.Left, out var lc, min, node.Value - 1);
        VerifyOrder(node.Right, out var rc, node.Value + 1, max);
        count = lc + rc + 1;
    }

    //private delegate TNode Advance(ref Iterator<int, TNode> iterator);

    private static void VerifyIteration(
        TreeIterator<int> iterator,
        bool backwards,
        IEnumerable<int> reference)
    {
        var r = reference.GetEnumerator();
        while (!iterator.IsEmpty) {
            var b1 = r.MoveNext();
            Assert.True(b1);
            Assert.True(iterator.Top.Value == r.Current);

            var b2 = !backwards ? iterator.Succ() : iterator.Pred();
            Assert.True(iterator.IsEmpty == !b2);
        }
        Assert.True(!r.MoveNext());
    }
}
