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
        Assert.That(result, Does.Not.Contain("Rel("));
        Assert.That(result, Does.Contain("Component(HomeController, \"HomeController\", \"\", \"Main controller\")"));
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

        Assert.That(result, Does.Contain("Component(HomeController, \"HomeController\", \"\", \"Main controller\")"));
        Assert.That(result, Does.Contain("Component(UserService, \"UserService\", \"\", \"User business logic\")"));
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

    [Test]
    public void Generator_renders_a_call_to_a_method_of_the_same_class_as_a_relation_between_the_methods()
    {
        var tree = new InvocationTree("ClassX", c4ComponentDescription: "Class description");
        var methodA = new InvocationTree("ClassX.MethodA", c4ComponentDescription: "Method A description");
        var sameClassCall = new InvocationTree("ClassX", c4ComponentDescription: "Class description")
        {
            IsSameClassCall = true,
        };
        var methodB = new InvocationTree("ClassX.MethodB", c4ComponentDescription: "Method B description");
        sameClassCall.AddInvocation(methodB);
        methodA.AddInvocation(sameClassCall);
        tree.AddInvocation(methodA);

        var result = PlantUmlGenerator.Generate(tree);

        Assert.That(result, Does.Contain("Rel(ClassX.MethodA, ClassX.MethodB, \"\")"));
        Assert.That(result, Does.Contain("Rel(ClassX, ClassX.MethodB, \"\")"));
        Assert.That(result, Does.Not.Contain("Rel(ClassX.MethodA, ClassX, \"\")"));
    }

    [Test]
    public void Generator_puts_technology_third_and_description_fourth()
    {
        var tree = new InvocationTree(
            "HomeController",
            c4ComponentDescription: "Main controller",
            c4ComponentTechnology: "ASP.NET Core");
        var repository = new InvocationTree(
            "UserRepository",
            c4ComponentDescription: "User data repository",
            c4ComponentTechnology: "EF Core Repository");
        tree.AddInvocation(repository);

        var result = PlantUmlGenerator.Generate(tree);

        Assert.That(
            result,
            Does.Contain("Component(HomeController, \"HomeController\", \"ASP.NET Core\", \"Main controller\")"));
        Assert.That(
            result,
            Does.Contain(
                "Component(UserRepository, \"UserRepository\", \"EF Core Repository\", \"User data repository\", $tags=\"repository\")"));
    }
}
