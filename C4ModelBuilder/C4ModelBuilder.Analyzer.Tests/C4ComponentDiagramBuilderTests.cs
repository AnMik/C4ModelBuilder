using System.Linq;
using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Analyzer.Tests;

[TestFixture]
public class C4ComponentDiagramBuilderTests
{
    [Test]
    public void Only_component_nodes_become_components()
    {
        var root = new InvocationTree("HomeController", isC4Component: true);
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", isC4Component: false));          // метод без флага
        root.AddInvocation(new InvocationTree("HomeController.SendAsync", isC4Component: true)); // метод-компонент

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(diagram, "HomeController", "HomeController.SendAsync");
    }

    [Test]
    public void Non_component_nodes_are_collapsed_into_relations_between_components()
    {
        var root = new InvocationTree("HomeController", isC4Component: true);
        var method = new InvocationTree("HomeController.GetUsersAsync", isC4Component: false);           // метод без флага
        var userApp = new InvocationTree("UserApplication", isC4Component: true);
        var innerMethod = new InvocationTree("UserApplication.GetUsersAsync", isC4Component: false);      // метод без флага
        var userService = new InvocationTree("UserService", isC4Component: true);
        innerMethod.AddInvocation(userService);
        userApp.AddInvocation(innerMethod);
        method.AddInvocation(userApp);
        root.AddInvocation(method);

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(diagram, "HomeController", "UserApplication", "UserService");
        AssertRelations(
            diagram,
            ("HomeController", "UserApplication"),
            ("UserApplication", "UserService"));
    }

    [Test]
    public void Method_calling_several_components_fans_out_into_several_relations()
    {
        var root = new InvocationTree("HomeController", isC4Component: true);
        var method = new InvocationTree("HomeController.GetUsersAsync", isC4Component: false);

        var userApp = new InvocationTree("UserApplication", isC4Component: true);
        userApp.AddInvocation(new InvocationTree("UserApplication.GetUsersAsync", isC4Component: false));

        var smsGateway = new InvocationTree("ISmsGateway", isC4Component: true);
        smsGateway.AddInvocation(new InvocationTree("ISmsGateway.SendAsync", isC4Component: false));

        method.AddInvocation(userApp);
        method.AddInvocation(smsGateway);
        root.AddInvocation(method);

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertRelations(
            diagram,
            ("HomeController", "UserApplication"),
            ("HomeController", "ISmsGateway"));
    }

    [Test]
    public void Interface_without_implementation_is_added_as_component()
    {
        var root = new InvocationTree("HomeController", isC4Component: true);
        var method = new InvocationTree("HomeController.GetUsersAsync", isC4Component: false);

        var smsGateway = new InvocationTree("ISmsGateway", isC4Component: true);
        smsGateway.AddInvocation(new InvocationTree("ISmsGateway.SendAsync", isC4Component: false));

        method.AddInvocation(smsGateway);
        root.AddInvocation(method);

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(diagram, "HomeController", "ISmsGateway");
    }

    [Test]
    public void Duplicate_nodes_and_relations_are_deduplicated()
    {
        var root = new InvocationTree("HomeController", isC4Component: true);
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", isC4Component: false));
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", isC4Component: false));

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(diagram, "HomeController");
        Assert.That(diagram.Relations, Is.Empty);
    }

    [Test]
    public void Component_root_without_children_stays_a_single_component()
    {
        var diagram = C4ComponentDiagramBuilder.Build(new InvocationTree("EmptyController", isC4Component: true));

        AssertComponents(diagram, "EmptyController");
        Assert.That(diagram.Relations, Is.Empty);
    }

    [Test]
    public void Non_component_root_produces_no_relations()
    {
        var root = new InvocationTree("RootContainer", isC4Component: false);                            // корень без флага
        root.AddInvocation(new InvocationTree("ComponentA", isC4Component: true));
        root.AddInvocation(new InvocationTree("ComponentB", isC4Component: true));

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(diagram, "ComponentA", "ComponentB");
        Assert.That(diagram.Relations, Is.Empty);                                 // нет предка → нет связей
    }

    [Test]
    public void Root_class_is_not_wrapped_by_a_pseudo_root_node()
    {
        var root = new InvocationTree("HomeController", isC4Component: true);
        root.AddInvocation(new InvocationTree("HomeController.GetUsersAsync", isC4Component: false));

        var diagram = C4ComponentDiagramBuilder.Build(root);

        var aliases = diagram.Components.Select(component => component.ComponentAlias).ToHashSet();
        var sources = diagram.Relations.Select(relation => relation.FromComponentAlias).ToHashSet();

        Assert.That(aliases, Does.Not.Contain("Root"));
        Assert.That(sources, Does.Not.Contain("Root"));
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

