namespace C4ModelBuilder.Models.Analysis;

/// <summary>
/// Контекст для построения C4 диаграммы: коллекция компонентов и связей между ними.
/// </summary>
/// <param name="Components">Компоненты (узлы) диаграммы.</param>
/// <param name="Relations">Связи (рёбра) между компонентами.</param>
public sealed record C4ComponentDiagram(
    IReadOnlyCollection<C4ComponentDiagram.C4Component> Components,
    IReadOnlyCollection<C4ComponentDiagram.C4Relation> Relations)
{
    public readonly record struct C4Component(string ComponentAlias, string ComponentName, string Description);

    public readonly record struct C4Relation(string FromComponentAlias, string ToComponentAlias);
}
