using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.PlantUmlCreator.Tests;

[TestFixture]
public class PlantUmlGeneratorTests
{
    [Test]
    public void Generate_WithEmptyContext_ShouldReturnOnlyHeaderAndFooter()
    {
        var ctx = new C4ComponentDiagram(Array.Empty<C4Component>(), Array.Empty<C4Relation>());

        var result = PlantUmlGenerator.Generate(ctx);

        Assert.That(result, Does.Contain("@startuml"));
        Assert.That(result, Does.Contain("@enduml"));
        Assert.That(result, Does.Not.Contain("Component("));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Generate_WithNullContext_ShouldThrowException()
    {
        Assert.Throws<ArgumentNullException>(() => PlantUmlGenerator.Generate(null!));
    }

    [Test]
    public void Generate_WithSingleComponentNoRelations_ShouldProduceOneComponentNoRelations()
    {
        var ctx = new C4ComponentDiagram(Components: [new C4Component("MyApp", "MyApp", string.Empty)], Array.Empty<C4Relation>());

        var result = PlantUmlGenerator.Generate(ctx);

        Assert.That(result, Does.Contain("Component("));
        Assert.That(result, Does.Contain("\"MyApp\""));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Generate_WithTwoComponentsAndOneRelation_ShouldProduceTwoComponentsOneRelation()
    {
        var ctx = new C4ComponentDiagram(
            Components:
            [
                new C4Component("ServiceA", "ServiceA", string.Empty),
                new C4Component("ServiceB", "ServiceB", string.Empty),
            ],
            Relations:
            [
                new C4Relation("ServiceA", "ServiceB", string.Empty),
            ]);

        var result = PlantUmlGenerator.Generate(ctx);

        // Два компонента
        Assert.That(result, Does.Contain("\"ServiceA\""));
        Assert.That(result, Does.Contain("\"ServiceB\""));
        // Одна связь
        Assert.That(CountStringOccurrences(result, "Rel("), Is.EqualTo(1));
    }

    [Test]
    public void Generate_WithDuplicateComponents_ShouldRenderAllComponents()
    {
        var ctx = new C4ComponentDiagram(
            Components:
            [
                new C4Component("A", "A", string.Empty),
                new C4Component("A", "A", string.Empty),
                new C4Component("B", "B", string.Empty),
            ],
            Relations:
            [
                new C4Relation("A", "B", string.Empty),
            ]);

        var result = PlantUmlGenerator.Generate(ctx);

        // Компоненты рендерятся как есть (дедупликация — ответственность создателя контекста)
        var componentLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.TrimStart().StartsWith("Component("));
        Assert.That(componentLines, Is.EqualTo(3));

        // Одна связь (A → B)
        var relLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.TrimStart().StartsWith("Rel("));
        Assert.That(relLines, Is.EqualTo(1));
    }

    [Test]
    public void Generate_WithComponentWithDescriptionAndTechnology_ShouldRenderCorrectMacro()
    {
        var ctx = new C4ComponentDiagram(
            Components:
            [
                new C4Component("ProductService", "Product Service", "Service for managing products"),
            ],
            Array.Empty<C4Relation>());

        var result = PlantUmlGenerator.Generate(ctx);

        Assert.That(result, Does.Contain("Component(ProductService, \"Product Service\", \"Service for managing products\")"));
    }

    [Test]
    public void Generate_WithRelationWithDescriptionAndTechnology_ShouldRenderCorrectMacro()
    {
        var ctx = new C4ComponentDiagram(
            Components:
            [
                new C4Component("A", "A", string.Empty),
                new C4Component("B", "B", string.Empty),
            ],
            Relations:
            [
                new C4Relation("A", "B", "calls"),
            ]);

        var result = PlantUmlGenerator.Generate(ctx);

        Assert.That(result, Does.Contain("Rel(A, B, \"calls\")"));
    }

    private static int CountStringOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
