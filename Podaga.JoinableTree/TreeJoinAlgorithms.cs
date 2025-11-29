using System;
using System.Collections.Generic;
using System.Text;

namespace Podaga.JoinableTree;

/// <summary>
/// This struct is used as input to
/// <see cref="TreeJoinAlgorithms.Insert{T}(Podaga.JoinableTree.TreeJoin{T}, Podaga.JoinableTree.JoinableTreeNode{T}?, ref Podaga.JoinableTree.TreeModifyState{T})"/>
/// and
/// <see cref="TreeJoinAlgorithms.Delete{T}(Podaga.JoinableTree.TreeJoin{T}, Podaga.JoinableTree.JoinableTreeNode{T}?, ref Podaga.JoinableTree.TreeModifyState{T})"/>
/// methods.
/// </summary>
public struct TreeModifyState<T>
{
    /// <summary>
    /// The value to insert or delete.
    /// </summary>
    public T Value;

    /// <summary>
    /// A node with <see cref="Value"/> that was found in the tree during insert or delete.
    /// </summary>
    /// <remarks>
    /// This field reflects the status of the operation.  For insert, the operation was successful if this value IS <c>null</c>.
    /// For delete, the operation was successful if this value IS NOT <c>null</c>.
    /// </remarks>
    public JoinableTreeNode<T>? Found;
}

/// <summary>
/// Extension methods on <see cref="TreeJoin{T}"/>; these implement non-essential algorithms.
/// The callers must respect nullable parameter annotations because no additional null checks are performed for performance reasons.
/// </summary>
public static class TreeJoinAlgorithms
{
    extension<T>(TreeJoin<T> @this)
    {
        /// <summary>
        /// Creates an iterator and optionally pushes <paramref name="node"/> on top of the stack.
        /// </summary>
        /// <param name="node">
        /// Optional node to push onto the stack.  If null, the returned iterator is empty.
        /// </param>
        /// <returns>An iterator instance which is either empty or contains only <paramref name="node"/> on the top of the stack.</returns>
        public TreeIterator<T> GetIterator(JoinableTreeNode<T>? node = null)
        {
            var ret = TreeIterator<T>.New(@this);
            if (node is not null)
                ret.Push(node);
            return ret;
        }

        /// <summary>
        /// Copies all nodes in the subtree starting from <paramref name="node"/>.  Each node is cloned if the its transient tag is
        /// different from the <paramref name="this"/> transient.
        /// </summary>
        /// <returns>The root of the copied subtree.</returns>
        public JoinableTreeNode<T> Copy(JoinableTreeNode<T> node)
        {
            node = node.Clone(@this);
            if (node.Left != null)
                node.Left = @this.Copy(node.Left);
            if (node.Right != null)
                node.Right = @this.Copy(node.Right);
            return node;
        }

        /// <summary>
        /// Single left rotation using <paramref name="node"/> as pivot node.
        /// This method will throw <see cref="NullReferenceException"/> when invoked on an inappropriate structure.
        /// </summary>
        /// <returns>New subtree root such that <paramref name="node"/> is its left child.</returns>
        public JoinableTreeNode<T> RotL(JoinableTreeNode<T> node)
        {
            node = node.Clone(@this);
            var y = node.Right!.Clone(@this);
            node.Right = y.Left;
            y.Left = node;
            node.Update(@this);
            y.Update(@this);
            return y;
        }

        /// <summary>
        /// Double left rotation using <paramref name="node"/> as pivot node.
        /// This method will throw <see cref="NullReferenceException"/> when invoked on an inappropriate structure.
        /// </summary>
        /// <returns>New subtree root such that <paramref name="node"/> is its left child.</returns>
        public JoinableTreeNode<T> RotLL(JoinableTreeNode<T> node)
        {
            node = node.Clone(@this);
            var x = node.Right!.Clone(@this);
            var y = x.Left!.Clone(@this);
            node.Right = y.Left;
            x.Left = y.Right;
            y.Left = node;
            y.Right = x;
            node.Update(@this);
            x.Update(@this);
            y.Update(@this);
            return y;
        }

