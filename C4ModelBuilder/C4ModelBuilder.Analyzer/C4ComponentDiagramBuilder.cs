using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Analyzer;

/// <summary>
/// Обходит <see cref="InvocationTree"/>, собирает <see cref="C4Component"/> и <see cref="C4Relation"/>,
/// схлопывая узлы, не помеченные <c>IsComponent</c>.
/// </summary>
internal static class C4ComponentDiagramBuilder
{
    /// <summary>
    /// Обходит дерево вызовов <paramref name="root"/>.
    /// В диаграмму попадают только узлы с <c>IsComponent == true</c>.
    /// Связи проводятся от ближайшего компонента-предка к текущему компоненту;
    /// непомеченные узлы (методы) схлопываются. Узлы и связи дедуплицируются.
    /// </summary>
    public static C4ComponentDiagram Build(InvocationTree root, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(root);

        var components = new HashSet<string>();
        var relations = new HashSet<(string From, string To)>();
        var stack = new Stack<(InvocationTree Node, string? NearestComponentAncestor)>();
        stack.Push((root, null));

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var (node, nearestComponentAncestor) = stack.Pop();
            var signature = node.NodeName;

            if (string.IsNullOrEmpty(signature))
            {
                continue;
            }

            if (node.IsC4Component)
            {
                components.Add(signature);

                if (nearestComponentAncestor != null)
                {
                    relations.Add((nearestComponentAncestor, signature));
                }

                nearestComponentAncestor = signature;
            }

            foreach (var child in node.Invocations)
            {
                stack.Push((child, nearestComponentAncestor));
            }
        }

        return new C4ComponentDiagram(
            Components: components
                .Select(signature => new C4Component(ComponentAlias: signature, ComponentName: signature, Description: string.Empty))
                .ToList(),
            Relations: relations
                .Select(relation => new C4Relation(FromComponentAlias: relation.From, ToComponentAlias: relation.To, Description: string.Empty))
                .ToList());
    }
}
