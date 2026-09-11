namespace C4ModelBuilder.Models.Analysis;

/// <summary>
/// Контекст для построения C4 диаграммы: коллекция компонентов и связей между ними.
/// </summary>
public sealed record C4ComponentDiagram(
    IReadOnlyCollection<C4ComponentDiagram.C4Component> Components,
    IReadOnlyCollection<C4ComponentDiagram.C4Relation> Relations)
{
    public readonly record struct C4Component(string ComponentAlias, string ComponentName, string Description);

    public readonly record struct C4Relation(string FromComponentAlias, string ToComponentAlias);

    /// <summary>
    /// Компоненты (узлы) диаграммы.
    /// </summary>
    public IReadOnlyCollection<C4Component> Components { get; } = Components;

    /// <summary>
    /// Связи (рёбра) между компонентами.
    /// </summary>
    public IReadOnlyCollection<C4Relation> Relations { get; } = Relations;
}
