using C4ModelBuilder.Models.Analysis;

namespace C4ModelBuilder.Analyzer.Tests;

[TestFixture]
public class SolutionAnalyzerIntegrationTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Test]
    public async Task Home_controller_root_class_produces_expected_invocation_tree()
    {
        var analyzer = await SolutionAnalyzer.Create(GetCurrentSolutionPath(), maxDepth: 15, Ct);
        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "HomeController").FirstAsync(Ct);

        var names = NodeNames(tree);
        Assert.That(names, Does.Not.Contain("AdminController"));
        Assert.That(names, Is.SupersetOf(new[] {
            "HomeController", "HomeController.GetUsersAsync", "UserApplication", "UserService",
            "UserRepository", "UserRepository.GetAll", "UserDataSource", "GetUsersHandler",
            "ISmsGateway", "ISmsGateway.SendAsync" }));

        var getUsersAsyncChildren = ChildNames(FindNode(tree, "HomeController.GetUsersAsync"));
        Assert.That(getUsersAsyncChildren, Is.SupersetOf(new[] { "UserApplication", "ISmsGateway", "GetUsersHandler" }));

        AssertDescriptions(
            tree,
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
    public async Task Admin_controller_root_class_produces_expected_invocation_tree()
    {
        var analyzer = await SolutionAnalyzer.Create(GetCurrentSolutionPath(), maxDepth: 15, Ct);
        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "AdminController").FirstAsync(Ct);

        var names = NodeNames(tree);
        Assert.That(names, Does.Not.Contain("HomeController"));
        Assert.That(names, Is.SupersetOf(new[] {
            "AdminController", "AdminController.SendPromoAsync", "UserApplication", "UserService",
            "UserRepository", "UserRepository.GetAll", "UserDataSource", "ISmsGateway", "ISmsGateway.SendAsync" }));

        AssertDescriptions(
            tree,
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

        var home = trees.Single(tree => tree.NodeName == "HomeController");
        var names = NodeNames(home);

        Assert.That(names, Is.SupersetOf(new[] { "HomeController", "UserApplication", "GetUsersHandler", "ISmsGateway" }));
        Assert.That(names.Intersect(["UserService", "UserRepository", "UserDataSource"]), Is.Empty);
    }

    private static IEnumerable<InvocationTree> Flatten(InvocationTree node)
    {
        yield return node;

        foreach (var child in node.Invocations)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private static HashSet<string> NodeNames(InvocationTree tree)
        => Flatten(tree).Select(node => node.NodeName).ToHashSet();

    private static InvocationTree FindNode(InvocationTree tree, string name)
        => Flatten(tree).First(node => node.NodeName == name);

    private static IEnumerable<string> ChildNames(InvocationTree node)
        => node.Invocations.Select(child => child.NodeName);

    private static void AssertDescriptions(InvocationTree tree, params (string Name, string ExpectedDescription)[] expectations)
    {
        var descriptionByName = Flatten(tree)
            .GroupBy(node => node.NodeName)
            .ToDictionary(group => group.Key, group => group.First().C4ComponentDescription);

        foreach (var (name, expectedDescription) in expectations)
        {
            Assert.That(
                descriptionByName.TryGetValue(name, out var actualDescription),
                $"{name} не найден в дереве вызовов.");

            Assert.That(
                actualDescription,
                Is.EqualTo(expectedDescription),
                $"Узел '{name}' должен иметь описание '{expectedDescription}', а получено '{actualDescription}'.");
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
