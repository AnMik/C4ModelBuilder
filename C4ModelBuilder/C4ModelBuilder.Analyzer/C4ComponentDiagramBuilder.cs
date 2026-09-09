using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Analyzer;

/// <summary>
/// Рекурсивно обходит MemberNode, собирает <see cref="C4Component"/> и <see cref="C4Relation"/>.
/// </summary>
internal static class C4ComponentDiagramBuilder
{
    /// <summary>
    /// Обходит дерево вызовов <paramref name="root"/> и делает узлом каждый компонент: и тип (класс/интерфейс),
    /// и метод. Каждое ребро родитель → ребёнок становится связью. Узлы и связи дедуплицируются.
    /// </summary>
    public static C4ComponentDiagram Build(MemberNode root, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(root);

        var components = new HashSet<string>();
        var relations = new HashSet<(string From, string To)>();
        var stack = new Stack<(string?, MemberNode)>();

        // Корневые дети добавляются без родителя (Root на диаграмму не попадает).
        foreach (var topLevelChild in root.Children)
        {
            stack.Push((null, topLevelChild));
        }

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var (parentSignature, node) = stack.Pop();
            var signature = node.MethodSignature;

            if (string.IsNullOrEmpty(signature))
            {
                continue;
            }

            components.Add(signature);

            if (parentSignature != null && parentSignature != signature)
            {
                relations.Add((parentSignature, signature));
            }

            foreach (var child in node.Children)
            {
                stack.Push((signature, child));
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
