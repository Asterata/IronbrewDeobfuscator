using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;

namespace IronbrewDeobfuscator.Deobfuscator.Utils;

public static class EnumerableExtentions
{
    internal static IEnumerable<List<SyntaxNode>> SplitListByPattern<T>(this List<SyntaxNode> list, IReadOnlyCollection<SyntaxNode> pattern)
    {
        var result = new List<List<SyntaxNode>>();
        var currentList = new List<SyntaxNode>();
        foreach (var node in list)
        {
            if (IsPatternMatched(currentList, pattern))
            {
                currentList.RemoveRange(currentList.Count - pattern.Count, pattern.Count);
                result.Add(currentList.ToList());
                currentList.Clear();
            }

            currentList.Add(node);
        }

        if (currentList.Count != 0)
        {
            result.Add(currentList.ToList());
        }
        
        return result;
        
        static bool IsPatternMatched(IReadOnlyList<SyntaxNode> currentList, IReadOnlyCollection<SyntaxNode> pattern)
        {
            if (currentList.Count < pattern.Count)
            {
                return false;
            }

            return !pattern.Where((t, i) => !currentList[currentList.Count - pattern.Count + i].Kind().Equals(t.Kind())).Any();
        }
    }
    
    internal static bool IsEmpty<T>(this IEnumerable<T> enumerable)
    {
        return !enumerable.Any();
    }

}