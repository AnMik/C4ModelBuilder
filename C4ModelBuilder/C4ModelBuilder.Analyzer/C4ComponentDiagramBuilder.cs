using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Analyzer;

/// <summary>
/// Рекурсивно обходит MemberNode, собирает <see cref="C4Component"/> и <see cref="C4Relation"/>.
/// </summary>
internal static class C4ComponentDiagramBuilder
{
    /// <summary>
    /// Обходит дерево <paramref name="root"/> и для каждого класса из MethodSignature формирует компонент,
    /// а для каждой пары (вызывающий → вызываемый) — связь.
    /// Компоненты и связи дедуплицируются.
    /// </summary>
    public static C4ComponentDiagram Build(MemberNode root, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(root);

        var components = new HashSet<(string Alias, string Name, string Description)>();
        var relations = new HashSet<(string From, string To)>();
        var stack = new Stack<(string?, MemberNode)>();

        // Начинаем с кортежа (null, root).
        // При рождении компонента передаётся ClassName текущего метода,
        // который будет использован для связи From в дочерних узлах.
        foreach (var topLevelChild in root.Children)
        {
            stack.Push((null, topLevelChild));
        }

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var (parentClassName, node) = stack.Pop();
            var currentClassName = ExtractClassName(node.MethodSignature);

            if (currentClassName == null)
            {
                continue;
            }

            components.Add((currentClassName, currentClassName, Description: string.Empty));

            if (parentClassName != null && parentClassName != currentClassName)
            {
                relations.Add((parentClassName, currentClassName));
            }

            foreach (var child in node.Children)
            {
                stack.Push((currentClassName, child));
            }
        }

        return new C4ComponentDiagram(
            Components: components
                .Select(x => new C4Component(ComponentAlias: x.Alias, ComponentName: x.Name, Description: x.Description))
                .ToList(),
            Relations: relations
                .Select(x => new C4Relation(FromComponentAlias: x.From, ToComponentAlias: x.To, Description: string.Empty))
                .ToList());
    }

    private static string? ExtractClassName(string methodSignature)
    {
        var dotIndex = methodSignature.IndexOf('.', StringComparison.Ordinal);
        return dotIndex > 0 ? methodSignature[..dotIndex] : null;
    }
}
