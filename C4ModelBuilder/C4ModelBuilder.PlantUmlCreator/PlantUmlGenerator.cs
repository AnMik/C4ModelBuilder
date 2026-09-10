using System.Text;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.PlantUmlCreator;

/// <summary>
/// Создаёт PlantUML-диаграмму C4 Component на основе <see cref="C4ComponentDiagram"/>.
/// </summary>
public static class PlantUmlGenerator
{
    /// <summary>
    /// Генерирует C4 Component PlantUML из готового контекста.
    /// </summary>
    /// <param name="componentDiagram">Контекст с компонентами и связями.</param>
    /// <returns>Строка с PlantUML-диаграммой.</returns>
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
}
