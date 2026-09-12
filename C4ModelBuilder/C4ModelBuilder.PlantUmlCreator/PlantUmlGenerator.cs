using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.PlantUmlCreator;

/// <summary>
/// Публичная точка входа генератора: строит PlantUML-диаграмму C4 Component из дерева вызовов.
/// Внутренние этапы разделены на <see cref="InvocationTreeMerger"/> (схлопывание дерева
/// в модель диаграммы) и <see cref="PlantUmlRenderer"/> (формирование текста PlantUML).
/// </summary>
public static class PlantUmlGenerator
{
    /// <summary>
    /// Генерирует текст C4 Component PlantUML диаграммы из дерева вызовов.
    /// </summary>
    public static string Generate(InvocationTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var componentDiagram = InvocationTreeMerger.MergeToComponentDiagram(tree);

        return PlantUmlRenderer.Render(componentDiagram);
    }
}
