namespace C4ModelBuilder.Models;

/// <summary>
/// Контекст для построения C4 диаграммы: коллекция компонентов и связей между ними.
/// </summary>
public sealed record PlantUmlC4ComponentDiagram(IReadOnlyList<C4Component> Components, IReadOnlyList<C4Relation> Relations)
{
    /// <summary>
    /// Компоненты (узлы) диаграммы.
    /// </summary>
    public IReadOnlyList<C4Component> Components { get; } = Components;

    /// <summary>
    /// Связи (рёбра) между компонентами.
    /// </summary>
    public IReadOnlyList<C4Relation> Relations { get; } = Relations;
}
