using Podaga.Interning;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Podaga.Test;

internal class InternableList_Test
{
    private static readonly FieldInfo BackingListProperty = typeof(InternableList<int>).GetField("list", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static void Run()
    {
        InternedListModificationThrows();
        TestSmallStorage();

        var rl = new List<int>();
    }

    // Large storage delegates to List<int>, so we don't test that.
    private static void TestSmallStorage()
    {
        var il = new InternableList<int>();
        for (var i = 0; i < InternableList<int>.MaxSmallCount / 2; ++i) {
            Assert.True(BackingListProperty.GetValue(il) is null);
            il.Add(i);
        }
        Assert.True(il.Equals([0, 1, 2, 3]));

        Assert.Throws<ArgumentOutOfRangeException>(() => il.Insert(-1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => il.Insert(il.Count + 1, -1));

        il.Insert(0, -1);
        il.Insert(il.Count, 4);
        il.Insert(3, -2);
        Assert.True(il.Equals([-1, 0, 1, -2, 2, 3, 4]));
        Assert.True(BackingListProperty.GetValue(il) is null);

        il.Add(il.Count);
        il.Add(il.Count);
        Assert.True(il.Count == 9 && BackingListProperty.GetValue(il) is not null);

        Assert.True(il.Remove(12) == false);
        Assert.True(il.Remove(8));
    }


    private static void InternedListModificationThrows()
    {
        using var i = new WeakHash();
        var l = new InternableList<int>() { 3 };
        l = l.Intern(i);

        Assert.True(l.IsReadOnly);
        Assert.True(l.Count == 1);
        Assert.True(l[0] == 3);
        Assert.True(BackingListProperty.GetValue(l) is null);
        Assert.True(l.Contains(3));
        Assert.True(l.IndexOf(0) < 0);

        var a = new int[3];
        l.CopyTo(a, 1);
        Assert.True(a[1] == 3);

        Assert.Throws<ObjectInternedException>(() => l[0] = 12);
        Assert.Throws<ObjectInternedException>(() => l.Add(1));
        Assert.Throws<ObjectInternedException>(() => l.Clear());
        Assert.Throws<ObjectInternedException>(() => l.Insert(0, 1));
        Assert.Throws<ObjectInternedException>(() => l.Remove(0));
        Assert.Throws<ObjectInternedException>(() => l.RemoveAt(0));

        var l2 = new InternableList<int>() { 3 }.Intern(i);
        Assert.True(ReferenceEquals(l, l2));

        var e = l.GetEnumerator();
        Assert.True(e.MoveNext());
        Assert.True(e.Current == 3);
        Assert.True(e.MoveNext() == false);
    }
}
