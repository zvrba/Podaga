using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Podaga.JoinableTree;

/// <summary>
/// Implementation of <see cref="TreeJoin{T}"/> for WB trees.
/// The balance factor is hard-coded to 1/4.  This value is just below the maximum proven in the paper
/// that makes the tree strongly joinable.
/// </summary>
/// <typeparam name="T">Value type stored in the tree.</typeparam>
public sealed class WBJoin<T> : TreeJoin<T>
{
    private const float Alpha = 0.25f;  // Alpha
    private const float AlphaC = 1 - Alpha;

    /// <inheritdoc/>
    public WBJoin(IComparer<T> comparer) : base(comparer) { }

    /// <inheritdoc/>
    public override TreeJoin<T> Clone() => new WBJoin<T>(Comparer);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int RPlus(int left, int right) => 0;    // Size is used as rank.


    // UTILITIES.  TODO! FIX! The calculations below can overflow when sizes exceed > 2^26 elements.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int S(JoinableTreeNode<T>? n) => n?.Size ?? 0;

    // Checks whether the left size is overweight using int arithmetic only.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LeftHeavy(int lsize, int rsize) => lsize + 1 > 3 * (lsize + rsize + 2) / 4;

    // Checks that the balance factor is within [1/4, 3/4] using int arithmetic only.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Like(int lsize, int rsize) {
        rsize += lsize + 2;         // Total tree weight
        lsize = (lsize + 1) * 4;    // Common denominator for lhs and rhs
        return lsize >= rsize && lsize <= 3 * rsize;
    }

    // Checks that the balance factor is within [1/4, 2/3) using int arithmetic only.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSingleRotation(int lsize, int rsize) {
        rsize += lsize + 2;         // Total tree weight
        lsize = (lsize + 1) * 12;   // LCM of 3 and 4
        return lsize >= 3 * rsize && lsize < 8 * rsize;
    }

    /// <inheritdoc/>
    public override JoinableTreeNode<T> Join(Section jd)
    {
        if (LeftHeavy(S(jd.Left), S(jd.Right)))
            return JoinR(jd);
        if (LeftHeavy(S(jd.Right), S(jd.Left)))
            return JoinL(jd);
        return JoinBalanced(jd);
    }

    /// <inheritdoc/>
    public override void ValidateStructure(JoinableTreeNode<T>? node) {
        if (node?.Size > 1)  // Single-element tree cannot be balanced.
            ValidateWeights(node);
    }

    private JoinableTreeNode<T> JoinR(Section jd) {
        if (Like(S(jd.Left), S(jd.Right)))             // Base case
            return JoinBalanced(jd);

        var tl = jd.Left!;
        jd.Left = tl.Right;
        var t1 = JoinR(jd);
        tl = tl.Clone(this);
        tl.Right = t1;
        tl.Update(this);

        if (!Like(S(tl.Left), S(t1))) {
            if (IsSingleRotation(S(tl.Left), S(t1))) tl = this.RotL(tl);
            else tl = this.RotLL(tl);
        }
        return tl;
    }

    // Follow left branch of tr until a TreeNode c is reached with a weight like to tl.
    private JoinableTreeNode<T> JoinL(Section jd) {
        if (Like(S(jd.Left), S(jd.Right)))
            return JoinBalanced(jd);

        var tr = jd.Right!;
        jd.Right = tr.Left;
        var t1 = JoinL(jd);
        tr = tr.Clone(this);
        tr.Left = t1;
        tr.Update(this);

        if (!Like(S(t1), S(tr.Right))) {
            if (IsSingleRotation(S(t1), S(tr.Right))) tr = this.RotR(tr);
            else tr = this.RotRR(tr);
        }
        return tr;
    }

    private static void ValidateWeights(JoinableTreeNode<T>? node) {
        if (node == null)
            return;

        var r = (float)(S(node.Left) + 1) / (S(node.Left) + S(node.Right) + 2);
        if (r < Alpha || r > AlphaC)
            throw new NotImplementedException();

        ValidateWeights(node.Left);
        ValidateWeights(node.Right);
    }
}
