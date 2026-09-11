using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.PlantUmlCreator.Tests;

[TestFixture]
public class PlantUmlGeneratorTests
{
    [Test]
    public void Generator_returns_only_header_and_footer_when_context_is_empty()
    {
        var ctx = new C4ComponentDiagram(Array.Empty<C4ComponentDiagram.C4Component>(), Array.Empty<C4ComponentDiagram.C4Relation>());

        var result = PlantUmlGenerator.Generate(ctx);

        Assert.That(result, Does.Contain("@startuml"));
        Assert.That(result, Does.Contain("@enduml"));
        Assert.That(result, Does.Not.Contain("Component("));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Generator_throws_when_context_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => PlantUmlGenerator.Generate((C4ComponentDiagram)null!));
    }

    [Test]
    public void Generator_renders_single_component_without_relations()
    {
        var ctx = new C4ComponentDiagram(
            Components: [new C4ComponentDiagram.C4Component("MyApp", "MyApp", string.Empty)],
            Relations: Array.Empty<C4ComponentDiagram.C4Relation>());

        var result = PlantUmlGenerator.Generate(ctx);

        Assert.That(result, Does.Contain("Component("));
        Assert.That(result, Does.Contain("\"MyApp\""));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Generator_renders_two_components_and_one_relation()
    {
        var ctx = new C4ComponentDiagram(
            Components: [
                new C4ComponentDiagram.C4Component("ServiceA", "ServiceA", string.Empty),
                new C4ComponentDiagram.C4Component("ServiceB", "ServiceB", string.Empty),
            ],
            Relations: [
                new C4ComponentDiagram.C4Relation("ServiceA", "ServiceB"),
            ]);

        var result = PlantUmlGenerator.Generate(ctx);

        // Два компонента
        Assert.That(result, Does.Contain("\"ServiceA\""));
        Assert.That(result, Does.Contain("\"ServiceB\""));
        // Одна связь
        Assert.That(CountStringOccurrences(result, "Rel("), Is.EqualTo(1));
    }

    [Test]
    public void Generator_renders_duplicate_components_as_they_are()
    {
        var ctx = new C4ComponentDiagram(
            Components: [
                new C4ComponentDiagram.C4Component("A", "A", string.Empty),
                new C4ComponentDiagram.C4Component("A", "A", string.Empty),
                new C4ComponentDiagram.C4Component("B", "B", string.Empty),
            ],
            Relations: [
                new C4ComponentDiagram.C4Relation("A", "B"),
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
    public void Generator_renders_component_macro_with_description_and_technology()
    {
        var ctx = new C4ComponentDiagram(
            Components: [new C4ComponentDiagram.C4Component("ProductService", "Product Service", "Service for managing products")],
            Relations: Array.Empty<C4ComponentDiagram.C4Relation>());

        var result = PlantUmlGenerator.Generate(ctx);

        Assert.That(result, Does.Contain("Component(ProductService, \"Product Service\", \"Service for managing products\")"));
    }

    [Test]
    public void Generator_renders_relation_macro_without_description()
    {
        var ctx = new C4ComponentDiagram(
            Components: [
                new C4ComponentDiagram.C4Component("A", "A", string.Empty),
                new C4ComponentDiagram.C4Component("B", "B", string.Empty),
            ],
            Relations: [
                new C4ComponentDiagram.C4Relation("A", "B"),
            ]);

        var result = PlantUmlGenerator.Generate(ctx);

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
// ===== Build(InvocationTree) tests =====

    [Test]
    public void Build_only_component_nodes_become_components()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null));
        root.AddInvocation(new InvocationTree("HomeController.SendAsync", c4ComponentDescription: string.Empty));

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "HomeController.SendAsync");
    }

    [Test]
    public void Build_non_component_nodes_are_collapsed_into_relations_between_components()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        var method = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var userApp = new InvocationTree("UserApplication", c4ComponentDescription: string.Empty);
        var innerMethod = new InvocationTree("UserApplication.GetUsersAsync", c4ComponentDescription: null);
        var userService = new InvocationTree("UserService", c4ComponentDescription: string.Empty);
        innerMethod.AddInvocation(userService);
        userApp.AddInvocation(innerMethod);
        method.AddInvocation(userApp);
        root.AddInvocation(method);

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "UserApplication", "UserService");
        AssertRelations(
            diagram,
            ("HomeController", "UserApplication"),
            ("UserApplication", "UserService"));
    }

    [Test]
    public void Build_method_calling_several_components_fans_out_into_several_relations()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        var method = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var userApp = new InvocationTree("UserApplication", c4ComponentDescription: string.Empty);
        userApp.AddInvocation(new InvocationTree("UserApplication.GetUsersAsync", c4ComponentDescription: null));
        var smsGateway = new InvocationTree("ISmsGateway", c4ComponentDescription: string.Empty);
        smsGateway.AddInvocation(new InvocationTree("ISmsGateway.SendAsync", c4ComponentDescription: null));
        method.AddInvocation(userApp);
        method.AddInvocation(smsGateway);
        root.AddInvocation(method);

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        AssertRelations(diagram,
            ("HomeController", "UserApplication"),
            ("HomeController", "ISmsGateway"));
    }

    [Test]
    public void Build_interface_without_implementation_is_added_as_component()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        var method = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var smsGateway = new InvocationTree("ISmsGateway", c4ComponentDescription: string.Empty);
        smsGateway.AddInvocation(new InvocationTree("ISmsGateway.SendAsync", c4ComponentDescription: null));
        method.AddInvocation(smsGateway);
        root.AddInvocation(method);

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "ISmsGateway");
    }

    [Test]
    public void Build_duplicate_nodes_and_relations_are_deduplicated()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        var method1 = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var method2 = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        method1.AddInvocation(new InvocationTree("UserApplication", c4ComponentDescription: string.Empty));
        method2.AddInvocation(new InvocationTree("UserApplication", c4ComponentDescription: string.Empty));
        root.AddInvocation(method1);
        root.AddInvocation(method2);

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "UserApplication");
        AssertRelations(diagram, ("HomeController", "UserApplication"));
    }
