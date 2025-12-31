using Podaga.Interning;
using System.Reflection;

namespace Podaga.Test;

internal class InternableList_Test
{
    private static readonly FieldInfo BackingListProperty = typeof(InternableList<int>).GetField("list", BindingFlags.NonPublic)!;


}