        /// <summary>
        /// Single right rotation using <paramref name="node"/> as pivot node.
        /// This method will throw <see cref="NullReferenceException"/> when invoked on an inappropriate structure.
        /// </summary>
        /// <returns>New subtree root such that <paramref name="node"/> is its right child.</returns>
        public JoinableTreeNode<T> RotR(JoinableTreeNode<T> node)
        {
            node = node.Clone(@this);
            var x = node.Left!.Clone(@this);
            node.Left = x.Right;
            x.Right = node;
            node.Update(@this);
            x.Update(@this);
            return x;
        }

        /// <summary>
        /// Double right rotation using <paramref name="node"/> as pivot node.
        /// This method will throw <see cref="NullReferenceException"/> when invoked on an inappropriate structure.
        /// </summary>
        /// <returns>New subtree root such that <paramref name="node"/> is its right child.</returns>
        public JoinableTreeNode<T> RotRR(JoinableTreeNode<T> node)
        {
            node = node.Clone(@this);
            var x = node.Left!.Clone(@this);
            var y = x.Right!.Clone(@this);
            x.Right = y.Left;
            node.Left = y.Right;
            y.Left = x;
            y.Right = node;
            x.Update(@this);
            node.Update(@this);
            y.Update(@this);
            return y;
        }

        /// <summary>
        /// Finds a value in the tree rooted at <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node at which to start the search.</param>
        /// <param name="value">Value to look for.</param>
        /// <param name="found">Set to the result of comparison with the last visited node.  When 0, the value was found.</param>
        /// <returns>
        /// The last visited node in the tree.  If <paramref name="found"/> is 0, the node contains a value that
        /// compares equal to <paramref name="value"/>.
        /// </returns>
        public JoinableTreeNode<T>? Find(JoinableTreeNode<T>? node, T value, out int found)
        {
            JoinableTreeNode<T>? prev = null;
            var c = -1;
            while (node != null && c != 0) {
                c = @this.Compare(value, node.Value);
                prev = node;
                node = c < 0 ? node.Left! : node.Right!;
            }
            found = c;
            return prev;
        }

        /// <summary>
        /// Inserts a value into the tree rooted at <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node to use as the root for inserting the new element.</param>
        /// <param name="state">An instance of <see cref="TreeModifyState{T}"/> with <c>Value</c> initialized.</param>
        /// <returns>
        /// Root of the modified tree.  Success (i.e., the value is not a duplicate) is indicated by
        /// <see cref="TreeModifyState{TValue}.Found"/> member of <paramref name="state"/>.
        /// </returns>
        public JoinableTreeNode<T> Insert(JoinableTreeNode<T>? node, ref TreeModifyState<T> state)
        {
            if (node is null) {
                state.Found = null;
                var n = new JoinableTreeNode<T>(@this) { Value = state.Value };
                n.Update(@this);
                return n;
            }

            var c = @this.Compare(state.Value, node.Value);
            if (c == 0) {
                state.Found = node;
                return node;
            }

            if (c < 0) {
                var n = @this.Insert(node.Left, ref state);
                if (state.Found != null)
                    return node;

                var jd = new TreeJoin<T>.Section { Left = n, Middle = node, Right = node.Right };
                return @this.Join(jd);
            } else {
                var n = @this.Insert(node.Right, ref state);
                if (state.Found != null)
                    return node;


                var jd = new TreeJoin<T>.Section { Left = node.Left, Middle = node, Right = n };
                return @this.Join(jd);
            }
        }

        /// <summary>
        /// Deletes a value from the tree rooted at <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node to use as the root for inserting the new element.</param>
        /// <param name="state">An instance of <see cref="TreeModifyState{T}"/> with <c>Value</c> initialized.</param>
        /// <returns>
        /// Root of the modified tree (null if the last node was deleted).  Success (i.e., the value is not a duplicate) is indicated by
        /// <see cref="TreeModifyState{TValue}.Found"/> member of <paramref name="state"/>.
        /// </returns>
        public JoinableTreeNode<T>? Delete(JoinableTreeNode<T>? node, ref TreeModifyState<T> state)
        {
            if (node is null) {
                state.Found = null;
                return null;
            }

            var c = @this.Compare(state.Value, node.Value);
            if (c == 0) {
                state.Found = node;
                var jd = new TreeJoin<T>.Section { Left = node.Left, Right = node.Right };
                return @this.Join2(jd);
            }

            if (c < 0) {
                var n = @this.Delete(node.Left, ref state);
                if (state.Found == null)
                    return node;

                var jd = new TreeJoin<T>.Section { Left = n, Middle = node, Right = node.Right };
                return @this.Join(jd);
            } else {
                var n = @this.Delete(node.Right, ref state);
                if (state.Found == null)
                    return node;

                var jd = new TreeJoin<T>.Section { Left = node.Left, Middle = node, Right = n };
                return @this.Join(jd);
            }
        }