[Test]
    public void Build_non_component_root_produces_no_relations()
    {
        var root = new InvocationTree("HomeController.Foo", c4ComponentDescription: null);
        root.AddInvocation(new InvocationTree("UserApplication", c4ComponentDescription: string.Empty));

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        Assert.That(diagram.Components.Select(component => component.ComponentAlias), Does.Contain("UserApplication"));
        Assert.That(diagram.Relations, Is.Empty);
    }

    [Test]
    public void Build_root_class_is_not_wrapped_by_a_pseudo_root_node()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null));

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        var aliases = diagram.Components.Select(component => component.ComponentAlias).ToHashSet();
        var sources = diagram.Relations.Select(relation => relation.FromComponentAlias).ToHashSet();

        Assert.That(aliases, Does.Not.Contain("Root"));
        Assert.That(sources, Does.Not.Contain("Root"));
    }

    [Test]
    public void Build_component_description_is_preserved()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: "Main controller");
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null));
        root.AddInvocation(new InvocationTree("UserService", c4ComponentDescription: "User business logic"));

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "UserService");

        var homeController = diagram.Components.Single(c => c.ComponentAlias == "HomeController");
        Assert.That(homeController.Description, Is.EqualTo("Main controller"));

        var userService = diagram.Components.Single(c => c.ComponentAlias == "UserService");
        Assert.That(userService.Description, Is.EqualTo("User business logic"));
    }

    [Test]
    public void Build_component_without_description_stores_empty_string()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        var component = diagram.Components.Single();
        Assert.That(component.Description, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Build_non_component_node_description_is_not_stored()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: "Controller");
        var method = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var userApp = new InvocationTree("UserApplication", c4ComponentDescription: "User app logic");
        method.AddInvocation(userApp);
        root.AddInvocation(method);

        var diagram = PlantUmlGenerator.BuildComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "UserApplication");

        var homeController = diagram.Components.Single(c => c.ComponentAlias == "HomeController");
        Assert.That(homeController.Description, Is.EqualTo("Controller"));

        var userApplication = diagram.Components.Single(c => c.ComponentAlias == "UserApplication");
        Assert.That(userApplication.Description, Is.EqualTo("User app logic"));
    }

    private static void AssertComponents(C4ComponentDiagram diagram, params string[] expected)
    {
        var actual = diagram.Components.Select(component => component.ComponentAlias).ToHashSet();
        CollectionAssert.AreEquivalent(expected, actual);
    }

    private static void AssertRelations(C4ComponentDiagram diagram, params (string From, string To)[] expected)
    {
        var actual = diagram.Relations
            .Select(relation => (relation.FromComponentAlias, relation.ToComponentAlias))
            .ToHashSet();

        CollectionAssert.AreEquivalent(expected, actual);
    }
}
