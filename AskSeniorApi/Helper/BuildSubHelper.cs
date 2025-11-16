using AskSeniorApi.DTO;

namespace AskSeniorApi.Helper;

public static class BuildSubHelper
{
    public static List<T> BuildHierarchy<T>(
        List<T> items,
        Func<T, string?> getParentId,
        Func<T, string> getId,
        Action<T, List<T>> setChildren
    )
    {
        var roots = items
            .Where(i => getParentId(i) == null)
            .ToList();

        foreach (var root in roots)
        {
            BuildChildren(root, items, getParentId, getId, setChildren);
        }

        return roots;
    }

    private static void BuildChildren<T>(
        T parent,
        List<T> items,
        Func<T, string?> getParentId,
        Func<T, string> getId,
        Action<T, List<T>> setChildren
    )
    {   
        var children = items
            .Where(i => getParentId(i) == getId(parent))
            .ToList();

        //Assign children to parent
        setChildren(parent, children);

        //Call recursively for each child  - build its children
        foreach (var child in children)
            BuildChildren(child, items, getParentId, getId, setChildren);
    }
}
