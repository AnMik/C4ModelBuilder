namespace C4ModelBuilder.PlantUmlCreator;

/// <summary>
/// Внутренняя модель диаграммы C4 Component: компоненты и связи между ними.
/// Промежуточное представление между схлопыванием дерева вызовов и рендерингом PlantUML.
/// </summary>
/// <param name="Components">Компоненты (узлы) диаграммы.</param>
/// <param name="Relations">Связи (рёбра) между компонентами.</param>
internal sealed record C4ComponentDiagram(
    IReadOnlyCollection<C4ComponentDiagram.C4Component> Components,
    IReadOnlyCollection<C4ComponentDiagram.C4Relation> Relations)
{
    public enum Type
    {
        Normal,
        Repository,
        Gateway
    }

    public readonly record struct C4Component(string ComponentAlias, string ComponentName, string Description, Type Type = Type.Normal);

    public readonly record struct C4Relation(string FromComponentAlias, string ToComponentAlias);
}