        /// <summary>
        /// Checks whether two sets are elementwise equal.
        /// Element equality is determined by <see cref="TreeJoin{T}.Comparer"/>.
        /// </summary>
        /// <param name="t1">Root of the first tree.</param>
        /// <param name="t2">Root of the second tree.</param>
        /// <returns>
        /// True if the two sets are equal.
        /// </returns>
        public bool SetEqual(JoinableTreeNode<T>? t1, JoinableTreeNode<T>? t2)
        {
            if (t1 is null || t2 is null)
                return (t1 is null) == (t2 is null);
            if (t1.Size != t2.Size)
                return false;

            var ai = @this.GetIterator(t1);
            ai.First();

            var bi = @this.GetIterator(t2);
            bi.First();

            // At this point, sizes are equal and at least 1.
            do {
                if (@this.Compare(ai.Top.Value, bi.Top.Value) != 0)
                    return false;
            } while (ai.Succ() && bi.Succ());
            return true;
        }

        /// <summary>
        /// Join-based union algorithm.
        /// </summary>
        /// <param name="t1">Root of the first tree.</param>
        /// <param name="t2">Root of the second tree.</param>
        /// <returns>Root of the tree that is the union of <paramref name="t1"/> and <paramref name="t2"/>.</returns>
        public JoinableTreeNode<T>? SetUnion(JoinableTreeNode<T>? t1, JoinableTreeNode<T>? t2)
        {
            if (t1 == null)
                return t2;
            if (t2 == null)
                return t1;

            var s = @this.Split(t1, t2.Value);
            var l = @this.SetUnion(s.Left, t2.Left);
            var r = @this.SetUnion(s.Right, t2.Right);
            if (s.Middle != null) {
                t1 = s.Middle.Clone(@this);
            } else {
                t1 = t2;
            }
            return @this.Join(new() { Left = l, Middle = t1, Right = r });
        }

        /// <summary>
        /// Join-based intersection algorithm.
        /// </summary>
        /// <param name="t1">Root of the first tree.</param>
        /// <param name="t2">Root of the second tree.</param>
        /// <returns>Root of the tree that is the intersection of <paramref name="t1"/> and <paramref name="t2"/>.</returns>
        public JoinableTreeNode<T>? SetIntersection(JoinableTreeNode<T>? t1, JoinableTreeNode<T>? t2)
        {
            if (t1 == null || t2 == null)
                return null;

            var s = @this.Split(t1, t2.Value);
            var l = @this.SetIntersection(s.Left, t2.Left);
            var r = @this.SetIntersection(s.Right, t2.Right);
            if (s.Middle != null) {
                t1 = s.Middle.Clone(@this);
                return @this.Join(new() { Left = l, Middle = t1, Right = r });
            }
            s.Left = l;
            s.Right = r;
            return @this.Join2(s);
        }

        /// <summary>
        /// Join-based difference algorithm.
        /// </summary>
        /// <param name="t1">Root of the first tree.</param>
        /// <param name="t2">Root of the second tree.</param>
        /// <returns>Root of the tree that is the difference of <paramref name="t1"/> and <paramref name="t2"/>.</returns>
        public JoinableTreeNode<T>? SetDifference(JoinableTreeNode<T>? t1, JoinableTreeNode<T>? t2)
        {
            if (t1 == null)
                return null;
            if (t2 == null)
                return t1;

            var s = @this.Split(t1, t2.Value);
            var l = @this.SetDifference(s.Left, t2.Left);
            var r = @this.SetDifference(s.Right, t2.Right);
            s.Left = l;
            s.Right = r;
            return @this.Join2(s);
        }
    }
}
