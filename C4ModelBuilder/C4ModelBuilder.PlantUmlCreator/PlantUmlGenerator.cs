using System.Text;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.PlantUmlCreator;

/// <summary>
/// Создаёт PlantUML-диаграмму C4 Component на основе дерева вызовов.
/// </summary>
public static class PlantUmlGenerator
{
    /// <summary>
    /// Генерирует текст C4 Component PlantUML диаграммы.
    /// </summary>
    public static string Generate(C4ComponentDiagram componentDiagram)
    {
        ArgumentNullException.ThrowIfNull(componentDiagram);

        var sb = new StringBuilder();

        sb
            .AppendLine("@startuml")
            .AppendLine()
            .AppendLine("!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Component.puml")
            .AppendLine();

        foreach (var component in componentDiagram.Components)
        {
            sb.AppendLine($"Component({component.ComponentAlias}, \"{component.ComponentName}\", \"{component.Description}\")");
        }

        if (componentDiagram.Components.Count > 0 && componentDiagram.Relations.Count > 0)
        {
            sb.AppendLine();
        }

        foreach (var relation in componentDiagram.Relations)
        {
            sb.AppendLine($"Rel({relation.FromComponentAlias}, {relation.ToComponentAlias}, \"\")");
        }

        sb
            .AppendLine()
            .AppendLine("@enduml");

        return sb.ToString();
    }

    /// <summary>
    /// Строит <see cref="C4ComponentDiagram"/> из дерева вызовов <see cref="InvocationTree"/>, схлопывая узлы без атрибута C4Component в связи между компонентами.
    /// </summary>
    public static C4ComponentDiagram BuildComponentDiagram(InvocationTree root)
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
            Components: components
                .Select(
                    signature => new C4ComponentDiagram.C4Component(
                        ComponentAlias: signature,
                        ComponentName: signature,
                        Description: descriptions.GetValueOrDefault(signature) ?? string.Empty))
                .ToList(),
            Relations: relations
                .Select(
                    relation => new C4ComponentDiagram.C4Relation(
                        FromComponentAlias: relation.From,
                        ToComponentAlias: relation.To))
                .ToList());
    }

    public static string Generate(InvocationTree tree)
    {
        var componentDiagram = BuildComponentDiagram(tree);
        return Generate(componentDiagram);
    }
}
