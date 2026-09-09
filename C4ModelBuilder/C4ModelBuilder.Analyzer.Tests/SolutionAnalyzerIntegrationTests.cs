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
            new[] { "HomeController", "GetUsersHandler", "UserService", "UserRepository" },
            componentAliases);

        var relations = diagram.Relations
            .Select(relation => (relation.FromComponentAlias, relation.ToComponentAlias))
            .ToHashSet();

        CollectionAssert.AreEquivalent(
            new[]
            {
                ("HomeController", "GetUsersHandler"),
                ("HomeController", "UserService"),
                ("GetUsersHandler", "UserService"),
                ("UserService", "UserRepository"),
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
        Assert.That(componentAliases, Does.Contain("GetUsersHandler"));
        Assert.That(componentAliases, Does.Contain("UserService"));
        Assert.That(componentAliases, Does.Not.Contain("UserRepository"));
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

