using System.Text;
using C4ModelBuilder.Models;

namespace C4ModelBuilder.PlantUmlCreator;

/// <summary>
/// Создаёт PlantUML-диаграмму C4 Component на основе дерева <see cref="MemberNode"/>.
/// </summary>
public static class PlantUmlGenerator
{
    private const string C4ComponentInclude = "https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Component.puml";

    /// <summary>
    /// Генерирует C4 Component PlantUML из дерева вызовов методов.
    /// </summary>
    /// <param name="root">Корневой узел MemberNode (с дочерними узлами).</param>
    /// <returns>Строка с PlantUML-диаграммой.</returns>
    public static string Generate(MemberNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var ctx = PlantUmlContextBuilder.Build(root);
        return Generate(ctx);
    }

    /// <summary>
    /// Генерирует C4 Component PlantUML из готового контекста.
    /// </summary>
    internal static string Generate(PlantUmlContext ctx)
    {
        var sb = new StringBuilder();

        sb.AppendLine("@startuml");
        sb.AppendLine();
        sb.AppendLine($"!include {C4ComponentInclude}");
        sb.AppendLine();
        sb.AppendLine("LAYOUT_WITH_LEGEND()");
        sb.AppendLine();

        foreach (var component in ctx.Components)
        {
            var description = component.Description ?? string.Empty;
            sb.AppendLine(
                component.Technology == null
                    ? $"Component({component.ComponentAlias}, \"{component.ComponentName}\", \"{description}\")"
                    : $"Component({component.ComponentAlias}, \"{component.ComponentName}\", \"{component.Technology}\", \"{description}\")");
        }

        if (ctx.Components.Count > 0 && ctx.Relations.Count > 0)
        {
            sb.AppendLine();
        }

        foreach (var relation in ctx.Relations)
        {
            var label = relation.Description ?? string.Empty;
            sb.AppendLine(
                relation.Technology == null
                    ? $"Rel({relation.FromComponentAlias}, {relation.ToComponentAlias}, \"{label}\")"
                    : $"Rel({relation.FromComponentAlias}, {relation.ToComponentAlias}, \"{label}\", \"{relation.Technology}\")");
        }

        sb.AppendLine();
        sb.AppendLine("@enduml");

        return sb.ToString();
    }
}