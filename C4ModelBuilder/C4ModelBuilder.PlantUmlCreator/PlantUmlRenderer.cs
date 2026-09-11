using System.Text;

namespace C4ModelBuilder.PlantUmlCreator;

/// <summary>
/// Формирует текст PlantUML (C4 Component) из внутренней модели диаграммы <see cref="C4ComponentDiagram"/>.
/// </summary>
internal static class PlantUmlRenderer
{
    public static string Render(C4ComponentDiagram diagram)
    {
        ArgumentNullException.ThrowIfNull(diagram);

        var sb = new StringBuilder();

        sb
            .AppendLine("@startuml")
            .AppendLine()
            .AppendLine("!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Component.puml")
            .AppendLine();

        foreach (var component in diagram.Components)
        {
            sb.AppendLine($"Component({component.ComponentAlias}, \"{component.ComponentName}\", \"{component.Description}\")");
        }

        if (diagram.Components.Count > 0 && diagram.Relations.Count > 0)
        {
            sb.AppendLine();
        }

        foreach (var relation in diagram.Relations)
        {
            sb.AppendLine($"Rel({relation.FromComponentAlias}, {relation.ToComponentAlias}, \"\")");
        }

        sb
            .AppendLine()
            .AppendLine("@enduml");

        return sb.ToString();
    }
}
