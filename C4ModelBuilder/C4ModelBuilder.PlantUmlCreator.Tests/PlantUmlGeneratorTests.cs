using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.PlantUmlCreator.Tests;

[TestFixture]
public class PlantUmlGeneratorTests
{
    [Test]
    public void Generator_throws_when_tree_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => PlantUmlGenerator.Generate(null!));
    }

    [Test]
    public void Generator_produces_empty_diagram_for_component_without_invocations()
    {
        var tree = new InvocationTree("HomeController", c4ComponentDescription: "Main controller");

        var result = PlantUmlGenerator.Generate(tree);

        Assert.That(result, Does.Contain("@startuml"));
        Assert.That(result, Does.Contain("@enduml"));
        Assert.That(result, Does.Contain("Component(HomeController, \"HomeController\", \"Main controller\")"));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Generator_collapses_call_tree_into_relations_between_components()
    {
        var tree = new InvocationTree("HomeController", c4ComponentDescription: "Main controller");
        var method = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var userService = new InvocationTree("UserService", c4ComponentDescription: "User business logic");
        method.AddInvocation(userService);
        tree.AddInvocation(method);

        var result = PlantUmlGenerator.Generate(tree);

        Assert.That(result, Does.Contain("Component(HomeController, \"HomeController\", \"Main controller\")"));
        Assert.That(result, Does.Contain("Component(UserService, \"UserService\", \"User business logic\")"));
        Assert.That(result, Does.Contain("Rel(HomeController, UserService, \"\")"));
        Assert.That(result, Does.Not.Contain("HomeController.GetUsersAsync"));
    }
}
