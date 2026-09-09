using System.Linq;
using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Analyzer.Tests;

[TestFixture]
public class C4ComponentDiagramBuilderTests
{
    [Test]
    public void RootClass_and_each_of_its_public_methods_are_separate_components()
    {
        var root = new MemberNode("HomeController");
        root.AddChild(new MemberNode("HomeController.GetUsersAsync"));
        root.AddChild(new MemberNode("HomeController.SendAsync"));

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(
            diagram,
            "HomeController",
            "HomeController.GetUsersAsync",
            "HomeController.SendAsync");

        AssertRelations(
            diagram,
            ("HomeController", "HomeController.GetUsersAsync"),
            ("HomeController", "HomeController.SendAsync"));
    }

    [Test]
    public void Parent_child_edges_become_relations_along_type_method_chain()
    {
        var root = new MemberNode("HomeController");
        var method = new MemberNode("HomeController.GetUsersAsync");
        var applicationType = new MemberNode("UserApplication");
        applicationType.AddChild(new MemberNode("UserApplication.GetUsersAsync"));
        method.AddChild(applicationType);
        root.AddChild(method);

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(
            diagram,
            "HomeController",
            "HomeController.GetUsersAsync",
            "UserApplication",
            "UserApplication.GetUsersAsync");

        AssertRelations(
            diagram,
            ("HomeController", "HomeController.GetUsersAsync"),
            ("HomeController.GetUsersAsync", "UserApplication"),
            ("UserApplication", "UserApplication.GetUsersAsync"));
    }

    [Test]
    public void Method_calling_several_types_fans_out_into_several_edges()
    {
        var root = new MemberNode("HomeController");
        var method = new MemberNode("HomeController.GetUsersAsync");

        var applicationType = new MemberNode("UserApplication");
        applicationType.AddChild(new MemberNode("UserApplication.GetUsersAsync"));

        var smsGatewayType = new MemberNode("ISmsGateway");
        smsGatewayType.AddChild(new MemberNode("ISmsGateway.SendAsync"));

        method.AddChild(applicationType);
        method.AddChild(smsGatewayType);
        root.AddChild(method);

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertRelations(
            diagram,
            ("HomeController", "HomeController.GetUsersAsync"),
            ("HomeController.GetUsersAsync", "UserApplication"),
            ("HomeController.GetUsersAsync", "ISmsGateway"),
            ("UserApplication", "UserApplication.GetUsersAsync"),
            ("ISmsGateway", "ISmsGateway.SendAsync"));
    }

    [Test]
    public void Interface_without_implementation_is_added_with_its_leaf_method()
    {
        var root = new MemberNode("HomeController");
        var method = new MemberNode("HomeController.GetUsersAsync");

        var gatewayType = new MemberNode("ISmsGateway");
        gatewayType.AddChild(new MemberNode("ISmsGateway.SendAsync"));

        method.AddChild(gatewayType);
        root.AddChild(method);

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(
            diagram,
            "HomeController",
            "HomeController.GetUsersAsync",
            "ISmsGateway",
            "ISmsGateway.SendAsync");
    }

    [Test]
    public void Duplicate_nodes_and_relations_are_deduplicated()
    {
        var root = new MemberNode("HomeController");
        root.AddChild(new MemberNode("HomeController.GetUsersAsync"));
        root.AddChild(new MemberNode("HomeController.GetUsersAsync"));

        var diagram = C4ComponentDiagramBuilder.Build(root);

        AssertComponents(diagram, "HomeController", "HomeController.GetUsersAsync");
        AssertRelations(diagram, ("HomeController", "HomeController.GetUsersAsync"));
    }

    [Test]
    public void Class_without_public_methods_stays_a_single_component()
    {
        var diagram = C4ComponentDiagramBuilder.Build(new MemberNode("EmptyController"));

        AssertComponents(diagram, "EmptyController");
        Assert.That(diagram.Relations, Is.Empty);
    }

    [Test]
    public void Root_class_is_not_wrapped_by_a_pseudo_root_node()
    {
        var root = new MemberNode("HomeController");
        root.AddChild(new MemberNode("HomeController.GetUsersAsync"));

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

