namespace C4ModelBuilder.PlantUmlCreator.Tests;

[TestFixture]
public class PlantUmlRendererTests
{
    [Test]
    public void Render_returns_only_header_and_footer_when_diagram_is_empty()
    {
        var diagram = new C4ComponentDiagram(
            Array.Empty<C4ComponentDiagram.C4Component>(),
            Array.Empty<C4ComponentDiagram.C4Relation>());

        var result = PlantUmlRenderer.Render(diagram);

        Assert.That(result, Does.Contain("@startuml"));
        Assert.That(result, Does.Contain("@enduml"));
        Assert.That(result, Does.Not.Contain("Component("));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Render_throws_when_diagram_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => PlantUmlRenderer.Render(null!));
    }

    [Test]
    public void Render_renders_single_component_without_relations()
    {
        var diagram = new C4ComponentDiagram(
            Components: [new C4ComponentDiagram.C4Component("MyApp", "MyApp", string.Empty)],
            Relations: Array.Empty<C4ComponentDiagram.C4Relation>());

        var result = PlantUmlRenderer.Render(diagram);

        Assert.That(result, Does.Contain("Component("));
        Assert.That(result, Does.Contain("\"MyApp\""));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Render_renders_two_components_and_one_relation()
    {
        var diagram = new C4ComponentDiagram(
            Components: [
                new C4ComponentDiagram.C4Component("ServiceA", "ServiceA", string.Empty),
                new C4ComponentDiagram.C4Component("ServiceB", "ServiceB", string.Empty),
            ],
            Relations: [
                new C4ComponentDiagram.C4Relation("ServiceA", "ServiceB"),
            ]);

        var result = PlantUmlRenderer.Render(diagram);

        // Два компонента
        Assert.That(result, Does.Contain("\"ServiceA\""));
        Assert.That(result, Does.Contain("\"ServiceB\""));
        // Одна связь
        Assert.That(CountStringOccurrences(result, "Rel("), Is.EqualTo(1));
    }

    [Test]
    public void Render_renders_duplicate_components_as_they_are()
    {
        var diagram = new C4ComponentDiagram(
            Components: [
                new C4ComponentDiagram.C4Component("A", "A", string.Empty),
                new C4ComponentDiagram.C4Component("A", "A", string.Empty),
                new C4ComponentDiagram.C4Component("B", "B", string.Empty),
            ],
            Relations: [
                new C4ComponentDiagram.C4Relation("A", "B"),
            ]);

        var result = PlantUmlRenderer.Render(diagram);

        // Рендер выводит модель как есть (дедупликация — ответственность этапа схлопывания)
        var componentLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.TrimStart().StartsWith("Component("));
        Assert.That(componentLines, Is.EqualTo(3));

        // Одна связь (A → B)
        var relLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.TrimStart().StartsWith("Rel("));
        Assert.That(relLines, Is.EqualTo(1));
    }

    [Test]
    public void Render_renders_component_macro_with_description()
    {
        var diagram = new C4ComponentDiagram(
            Components: [new C4ComponentDiagram.C4Component("ProductService", "Product Service", "Service for managing products")],
            Relations: Array.Empty<C4ComponentDiagram.C4Relation>());

        var result = PlantUmlRenderer.Render(diagram);

        Assert.That(result, Does.Contain("Component(ProductService, \"Product Service\", \"Service for managing products\")"));
    }

    [Test]
    public void Render_renders_relation_macro_without_description()
    {
        var diagram = new C4ComponentDiagram(
            Components: [
                new C4ComponentDiagram.C4Component("A", "A", string.Empty),
                new C4ComponentDiagram.C4Component("B", "B", string.Empty),
            ],
            Relations: [
                new C4ComponentDiagram.C4Relation("A", "B"),
            ]);

        var result = PlantUmlRenderer.Render(diagram);

        Assert.That(result, Does.Contain("Rel(A, B, \"\")"));
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

    [Test]
    public void Render_renders_repository_component_with_tag()
    {
        var diagram = new C4ComponentDiagram(
            Components:
            [
                new C4ComponentDiagram.C4Component("UserRepository", "UserRepository", "User DB", C4ComponentDiagram.Type.Repository)
            ],
            Relations: Array.Empty<C4ComponentDiagram.C4Relation>());

        var result = PlantUmlRenderer.Render(diagram);

        Assert.That(result, Does.Contain("Component(UserRepository, \"UserRepository\", \"User DB\", $tags=\"repository\")"));
    }

    [Test]
    public void Render_renders_gateway_component_with_tag()
    {
        var diagram = new C4ComponentDiagram(
            Components: [new C4ComponentDiagram.C4Component("SmsGateway", "SmsGateway", "External SMS", C4ComponentDiagram.Type.Gateway)],
            Relations: Array.Empty<C4ComponentDiagram.C4Relation>());

        var result = PlantUmlRenderer.Render(diagram);

        Assert.That(result, Does.Contain("Component(SmsGateway, \"SmsGateway\", \"External SMS\", $tags=\"gateway\")"));
    }
}
