using Microsoft.Build.Locator;

namespace C4ModelBuilder.Analyzer.Tests;

[TestFixture]
public class SolutionAnalyzerIntegrationTests
{
    private static string? _solutionPath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // MSBuildWorkspace (используется внутри SolutionAnalyzer) требует зарегистрированный MSBuild.
        RegisterMsBuild();
        _solutionPath = GetCurrentSolutionPath();
    }

    [Test]
    public async Task AnalyzeComponents_OnRepositorySolution_ReturnsSingleDiagramWithExpectedGraph()
    {
        var diagrams = await SolutionAnalyzer.AnalyzeComponents(_solutionPath!);

        Assert.That(diagrams, Is.Not.Null);
        Assert.That(diagrams.Count, Is.EqualTo(1));

        var diagram = diagrams.Single();

        var componentAliases = diagram.Components
            .Select(component => component.ComponentAlias)
            .ToHashSet();

        CollectionAssert.AreEquivalent(
            new[]
            {
                "HomeController", "UserApplication", "GetUsersHandler", "UserService", "UserRepository", "UserDataSource", "ISmsGateway",
                "HomeController.GetUsersAsync", "UserApplication.GetUsersAsync", "GetUsersHandler.HandleAsync",
                "UserService.GetUsersAsync", "UserRepository.GetAll", "UserDataSource.FetchAll", "ISmsGateway.SendAsync",
            },
            componentAliases);

        var relations = diagram.Relations
            .Select(relation => (relation.FromComponentAlias, relation.ToComponentAlias))
            .ToHashSet();

        CollectionAssert.AreEquivalent(
            new[]
            {
                ("HomeController", "HomeController.GetUsersAsync"),
                ("HomeController.GetUsersAsync", "UserApplication"),
                ("UserApplication", "UserApplication.GetUsersAsync"),
                ("UserApplication.GetUsersAsync", "UserService"),
                ("UserService", "UserService.GetUsersAsync"),
                ("UserService.GetUsersAsync", "UserRepository"),
                ("UserRepository", "UserRepository.GetAll"),
                ("UserRepository.GetAll", "UserDataSource"),
                ("UserDataSource", "UserDataSource.FetchAll"),
                ("HomeController.GetUsersAsync", "ISmsGateway"),
                ("ISmsGateway", "ISmsGateway.SendAsync"),
                ("HomeController.GetUsersAsync", "GetUsersHandler"),
                ("GetUsersHandler", "GetUsersHandler.HandleAsync"),
                ("GetUsersHandler.HandleAsync", "UserApplication"),
            },
            relations);
    }

    [Test]
    public async Task AnalyzeComponents_MaxDepthOne_TruncatesDeepestRelation()
    {
        var diagrams = await SolutionAnalyzer.AnalyzeComponents(_solutionPath!, maxDepth: 1);

        var componentAliases = diagrams.Single()
            .Components
            .Select(component => component.ComponentAlias)
            .ToHashSet();

        Assert.That(componentAliases, Does.Contain("HomeController"));
        Assert.That(componentAliases, Does.Contain("HomeController.GetUsersAsync"));
        Assert.That(componentAliases, Does.Contain("UserApplication"));
        Assert.That(componentAliases, Does.Contain("GetUsersHandler"));
        Assert.That(componentAliases, Does.Contain("ISmsGateway"));

        Assert.That(componentAliases, Does.Not.Contain("UserService"));
        Assert.That(componentAliases, Does.Not.Contain("UserRepository"));
        Assert.That(componentAliases, Does.Not.Contain("UserDataSource"));
        Assert.That(componentAliases, Does.Not.Contain("UserRepository.GetAll"));
        Assert.That(componentAliases, Does.Not.Contain("UserDataSource.FetchAll"));
    }

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

