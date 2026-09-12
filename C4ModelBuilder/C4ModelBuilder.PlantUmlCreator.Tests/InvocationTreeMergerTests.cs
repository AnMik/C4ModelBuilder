using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.PlantUmlCreator.Tests;

[TestFixture]
public class InvocationTreeMergerTests
{
    [Test]
    public void Merge_only_component_nodes_become_components()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null));
        root.AddInvocation(new InvocationTree("HomeController.SendAsync", c4ComponentDescription: string.Empty));

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "HomeController.SendAsync");
    }

    [Test]
    public void Merge_non_component_nodes_are_collapsed_into_relations_between_components()
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

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "UserApplication", "UserService");
        AssertRelations(
            diagram,
            ("HomeController", "UserApplication"),
            ("UserApplication", "UserService"));
    }

    [Test]
    public void Merge_method_calling_several_components_fans_out_into_several_relations()
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

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertRelations(
            diagram,
            ("HomeController", "UserApplication"),
            ("HomeController", "ISmsGateway"));
    }

    [Test]
    public void Merge_interface_without_implementation_is_added_as_component()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        var method = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var smsGateway = new InvocationTree("ISmsGateway", c4ComponentDescription: string.Empty);
        smsGateway.AddInvocation(new InvocationTree("ISmsGateway.SendAsync", c4ComponentDescription: null));
        method.AddInvocation(smsGateway);
        root.AddInvocation(method);

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "ISmsGateway");
    }

    [Test]
    public void Merge_duplicate_nodes_and_relations_are_deduplicated()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        var method1 = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var method2 = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        method1.AddInvocation(new InvocationTree("UserApplication", c4ComponentDescription: string.Empty));
        method2.AddInvocation(new InvocationTree("UserApplication", c4ComponentDescription: string.Empty));
        root.AddInvocation(method1);
        root.AddInvocation(method2);

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "UserApplication");
        AssertRelations(diagram, ("HomeController", "UserApplication"));
    }

    [Test]
    public void Merge_non_component_root_produces_no_relations()
    {
        var root = new InvocationTree("HomeController.Foo", c4ComponentDescription: null);
        root.AddInvocation(new InvocationTree("UserApplication", c4ComponentDescription: string.Empty));

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        Assert.That(diagram.Components.Select(component => component.ComponentAlias), Does.Contain("UserApplication"));
        Assert.That(diagram.Relations, Is.Empty);
    }

    [Test]
    public void Merge_root_class_is_not_wrapped_by_a_pseudo_root_node()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null));

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        var aliases = diagram.Components.Select(component => component.ComponentAlias).ToHashSet();
        var sources = diagram.Relations.Select(relation => relation.FromComponentAlias).ToHashSet();

        Assert.That(aliases, Does.Not.Contain("Root"));
        Assert.That(sources, Does.Not.Contain("Root"));
    }

    [Test]
    public void Merge_component_description_is_preserved()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: "Main controller");
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null));
        root.AddInvocation(new InvocationTree("UserService", c4ComponentDescription: "User business logic"));

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "UserService");

        var homeController = diagram.Components.Single(c => c.ComponentAlias == "HomeController");
        Assert.That(homeController.Description, Is.EqualTo("Main controller"));

        var userService = diagram.Components.Single(c => c.ComponentAlias == "UserService");
        Assert.That(userService.Description, Is.EqualTo("User business logic"));
    }

    [Test]
    public void Merge_component_without_description_stores_empty_string()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: string.Empty);

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        var component = diagram.Components.Single();
        Assert.That(component.Description, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Merge_non_component_node_description_is_not_stored()
    {
        var root = new InvocationTree("HomeController", c4ComponentDescription: "Controller");
        var method = new InvocationTree("HomeController.GetUsersAsync", c4ComponentDescription: null);
        var userApp = new InvocationTree("UserApplication", c4ComponentDescription: "User app logic");
        method.AddInvocation(userApp);
        root.AddInvocation(method);

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertComponents(diagram, "HomeController", "UserApplication");

        var homeController = diagram.Components.Single(c => c.ComponentAlias == "HomeController");
        Assert.That(homeController.Description, Is.EqualTo("Controller"));

        var userApplication = diagram.Components.Single(c => c.ComponentAlias == "UserApplication");
        Assert.That(userApplication.Description, Is.EqualTo("User app logic"));
    }

    [Test]
    public void Merge_internal_call_to_the_same_component_does_not_produce_a_self_relation()
    {
        var root = new InvocationTree("AccountController", c4ComponentDescription: "Account API");
        var method = new InvocationTree("AccountController.GetLoyaltyCard", c4ComponentDescription: null);
        var internalClassCall = new InvocationTree("AccountController", c4ComponentDescription: "Account API");
        var internalMethodCall = new InvocationTree("AccountController.GetLoyaltyCard", c4ComponentDescription: null);
        internalClassCall.AddInvocation(internalMethodCall);
        method.AddInvocation(internalClassCall);
        root.AddInvocation(method);

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertComponents(diagram, "AccountController");
        Assert.That(diagram.Relations, Is.Empty);
    }

    [Test]
    public void Merge_internal_call_to_the_same_component_still_relates_the_component_to_downstream_components()
    {
        var root = new InvocationTree("AccountController", c4ComponentDescription: "Account API");
        var method = new InvocationTree("AccountController.DeleteLoyaltyCard", c4ComponentDescription: null);
        var internalClassCall = new InvocationTree("AccountController", c4ComponentDescription: "Account API");
        var userService = new InvocationTree("UserService", c4ComponentDescription: "User business logic");
        internalClassCall.AddInvocation(userService);
        method.AddInvocation(internalClassCall);
        root.AddInvocation(method);

        var diagram = InvocationTreeMerger.MergeToComponentDiagram(root);

        AssertComponents(diagram, "AccountController", "UserService");
        AssertRelations(diagram, ("AccountController", "UserService"));
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
