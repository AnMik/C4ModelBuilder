using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.PlantUmlCreator;

/// <summary>
/// Схлопывает дерево вызовов <see cref="InvocationTree"/> в модель диаграммы <see cref="C4ComponentDiagram"/>:
/// узлы без атрибута C4Component пропускаются, а связи протягиваются между ближайшими
/// компонентами-предками; узел-обёртка вызова метода своего класса прозрачен — связь идёт к вызванному
/// методу, а сам класс остаётся компонентом-предком; связи компонента с самим собой не создаются,
/// дубликаты компонентов и связей устраняются.
/// </summary>
internal static class InvocationTreeMerger
{
    public static C4ComponentDiagram MergeToComponentDiagram(InvocationTree root)
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

            if (node.C4ComponentDescription != null)
            {
                components.Add(signature);
                descriptions[signature] = node.C4ComponentDescription;

                if (node.IsSameClassCall)
                {
                    // Обёртка вызова метода своего класса прозрачна для связей: концом связи становится
                    // вызванный метод, а класс этой связи остаётся компонентом-предком содержимого вызова.
                    foreach (var child in node.Invocations.Where(child => child.C4ComponentDescription != null))
                    {
                        if (nearestComponentAncestor != null && nearestComponentAncestor != child.NodeName)
                        {
                            relations.Add((From: nearestComponentAncestor, To: child.NodeName));
                        }
                    }
                }
                else if (nearestComponentAncestor != null && nearestComponentAncestor != signature)
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
            Components: components
                .Select(
                    signature => new C4ComponentDiagram.C4Component(
                        ComponentAlias: signature,
                        ComponentName: signature,
                        Description: descriptions.GetValueOrDefault(signature) ?? string.Empty,
                        Type: ResolveComponentType(signature)))
                .ToList(),
            Relations: relations
                .Select(relation => new C4ComponentDiagram.C4Relation(FromComponentAlias: relation.From, ToComponentAlias: relation.To))
                .ToList());
    }

    private static C4ComponentDiagram.Type ResolveComponentType(string componentName)
    {
        if (componentName.Contains("Repository", StringComparison.OrdinalIgnoreCase))
        {
            return C4ComponentDiagram.Type.Repository;
        }

        if (componentName.Contains("Gateway", StringComparison.OrdinalIgnoreCase))
        {
            return C4ComponentDiagram.Type.Gateway;
        }

        return C4ComponentDiagram.Type.Normal;
    }
}
