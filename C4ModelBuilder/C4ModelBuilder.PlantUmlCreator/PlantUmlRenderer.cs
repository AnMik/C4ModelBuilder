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

        sb.AppendLine("@startuml")
          .AppendLine()
          .AppendLine("!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Component.puml")
          .AppendLine()
          .AppendLine("AddElementTag(\"repository\", $bgColor=\"#528354\", $fontColor=\"#ffffff\")")
          .AppendLine("AddElementTag(\"gateway\", $bgColor=\"#7b4286\", $fontColor=\"#ffffff\")")
          .AppendLine();

        foreach (var component in diagram.Components)
        {
            var tagArg = component.Type switch
            {
                C4ComponentDiagram.Type.Repository => ", $tags=\"repository\"",
                C4ComponentDiagram.Type.Gateway => ", $tags=\"gateway\"",
                _ => string.Empty
            };

            sb.AppendLine($"Component({component.ComponentAlias}, \"{component.ComponentName}\", \"{component.Description}\"{tagArg})");
        }

        if (diagram.Components.Count > 0 && diagram.Relations.Count > 0)
        {
            sb.AppendLine();
        }

        foreach (var relation in diagram.Relations)
        {
            sb.AppendLine($"Rel({relation.FromComponentAlias}, {relation.ToComponentAlias}, \"\")");
        }

        sb.AppendLine()
          .AppendLine("@enduml");

        return sb.ToString();
    }
}
