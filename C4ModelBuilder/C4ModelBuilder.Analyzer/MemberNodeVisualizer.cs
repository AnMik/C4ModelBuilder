using C4ModelBuilder.Analyzer.Infrastructure;
using C4ModelBuilder.Analyzer.Models;

namespace C4ModelBuilder.Analyzer;

internal static class MemberNodeVisualizer
{
    public static void WriteToConsole(MemberNode node, int depth = 0)
    {
        ArgumentNullException.ThrowIfNull(node);
        WriteWithTab(depth, node.Name);

        if (node.Children.Count == 0)
        {
            WriteWithTab(depth + 1, "<Empty>");
            return;
        }

        foreach (var child in node.Children)
        {
            WriteToConsole(child, depth + 1);
        }
    }

    private static string GetTabs(int count) => $"{Enumerable.Repeat("   ", count).JoinStrings(string.Empty)}\u2514\u2500\u2500";

    private static void WriteWithTab(int depth, string text) => Console.WriteLine($"{GetTabs(depth)}{text}");
}
