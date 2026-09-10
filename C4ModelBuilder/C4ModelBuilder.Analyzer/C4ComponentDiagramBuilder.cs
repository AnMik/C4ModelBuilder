using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Analyzer;

/// <summary>
/// Обходит <see cref="InvocationTree"/>, собирает <see cref="C4Component"/> и <see cref="C4Relation"/>, схлопывая узлы, не помеченные <c>IsComponent</c>.
/// </summary>
internal static class C4ComponentDiagramBuilder
{
    public static C4ComponentDiagram Build(InvocationTree root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var components = new HashSet<string>();
        var descriptions = new Dictionary<string, string?>();
        var relations = new HashSet<(string From, string To)>();
        var stack = new Stack<(InvocationTree Node, string? NearestComponentAncestor)>();
        stack.Push((root, null));

        while (stack.Count > 0)
        {
            var (node, nearestComponentAncestor) = stack.Pop();
            var signature = node.NodeName;

            if (string.IsNullOrEmpty(signature))
            {
                continue;
            }

            if (node.IsC4Component)
            {
                components.Add(signature);
                descriptions[signature] = node.Description;

                if (nearestComponentAncestor != null)
                {
                    relations.Add((From: nearestComponentAncestor, To: signature));
                }

                nearestComponentAncestor = signature;
            }

            foreach (var child in node.Invocations)
            {
                stack.Push((child, nearestComponentAncestor));
            }
        }

        return new C4ComponentDiagram(
            DiagramName: root.NodeName,
            Components: components
                .Select(
                    signature => new C4Component(
                        ComponentAlias: signature,
                        ComponentName: signature,
                        Description: descriptions.GetValueOrDefault(signature) ?? string.Empty))
                .ToList(),
            Relations: relations
                .Select(
                    relation => new C4Relation(FromComponentAlias: relation.From, ToComponentAlias: relation.To))
                .ToList());
    }
}
