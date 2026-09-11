using C4ModelBuilder.Models.Analysis;
using C4ModelBuilder.PlantUmlCreator;

namespace C4ModelBuilder.Analyzer.Tests;

[TestFixture]
public class SolutionAnalyzerIntegrationTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Test]
    public async Task Home_controller_root_class_produces_expected_component_diagram()
    {
        var analyzer = await SolutionAnalyzer.Create(GetCurrentSolutionPath(), maxDepth: 15, Ct);
        var homeControllerTree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "HomeController").FirstAsync(Ct);
        var diagram = PlantUmlGenerator.BuildComponentDiagram(homeControllerTree);

        Assert.That(diagram.Components.Select(component => component.ComponentAlias), Does.Not.Contain("AdminController"));

        AssertGraph(
            diagram,
            [
                new GraphEdge("HomeController", "HomeController.GetUsersAsync"),
                new GraphEdge("HomeController.GetUsersAsync", "UserApplication", "ISmsGateway", "GetUsersHandler"),
                new GraphEdge("UserApplication", "UserService"),
                new GraphEdge("UserService", "UserRepository"),
                new GraphEdge("UserRepository", "UserRepository.GetAll"),
                new GraphEdge("UserRepository.GetAll", "UserDataSource"),
                new GraphEdge("GetUsersHandler", "UserApplication"),
                new GraphEdge("ISmsGateway", "ISmsGateway.SendAsync")
            ]);

        AssertDescriptions(
            diagram,
            ("HomeController", "Main page: list users and send welcome"),
            ("HomeController.GetUsersAsync", "Get users list with welcome message"),
            ("UserApplication", "User application use-case layer"),
            ("UserService", "User business logic"),
            ("UserRepository", "User data repository"),
            ("UserRepository.GetAll", "Fetch all users from data source"),
            ("UserDataSource", "User data source (DB)"),
            ("GetUsersHandler", "CQRS handler for GetUsers query"),
            ("ISmsGateway", "External SMS gateway"),
            ("ISmsGateway.SendAsync", "Send SMS message"));
    }

    [Test]
    public async Task Admin_controller_root_class_produces_expected_component_diagram()
    {
        var analyzer = await SolutionAnalyzer.Create(GetCurrentSolutionPath(), maxDepth: 15, Ct);
        var adminControllerTree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "AdminController").FirstAsync(Ct);
        var diagram = PlantUmlGenerator.BuildComponentDiagram(adminControllerTree);

        Assert.That(diagram.Components.Select(component => component.ComponentAlias), Does.Not.Contain("HomeController"));

        AssertGraph(
            diagram,
            [
                new GraphEdge("AdminController", "AdminController.SendPromoAsync"),
                new GraphEdge("AdminController.SendPromoAsync", "UserApplication", "ISmsGateway"),
                new GraphEdge("UserApplication", "UserService"),
                new GraphEdge("UserService", "UserRepository"),
                new GraphEdge("UserRepository", "UserRepository.GetAll"),
                new GraphEdge("UserRepository.GetAll", "UserDataSource"),
                new GraphEdge("ISmsGateway", "ISmsGateway.SendAsync")
            ]);

        AssertDescriptions(
            diagram,
            ("AdminController", "Admin page: send promo campaigns"),
            ("AdminController.SendPromoAsync", "Send promo campaign via SMS"),
            ("UserApplication", "User application use-case layer"),
            ("UserService", "User business logic"),
            ("UserRepository", "User data repository"),
            ("UserRepository.GetAll", "Fetch all users from data source"),
            ("UserDataSource", "User data source (DB)"),
            ("ISmsGateway", "External SMS gateway"),
            ("ISmsGateway.SendAsync", "Send SMS message"));
    }

    [Test]
    public async Task Analysis_depth_limits_how_deep_a_call_tree_is_expanded()
    {
        var analyzer = await SolutionAnalyzer.Create(GetCurrentSolutionPath(), maxDepth: 1, Ct);

        var trees = await analyzer.AnalyzeComponents(Ct).ToListAsync(Ct);

        var shallowDiagrams = trees.Select(PlantUmlGenerator.BuildComponentDiagram).ToList();

        var home = shallowDiagrams.Single(diagram => diagram.Components.Any(component => component.ComponentAlias == "HomeController"));
        var componentAliases = home.Components.Select(component => component.ComponentAlias).ToHashSet();

        Assert.That(componentAliases, Is.SupersetOf(new[] { "HomeController", "UserApplication", "GetUsersHandler", "ISmsGateway", }));
        Assert.That(componentAliases.Intersect(["UserService", "UserRepository", "UserDataSource",]), Is.Empty);
    }


    private static void AssertGraph(C4ComponentDiagram diagram, GraphEdge[] edges)
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

    private static void AssertDescriptions(C4ComponentDiagram diagram, params (string Alias, string ExpectedDescription)[] expectations)
    {
        var descriptionByAlias = diagram.Components.ToDictionary(c => c.ComponentAlias, c => c.Description);

        foreach (var (alias, expectedDescription) in expectations)
        {
            Assert.That(
                descriptionByAlias.TryGetValue(alias, out var actualDescription),
                $"{alias} не найден среди компонентов диаграммы.");

            Assert.That(
                actualDescription,
                Is.EqualTo(expectedDescription),
                $"Компонент '{alias}' должен иметь описание '{expectedDescription}', а получено '{actualDescription}'.");
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

    private sealed record GraphEdge(string From, params string[] To);
}
