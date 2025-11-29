using System;
using System.Collections.Generic;
using System.Text;

namespace Podaga.JoinableTree;

/// <summary>
/// Extension methods on <see cref="TreeIterator{T}"/>.  Iteration leaves a "trail" on the iterator's stack, thus allowing
/// forward and backward navigation.
/// </summary>
public static class TreeIteratorAlgorithms
{
    extension<T>(ref TreeIterator<T> @this)
    {
        /// <summary>
        /// Finds a value equivalent to <paramref name="value"/> beginning from the node at the top of the stack.
        /// The stack is extended with the path to the last visited node.
        /// </summary>
        /// <param name="value">Value to find.  Only the key fields must be initialized.</param>
        /// <returns>
        /// The result of the last comparison leading to the top node in <c>this</c>.
        /// Zero means that an equivalent value was found and is on top of the stack.
        /// </returns>
        public int Find(T value)
        {
            var node = @this.TryPop();
            var comparer = @this.Comparer;

            int c = -1;
            while (node != null) {
                @this.Push(node);
                if ((c = comparer.Compare(value, node.Value)) == 0)
                    break;
                node = c < 0 ? node.Left : node.Right;
            }
            return c;
        }

        /// <summary>
        /// Extends <c>this</c> with the path to the leftmost node starting from the node at the top of the stack.
        /// </summary>
        /// <returns>
        /// True if a node was found.  False is returned only when <c>this</c> is empty.
        /// </returns>
        public bool First()
        {
            for (var node = @this.TryPop(); node != null; node = node.Left)
                @this.Push(node);
            return !@this.IsEmpty;
        }

        /// <summary>
        /// Extends <c>this</c> with the path to the rightmost node starting from the node at the top of the stack.
        /// </summary>
        /// <returns>
        /// True if a node was found.  False is returned only when <c>this</c> is empty.
        /// </returns>
        public bool Last()
        {
            for (var node = @this.TryPop(); node != null; node = node.Right)
                @this.Push(node);
            return !@this.IsEmpty;
        }

        /// <summary>
        /// Moves <c>this</c> to the next element in sort order (wrt the node at the top of the stack).
        /// </summary>
        /// <returns>True if the next element exists, false otherwise.</returns>
        public bool Succ()
        {
            var current = @this.TryPop();
            if (current == null)
                return false;

            if (current.Right != null) {
                @this.Push(current);
                for (current = current.Right; current != null; current = current.Left)
                    @this.Push(current);
            } else {
                JoinableTreeNode<T> y;
                do {
                    y = current;
                    if ((current = @this.TryPop()) == null)
                        return false;
                } while (y == current.Right);
                @this.Push(current);
            }
            return true;
        }

        /// <summary>
        /// Moves <c>this</c> to the previous element in sort order (wrt the node at the top of the stack).
        /// </summary>
        /// <returns>True if the next element exists, false otherwise.</returns>
        public bool Pred()
        {
            var current = @this.TryPop();
            if (current == null)
                return false;

            if (current.Left != null) {
                @this.Push(current);
                for (current = current.Left; current != null; current = current.Right)
                    @this.Push(current);
            } else {
                JoinableTreeNode<T> y;
                do {
                    y = current;
                    if ((current = @this.TryPop()) == null)
                        return false;
                } while (y == current.Left);
                @this.Push(current);
            }
            return true;
        }

        /// <summary>
        /// Sets <c>this</c> to the n'th element in sorted order (wrt. the node at the top of the stack).
        /// </summary>
        /// <param name="index">Order of the element to retrieve.</param>
        /// <exception cref="IndexOutOfRangeException">
        /// Index is outside of range <c>[0, Size-1)</c>, size being the size of the subtree.
        /// </exception>
        public void Nth(int index)
        {
            var node = @this.IsEmpty ? null : @this.Top;
            if (node == null || index < 0 || index >= node.Size)
                throw new IndexOutOfRangeException("Invalid tree element index.");
            ++index;    // Makes calculations easier.

        loop:
            @this.Push(node!);
            var l = node!.Left?.Size ?? 0;
            if (index == l + 1)
                return;
            if (index <= l) {
                node = node.Left;
            } else {
                node = node.Right;
                index -= l + 1;
            }
            goto loop;
        }
    }
}
