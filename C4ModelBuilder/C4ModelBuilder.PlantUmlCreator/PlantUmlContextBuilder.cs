using C4ModelBuilder.Models;

namespace C4ModelBuilder.PlantUmlCreator;

/// <summary>
/// Рекурсивно обходит MemberNode, собирает <see cref="C4Component"/> и <see cref="C4Relation"/>.
/// </summary>
internal static class PlantUmlContextBuilder
{
    /// <summary>
    /// Обходит дерево <paramref name="root"/> и для каждого класса из MethodSignature формирует компонент,
    /// а для каждой пары (вызывающий → вызываемый) — связь.
    /// Компоненты и связи дедуплицируются.
    /// </summary>
    public static PlantUmlContext Build(MemberNode root)
    {
        var components = new HashSet<(string Alias, string Name, string? Description, string? Technology)>();
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
            var (parentClassName, node) = stack.Pop();
            var currentClassName = ExtractClassName(node.MethodSignature);

            if (currentClassName == null)
            {
                continue;
            }

            AddComponent(components, currentClassName);

            if (parentClassName != null && parentClassName != currentClassName)
            {
                relations.Add((parentClassName, currentClassName));
            }

            foreach (var child in node.Children)
            {
                stack.Push((currentClassName, child));
            }
        }

        return new PlantUmlContext
        {
            Components = components
                .Select(x => new C4Component(x.Alias, x.Name, x.Description, x.Technology))
                .ToList(),
            Relations = relations
                .Select(x => new C4Relation(x.From, x.To))
                .ToList(),
        };
    }

    private static string? ExtractClassName(string methodSignature)
    {
        var dotIndex = methodSignature.IndexOf('.', StringComparison.Ordinal);
        return dotIndex > 0 ? methodSignature[..dotIndex] : null;
    }

    private static void AddComponent(
        HashSet<(string Alias, string Name, string? Description, string? Technology)> components,
        string className)
    {
        // Alias и DisplayName совпадают (не знаем настоящего имени без атрибутов)
        components.Add((className, className, null, null));
    }
}

public sealed record PlantUmlContext
{
    public IReadOnlyList<C4Component> Components { get; init; } = [];
    public IReadOnlyList<C4Relation> Relations { get; init; } = [];
}