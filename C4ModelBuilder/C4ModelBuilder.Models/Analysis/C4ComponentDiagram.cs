namespace C4ModelBuilder.Models.Analysis;

/// <summary>
/// Контекст для построения C4 диаграммы: коллекция компонентов и связей между ними.
/// </summary>
public sealed record C4ComponentDiagram(
    string DiagramName,
    IReadOnlyCollection<C4Component> Components,
    IReadOnlyCollection<C4Relation> Relations)
{
    public string DiagramName { get; init; } = DiagramName;

    /// <summary>
    /// Компоненты (узлы) диаграммы.
    /// </summary>
    public IReadOnlyCollection<C4Component> Components { get; } = Components;

    /// <summary>
    /// Связи (рёбра) между компонентами.
    /// </summary>
    public IReadOnlyCollection<C4Relation> Relations { get; } = Relations;
}
