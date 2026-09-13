using System.Text;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Cli.Infrastructure;

internal static class InvocationTreeFormatter
{
    public static string Format(InvocationTree node, int depth = 0)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{new string(' ', depth * 3)}└──{node.NodeName}");

        if (node.Invocations.Count == 0)
        {
            builder.AppendLine($"{new string(' ', (depth + 1) * 3)}└──<Empty>");
            return builder.ToString();
        }

        foreach (var child in node.Invocations)
        {
            builder.Append(Format(child, depth + 1));
        }

        return builder.ToString();
    }
}
