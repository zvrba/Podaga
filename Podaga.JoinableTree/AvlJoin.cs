using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Podaga.JoinableTree;

/// <summary>
/// Implementation of <see cref="TreeJoin{T}"/> for AVL trees.
/// </summary>
/// <typeparam name="T">Value type stored in the tree.</typeparam>
public sealed class AvlJoin<T> : TreeJoin<T>
{
    /// <inheritdoc/>
    public AvlJoin(IComparer<T> comparer) : base(comparer)
    {
        RZero = 0;
    }

    /// <inheritdoc/>
    public override TreeJoin<T> Clone() => new AvlJoin<T>(Comparer);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int RPlus(int left, int right) => 1 + (left > right ? left : right);

    // UTILITIES

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int H(JoinableTreeNode<T>? n) => n?.Rank ?? 0;

    /// <inheritdoc/>
    public override JoinableTreeNode<T> Join(Section jd) {
        if (H(jd.Left) > H(jd.Right) + 1)
            return JoinR(jd);
        if (H(jd.Right) > H(jd.Left) + 1)
            return JoinL(jd);
        return JoinBalanced(jd);
    }

    /// <inheritdoc/>
    public override void ValidateStructure(JoinableTreeNode<T>? node) => ValidateHeights(node);

    // Search along the right spine of tl ...
    private JoinableTreeNode<T> JoinR(Section jd)
    {
        var tl = jd.Left!;
        var (l, c) = (tl.Left, tl.Right);
        if (H(c) <= H(jd.Right) + 1) {
            jd.Left = c;
            var t1 = JoinBalanced(jd);
            tl = tl.Clone(this);
            tl.Right = t1;
            tl.Update(this);
            if (t1.Rank > H(l) + 1)
                tl = this.RotLL(tl);
        } else {
            jd.Left = c;
            var t1 = JoinR(jd);
            tl = tl.Clone(this);
            tl.Right = t1;
            tl.Update(this);
            if (t1.Rank > H(l) + 1)
                tl = this.RotL(tl);
        }
        return tl;
    }

    // Search along the left spine of tr...
    private JoinableTreeNode<T> JoinL(Section jd)
    {
        var tr = jd.Right!;
        var (c, r) = (tr.Left, tr.Right);
        if (H(c) <= H(jd.Left) + 1) {
            jd.Right = c;
            var t1 = JoinBalanced(jd);
            tr = tr.Clone(this);
            tr.Left = t1;
            tr.Update(this);
            if (t1.Rank > H(r) + 1)
                tr = this.RotRR(tr);
        } else {
            jd.Right = c;
            var t1 = JoinL(jd);
            tr = tr.Clone(this);
            tr.Left = t1;
            tr.Update(this);
            if (t1.Rank > H(r) + 1)
                tr = this.RotR(tr);
        }
        return tr;
    }

    private static int ValidateHeights(JoinableTreeNode<T>? node)
    {
        if (node == null)
            return 0;

        var l = ValidateHeights(node.Left);
        var r = ValidateHeights(node.Right);
        var h = 1 + (l > r ? l : r);
        var b = r - l;

        if (node.Rank != h)
            throw new NotImplementedException();
        if (b < -1 || b > 1)
            throw new NotImplementedException();

        return h;
    }
}
