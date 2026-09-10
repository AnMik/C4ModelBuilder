using C4ModelBuilder.Models.Analysis;
using Microsoft.Build.Locator;

namespace C4ModelBuilder.Analyzer.Tests;

[TestFixture]
public class SolutionAnalyzerIntegrationTests
{
    private IReadOnlyCollection<C4ComponentDiagram>? _diagrams;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        RegisterMsBuild();

        var analyzer = await SolutionAnalyzer.Create(GetCurrentSolutionPath(), maxDepth: 15, CancellationToken.None);

        _diagrams = await analyzer
            .AnalyzeComponents(CancellationToken.None)
            .ToListAsync(CancellationToken.None);
    }

    [Test]
    public void Home_controller_root_class_produces_expected_call_tree()
    {
        Assert.That(_diagrams!.Count, Is.EqualTo(2));

        var home = DiagramByRootAlias("HomeController");

        AssertGraph(
            home,
            Edge("HomeController", "HomeController.GetUsersAsync"),
            Edge("HomeController.GetUsersAsync", "UserApplication", "ISmsGateway", "GetUsersHandler"),
            Edge("UserApplication", "UserApplication.GetUsersAsync"),
            Edge("UserApplication.GetUsersAsync", "UserService"),
            Edge("UserService", "UserService.GetUsersAsync"),
            Edge("UserService.GetUsersAsync", "UserRepository"),
            Edge("UserRepository", "UserRepository.GetAll"),
            Edge("UserRepository.GetAll", "UserDataSource"),
            Edge("UserDataSource", "UserDataSource.FetchAll"),
            Edge("GetUsersHandler", "GetUsersHandler.HandleAsync"),
            Edge("GetUsersHandler.HandleAsync", "UserApplication"),
            Edge("ISmsGateway", "ISmsGateway.SendAsync"));

        Assert.That(home.Components.Select(component => component.ComponentAlias), Does.Not.Contain("AdminController"));
    }

    [Test]
    public void Admin_controller_root_class_produces_expected_call_tree()
    {
        var admin = DiagramByRootAlias("AdminController");

        AssertGraph(
            admin,
            Edge("AdminController", "AdminController.SendPromoAsync"),
            Edge("AdminController.SendPromoAsync", "UserApplication", "ISmsGateway"),
            Edge("UserApplication", "UserApplication.GetUsersAsync"),
            Edge("UserApplication.GetUsersAsync", "UserService"),
            Edge("UserService", "UserService.GetUsersAsync"),
            Edge("UserService.GetUsersAsync", "UserRepository"),
            Edge("UserRepository", "UserRepository.GetAll"),
            Edge("UserRepository.GetAll", "UserDataSource"),
            Edge("UserDataSource", "UserDataSource.FetchAll"),
            Edge("ISmsGateway", "ISmsGateway.SendAsync"));

        Assert.That(admin.Components.Select(component => component.ComponentAlias), Does.Not.Contain("HomeController"));
    }

    [Test]
    public async Task Analysis_depth_limits_how_deep_a_call_tree_is_expanded()
    {
        var analyzer = await SolutionAnalyzer.Create(GetCurrentSolutionPath(), maxDepth: 1, CancellationToken.None);

        var shallowDiagrams = await analyzer
            .AnalyzeComponents(CancellationToken.None)
            .ToListAsync(CancellationToken.None);

        var home = shallowDiagrams.Single(diagram => diagram.Components.Any(component => component.ComponentAlias == "HomeController"));
        var componentAliases = home.Components.Select(component => component.ComponentAlias).ToHashSet();

        Assert.That(componentAliases, Is.SupersetOf(new[]
            {
                "HomeController", "HomeController.GetUsersAsync",
                "UserApplication", "UserApplication.GetUsersAsync",
                "GetUsersHandler", "GetUsersHandler.HandleAsync",
                "ISmsGateway",
            }));

        Assert.That(componentAliases.Intersect(
            [
                "UserService", "UserService.GetUsersAsync",
                "UserRepository", "UserRepository.GetAll",
                "UserDataSource", "UserDataSource.FetchAll",
            ]), Is.Empty);
    }


    private C4ComponentDiagram DiagramByRootAlias(string rootAlias)
        => _diagrams!.Single(
            diagram => diagram.Components.Any(component => component.ComponentAlias == rootAlias));

    private static GraphEdge Edge(string from, params string[] to) => new(from, to);

    private static void AssertGraph(C4ComponentDiagram diagram, params GraphEdge[] edges)
    {
        var expectedNodes = new HashSet<string>();
        var expectedRelations = new HashSet<(string From, string To)>();

        foreach (var edge in edges)
        {
            expectedNodes.Add(edge.From);
            foreach (var to in edge.To)
            {
                expectedNodes.Add(to);
                expectedRelations.Add((edge.From, to));
            }
        }

        var actualNodes = diagram.Components.Select(component => component.ComponentAlias).ToHashSet();
        var actualRelations = diagram.Relations
            .Select(relation => (relation.FromComponentAlias, relation.ToComponentAlias))
            .ToHashSet();

        CollectionAssert.AreEquivalent(expectedNodes, actualNodes);
        CollectionAssert.AreEquivalent(expectedRelations, actualRelations);
    }

    private sealed record GraphEdge(string From, params string[] To);

    private static void RegisterMsBuild()
    {
        try
        {
            MSBuildLocator.RegisterDefaults();
        }
        catch (InvalidOperationException)
        {
            // MSBuild уже зарегистрирован.
        }
    }

    private static string GetCurrentSolutionPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var solutionPath = Path.Combine(directory.FullName, "C4ModelBuilder.sln");

            if (File.Exists(solutionPath))
            {
                return solutionPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Каталог решения C4ModelBuilder.sln не найден.");
    }
}
