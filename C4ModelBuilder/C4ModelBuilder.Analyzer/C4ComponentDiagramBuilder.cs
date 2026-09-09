using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Analyzer;

/// <summary>
/// Рекурсивно обходит MemberNode, собирает <see cref="C4Component"/> и <see cref="C4Relation"/>.
/// </summary>
internal static class C4ComponentDiagramBuilder
{
    /// <summary>
    /// Обходит дерево вызовов <paramref name="root"/> и делает узлом каждый компонент: корень и каждый узел
    /// (тип/класс/интерфейс или метод). Каждое ребро родитель → ребёнок становится связью. Узлы и связи дедуплицируются.
    /// </summary>
    public static C4ComponentDiagram Build(MemberNode root, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(root);

        var components = new HashSet<string>();
        var relations = new HashSet<(string From, string To)>();
        var stack = new Stack<MemberNode>();

        stack.Push(root);

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var node = stack.Pop();
            var signature = node.MethodSignature;

            if (string.IsNullOrEmpty(signature))
            {
                continue;
            }

            components.Add(signature);

            foreach (var child in node.Children)
            {
                relations.Add((signature, child.MethodSignature));
                stack.Push(child);
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
