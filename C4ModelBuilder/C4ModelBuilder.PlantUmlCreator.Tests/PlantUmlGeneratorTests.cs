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

    [Test]
    public void Generator_internal_calls_inside_a_component_do_not_render_self_relations()
    {
        var tree = new InvocationTree("AccountController", c4ComponentDescription: "Account API");
        var method = new InvocationTree("AccountController.GetStoredPaymentCards", c4ComponentDescription: null);
        var internalClassCall = new InvocationTree("AccountController", c4ComponentDescription: "Account API");
        var constructor = new InvocationTree(
            "GetStoredPaymentCardsViewModelConstructor",
            c4ComponentDescription: "Stored payment cards view model constructor");
        internalClassCall.AddInvocation(constructor);
        method.AddInvocation(internalClassCall);
        tree.AddInvocation(method);

        var result = PlantUmlGenerator.Generate(tree);

        Assert.That(result, Does.Not.Contain("Rel(AccountController, AccountController, \"\")"));
        Assert.That(
            result,
            Does.Not.Contain(
                "Rel(GetStoredPaymentCardsViewModelConstructor, GetStoredPaymentCardsViewModelConstructor, \"\")"));
        Assert.That(result, Does.Contain("Rel(AccountController, GetStoredPaymentCardsViewModelConstructor, \"\")"));
    }
}
