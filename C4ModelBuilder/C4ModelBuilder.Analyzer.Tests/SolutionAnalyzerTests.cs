using System.Text;
using C4ModelBuilder.Attributes;
using C4ModelBuilder.Models.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace C4ModelBuilder.Analyzer.Tests;

[TestFixture]
public class SolutionAnalyzerTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Test]
    public async Task Create_parses_solution_and_resolves_cqrs_handlers()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(NullLogger.Instance, CreateSampleSolution(workspace), maxDepth: 15);

        var trees = await analyzer.AnalyzeComponents(Ct).ToListAsync(Ct);

        Assert.That(trees.Select(tree => tree.NodeName),
            Is.EquivalentTo(new[] { "HomeController", "AdminController", "CacheController", "StatsController" }));

        var home = trees.Single(tree => tree.NodeName == "HomeController");
        Assert.That(NodeNames(home), Does.Contain("GetUsersHandler"));
    }

    [Test]
    public async Task AnalyzeComponents_produces_expected_call_tree_for_home_controller()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(NullLogger.Instance, CreateSampleSolution(workspace), maxDepth: 15);
        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "HomeController").FirstAsync(Ct);

        var names = NodeNames(tree);
        Assert.That(names, Does.Not.Contain("AdminController"));
        Assert.That(names, Is.SupersetOf(new[] {
            "HomeController", "HomeController.GetUsersAsync", "UserApplication", "UserService",
            "UserRepository", "UserRepository.GetAll", "UserDataSource", "GetUsersHandler",
            "ISmsGateway", "ISmsGateway.SendAsync" }));

        Assert.That(ChildNames(FindNode(tree, "HomeController.GetUsersAsync")),
            Is.SupersetOf(new[] { "UserApplication", "ISmsGateway", "GetUsersHandler" }));

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
    public async Task AnalyzeComponents_isolates_each_root_class()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(NullLogger.Instance, CreateSampleSolution(workspace), maxDepth: 15);
        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "AdminController").FirstAsync(Ct);

        var names = NodeNames(tree);
        Assert.That(names, Does.Not.Contain("HomeController"));
        Assert.That(names, Is.SupersetOf(new[] {
            "AdminController", "AdminController.SendPromoAsync", "AdminController.SendOneAsync", "UserApplication",
            "UserService", "UserRepository", "UserRepository.GetAll", "UserDataSource", "ISmsGateway",
            "ISmsGateway.SendAsync" }));

        AssertDescriptions(
            tree,
            ("AdminController", "Admin page: send promo campaigns"),
            ("AdminController.SendPromoAsync", "Send promo campaign via SMS"),
            ("AdminController.SendOneAsync", "Send one SMS message"));
    }

    [Test]
    public async Task AnalyzeComponents_respects_depth_limit()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(NullLogger.Instance, CreateSampleSolution(workspace), maxDepth: 1);

        var trees = await analyzer.AnalyzeComponents(Ct).ToListAsync(Ct);

        var home = trees.Single(tree => tree.NodeName == "HomeController");
        var names = NodeNames(home);

        Assert.That(names, Is.SupersetOf(new[] { "HomeController", "UserApplication", "GetUsersHandler", "ISmsGateway" }));
        Assert.That(names.Intersect(["UserService", "UserRepository", "UserDataSource"]), Is.Empty);
    }

    [Test]
    public async Task Create_and_analyzeComponents_produce_a_tree_for_every_root()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(NullLogger.Instance, CreateSampleSolution(workspace), maxDepth: 15);

        var trees = await analyzer.AnalyzeComponents(Ct).ToListAsync(Ct);

        Assert.That(trees.Select(tree => tree.NodeName),
            Is.EquivalentTo(new[] { "HomeController", "AdminController", "CacheController", "StatsController" }));
        Assert.That(trees.All(tree => tree.Invocations.Count > 0), Is.True);
    }

    [Test]
    public async Task AnalyzeComponents_falls_back_to_request_when_cqrs_handler_is_missing()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(NullLogger.Instance, CreateSampleSolution(workspace), maxDepth: 15);

        var stats = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "StatsController").FirstAsync(Ct);

        AssertDescriptions(stats, ("GetStats", "Query without a handler"));
    }

    [Test]
    public async Task AnalyzeComponents_skips_framework_type_fields_without_crashing()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(NullLogger.Instance, CreateSampleSolution(workspace), maxDepth: 15);

        var cache = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "CacheController").FirstAsync(Ct);

        Assert.That(NodeNames(cache), Is.EquivalentTo(new[] { "CacheController", "CacheController.Get" }));
    }

    [Test]
    public async Task Analyze_resolves_cqrs_handler_by_request_type_argument_not_result_type()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSolution(
                workspace,
                ("RdsCqrs.cs", RdsCqrsStubs),
                ("Sample.cs", CqrsHandlerMatchingScenario)),
            maxDepth: 15);

        var trees = await analyzer.AnalyzeComponents(Ct).ToListAsync(Ct);

        var fooTree = trees.Single(tree => tree.NodeName == "FooController");
        Assert.That(NodeNames(fooTree), Does.Contain("FooHandler"));

        var barTree = trees.Single(tree => tree.NodeName == "BarController");
        Assert.That(NodeNames(barTree), Does.Not.Contain("FooHandler"));
        Assert.That(NodeNames(barTree), Does.Contain("GetBar"));
    }

    [Test]
    public async Task Analysis_resolves_method_declared_in_nested_class()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSolution(workspace, ("Sample.cs", NestedWidgetScenario)),
            maxDepth: 15);

        var widgetTree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "WidgetController").FirstAsync(Ct);

        var names = NodeNames(widgetTree);
        Assert.That(names, Does.Contain("Inner"));
        Assert.That(names, Does.Contain("Inner.DoAsync"));
        Assert.That(names, Does.Not.Contain("Outer"));
    }

    [Test]
    public async Task Analysis_resolves_field_interface_to_first_implementation_in_solution_order()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSolution(workspace, ("Sample.cs", InterfaceImplementationScenario)),
            maxDepth: 15);

        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "NotifyController").FirstAsync(Ct);

        var nodes = Flatten(tree)
                    .Select(node => $"{node.NodeName}|{node.C4ComponentDescription ?? "<null>"}")
                    .ToList();

        Assert.That(nodes, Is.EqualTo(new[]
        {
            "NotifyController|Root controller notifying",
            "NotifyController.RunAsync|<null>",
            "FirstNotifier|First notifier implementation",
            "FirstNotifier.NotifyAsync|<null>",
        }));
    }

    [Test]
    public async Task Analysis_resolves_interface_to_implementation_of_derived_interface()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSolution(workspace, ("Sample.cs", DerivedInterfaceScenario)),
            maxDepth: 15);

        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "BaseNotifyController").FirstAsync(Ct);

        var names = NodeNames(tree);

        Assert.That(names, Is.SupersetOf(new[] { "EmailNotifier", "EmailNotifier.NotifyAsync" }));
        Assert.That(names, Does.Not.Contain("INotifier"));
    }

    [Test]
    public async Task Analysis_keeps_interface_node_when_no_implementation_exists()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSolution(workspace, ("Sample.cs", UnresolvedInterfaceScenario)),
            maxDepth: 15);

        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "OrphanNotifyController").FirstAsync(Ct);

        Assert.That(NodeNames(tree), Is.SupersetOf(new[] { "INotifier", "INotifier.NotifyAsync" }));
    }

    [Test]
    public async Task Analysis_matches_field_interface_by_full_name_not_by_short_name()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSolution(workspace, ("Sample.cs", SameNamedInterfacesScenario)),
            maxDepth: 15);

        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "BetaNotifyController").FirstAsync(Ct);

        var names = NodeNames(tree);

        Assert.That(names, Does.Contain("BetaNotifier"));
        Assert.That(names, Does.Not.Contain("AlphaNotifier"));
    }

    [Test]
    public async Task Analysis_marks_a_call_to_a_method_of_the_same_class_as_a_return_to_the_same_class()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSolution(workspace, ("Sample.cs", SameClassCallScenario)),
            maxDepth: 15);

        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "BasketController").FirstAsync(Ct);

        var sameClassCall = FindNode(tree, "BasketController.RecalculateAsync")
                            .Invocations
                            .Single(child => child.NodeName == "BasketController");

        Assert.That(sameClassCall.IsSameClassCall, Is.True);
        Assert.That(sameClassCall.Invocations.Single().NodeName, Is.EqualTo("BasketController.ValidateAsync"));
    }

    [Test]
    public async Task Analysis_does_not_mark_a_return_through_another_component()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSolution(workspace, ("Sample.cs", ReturnThroughAnotherComponentScenario)),
            maxDepth: 15);

        var tree = await analyzer.AnalyzeComponents(Ct).Where(x => x.NodeName == "OrderController").FirstAsync(Ct);

        var returnToOrderController = FindNode(tree, "PaymentService.PayAsync")
                                      .Invocations
                                      .Single(child => child.NodeName == "OrderController");

        Assert.That(returnToOrderController.IsSameClassCall, Is.False);
        Assert.That(
            returnToOrderController.Invocations.Single().NodeName,
            Is.EqualTo("OrderController.CompleteAsync"));
    }

    private static Solution CreateSampleSolution(AdhocWorkspace workspace)
        => CreateSolution(
            workspace,
            ("RdsCqrs.cs", RdsCqrsStubs),
            ("Sample.cs", SampleScenario));

    private static Solution CreateSolution(AdhocWorkspace workspace, params (string FileName, string Source)[] documents)
    {
        var projectId = ProjectId.CreateNewId();

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name: "Sample",
            assemblyName: "Sample",
            language: LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            documents: documents.Select(document => CreateDocumentInfo(projectId, document.FileName, document.Source)),
            metadataReferences: CreateMetadataReferences());

        workspace.AddProject(projectInfo);

        return workspace.CurrentSolution;
    }

    private static DocumentInfo CreateDocumentInfo(ProjectId projectId, string fileName, string source)
        => DocumentInfo.Create(
            DocumentId.CreateNewId(projectId),
            name: fileName,
            sourceCodeKind: SourceCodeKind.Regular,
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(source, Encoding.UTF8), VersionStamp.Create())),
            filePath: fileName);

    private static IReadOnlyList<MetadataReference> CreateMetadataReferences()
    {
        var trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;

        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(C4ComponentAttribute).Assembly.Location));

        return references;
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

    private const string RdsCqrsStubs =
        """
        namespace Rds.Cqrs
        {
            public interface IQuery
            {
            }
        }

        namespace Rds.Cqrs.Queries
        {
            public interface IQueryService
            {
                System.Threading.Tasks.Task Ask(IQuery query, System.Threading.CancellationToken ct = default);
            }

            public interface IQueryHandler<TQuery, TResult>
            {
                System.Threading.Tasks.Task<TResult> HandleAsync(TQuery query, System.Threading.CancellationToken ct = default);
            }
        }
        """;

    private const string SampleScenario =
        """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample
        {
            public sealed class User
            {
            }

            public sealed class GetUsers : Rds.Cqrs.IQuery
            {
            }

            [C4Component("CQRS handler for GetUsers query")]
            public sealed class GetUsersHandler : Rds.Cqrs.Queries.IQueryHandler<GetUsers, User[]>
            {
                private readonly IUserApplication _userApplication;

                public GetUsersHandler(IUserApplication userApplication) => _userApplication = userApplication;

                public Task<User[]> HandleAsync(GetUsers query, CancellationToken ct) => _userApplication.GetUsersAsync(ct);
            }

            public interface IUserApplication
            {
                Task<User[]> GetUsersAsync(CancellationToken ct);
            }

            public interface IUserService
            {
                Task<User[]> GetUsersAsync(CancellationToken ct);
            }

            public interface IUserRepository
            {
                Task<User[]> GetAll(CancellationToken ct);
            }

            public interface IUserDataSource
            {
                Task<User[]> FetchAll(CancellationToken ct);
            }

            [C4Component("External SMS gateway")]
            public interface ISmsGateway
            {
                [C4Component("Send SMS message")]
                Task SendAsync(string message, CancellationToken ct);
            }

            [C4Component("User application use-case layer")]
            public sealed class UserApplication : IUserApplication
            {
                private readonly IUserService _userService;

                public UserApplication(IUserService userService) => _userService = userService;

                public Task<User[]> GetUsersAsync(CancellationToken ct) => _userService.GetUsersAsync(ct);
            }

            [C4Component("User business logic")]
            public sealed class UserService : IUserService
            {
                private readonly IUserRepository _userRepository;

                public UserService(IUserRepository userRepository) => _userRepository = userRepository;

                public Task<User[]> GetUsersAsync(CancellationToken ct) => _userRepository.GetAll(ct);
            }

            [C4Component("User data repository")]
            public sealed class UserRepository : IUserRepository
            {
                private readonly IUserDataSource _userDataSource;

                public UserRepository(IUserDataSource userDataSource) => _userDataSource = userDataSource;

                [C4Component("Fetch all users from data source")]
                public Task<User[]> GetAll(CancellationToken ct) => _userDataSource.FetchAll(ct);
            }

            [C4Component("User data source (DB)")]
            public sealed class UserDataSource : IUserDataSource
            {
                public Task<User[]> FetchAll(CancellationToken ct) => Task.FromResult(Array.Empty<User>());
            }

            [C4Component("Main page: list users and send welcome", isRoot: true)]
            public sealed class HomeController
            {
                private readonly Rds.Cqrs.Queries.IQueryService _queryService;
                private readonly IUserApplication _userApplication;
                private readonly ISmsGateway _smsGateway;

                public HomeController(
                    Rds.Cqrs.Queries.IQueryService queryService,
                    IUserApplication userApplication,
                    ISmsGateway smsGateway)
                {
                    _queryService = queryService;
                    _userApplication = userApplication;
                    _smsGateway = smsGateway;
                }

                [C4Component("Get users list with welcome message")]
                public async Task GetUsersAsync(CancellationToken ct)
                {
                    var users = await _userApplication.GetUsersAsync(ct);
                    await _smsGateway.SendAsync("welcome", ct);
                    await _queryService.Ask(new GetUsers(), ct);

                    GC.KeepAlive(users);
                }
            }

            [C4Component("Admin page: send promo campaigns", isRoot: true)]
            public sealed class AdminController
            {
                private readonly IUserApplication _userApplication;
                private readonly ISmsGateway _smsGateway;

                public AdminController(IUserApplication userApplication, ISmsGateway smsGateway)
                {
                    _userApplication = userApplication;
                    _smsGateway = smsGateway;
                }

                [C4Component("Send promo campaign via SMS")]
                public async Task SendPromoAsync(CancellationToken ct)
                {
                    var users = await _userApplication.GetUsersAsync(ct);
                    await SendOneAsync("promo", ct);

                    GC.KeepAlive(users);
                }

                [C4Component("Send one SMS message")]
                private async Task SendOneAsync(string message, CancellationToken ct)
                {
                    await _smsGateway.SendAsync(message, ct);
                }
            }

            [C4Component("Cache that uses a framework collection", isRoot: true)]
            public sealed class CacheController
            {
                private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _cache = new();

                public string Get(string key) => _cache.GetOrAdd(key, static k => k);
            }

            [C4Component("Query without a handler")]
            public sealed class GetStats : Rds.Cqrs.IQuery
            {
            }

            [C4Component("Controller with unresolved CQRS request", isRoot: true)]
            public sealed class StatsController
            {
                private readonly Rds.Cqrs.Queries.IQueryService _queryService;

                public StatsController(Rds.Cqrs.Queries.IQueryService queryService) => _queryService = queryService;

                public async Task GetStatsAsync(CancellationToken ct) => await _queryService.Ask(new GetStats(), ct);
            }
        }
        """;

    private const string CqrsHandlerMatchingScenario =
        """
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample
        {
            public sealed class GetFoo : Rds.Cqrs.IQuery
            {
            }

            public sealed class GetBar : Rds.Cqrs.IQuery
            {
            }

            [C4Component("Handler for GetFoo")]
            public sealed class FooHandler : Rds.Cqrs.Queries.IQueryHandler<GetFoo, GetBar>
            {
                public Task<GetBar> HandleAsync(GetFoo query, CancellationToken ct) => Task.FromResult(new GetBar());
            }

            [C4Component("Root controller for GetFoo", isRoot: true)]
            public sealed class FooController
            {
                private readonly Rds.Cqrs.Queries.IQueryService _queryService;

                public FooController(Rds.Cqrs.Queries.IQueryService queryService) => _queryService = queryService;

                public async Task RunAsync(CancellationToken ct) => await _queryService.Ask(new GetFoo(), ct);
            }

            [C4Component("Root controller for GetBar", isRoot: true)]
            public sealed class BarController
            {
                private readonly Rds.Cqrs.Queries.IQueryService _queryService;

                public BarController(Rds.Cqrs.Queries.IQueryService queryService) => _queryService = queryService;

                public async Task RunAsync(CancellationToken ct) => await _queryService.Ask(new GetBar(), ct);
            }
        }
        """;

    private const string NestedWidgetScenario =
        """
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample
        {
            public interface IWidget
            {
                Task DoAsync(CancellationToken ct);
            }

            public sealed class Outer
            {
                public sealed class Inner : IWidget
                {
                    public Task DoAsync(CancellationToken ct) => Task.CompletedTask;
                }
            }

            [C4Component("Root controller calling a nested widget", isRoot: true)]
            public sealed class WidgetController
            {
                private readonly IWidget _widget;

                public WidgetController(IWidget widget) => _widget = widget;

                public async Task RunAsync(CancellationToken ct) => await _widget.DoAsync(ct);
            }
        }
        """;

    private const string InterfaceImplementationScenario =
        """
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample
        {
            public interface INotifier
            {
                Task NotifyAsync(CancellationToken ct);
            }

            [C4Component("First notifier implementation")]
            public sealed class FirstNotifier : INotifier
            {
                public Task NotifyAsync(CancellationToken ct) => Task.CompletedTask;
            }

            [C4Component("Second notifier implementation")]
            public sealed class SecondNotifier : INotifier
            {
                public Task NotifyAsync(CancellationToken ct) => Task.CompletedTask;
            }

            [C4Component("Root controller notifying", isRoot: true)]
            public sealed class NotifyController
            {
                private readonly INotifier _notifier;

                public NotifyController(INotifier notifier) => _notifier = notifier;

                public async Task RunAsync(CancellationToken ct) => await _notifier.NotifyAsync(ct);
            }
        }
        """;

    private const string DerivedInterfaceScenario =
        """
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample
        {
            public interface INotifier
            {
                Task NotifyAsync(CancellationToken ct);
            }

            public interface IEmailNotifier : INotifier
            {
            }

            [C4Component("Email notifier")]
            public sealed class EmailNotifier : IEmailNotifier
            {
                public Task NotifyAsync(CancellationToken ct) => Task.CompletedTask;
            }

            [C4Component("Root controller requiring the base notifier", isRoot: true)]
            public sealed class BaseNotifyController
            {
                private readonly INotifier _notifier;

                public BaseNotifyController(INotifier notifier) => _notifier = notifier;

                public async Task RunAsync(CancellationToken ct) => await _notifier.NotifyAsync(ct);
            }
        }
        """;

    private const string UnresolvedInterfaceScenario =
        """
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample
        {
            public interface INotifier
            {
                Task NotifyAsync(CancellationToken ct);
            }

            [C4Component("Root controller without a notifier implementation", isRoot: true)]
            public sealed class OrphanNotifyController
            {
                private readonly INotifier _notifier;

                public OrphanNotifyController(INotifier notifier) => _notifier = notifier;

                public async Task RunAsync(CancellationToken ct) => await _notifier.NotifyAsync(ct);
            }
        }
        """;

    private const string SameNamedInterfacesScenario =
        """
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample.Alpha
        {
            public interface INotifier
            {
                Task NotifyAsync(CancellationToken ct);
            }

            public sealed class AlphaNotifier : INotifier
            {
                public Task NotifyAsync(CancellationToken ct) => Task.CompletedTask;
            }
        }

        namespace Sample.Beta
        {
            public interface INotifier
            {
                Task NotifyAsync(CancellationToken ct);
            }

            public sealed class BetaNotifier : INotifier
            {
                public Task NotifyAsync(CancellationToken ct) => Task.CompletedTask;
            }
        }

        namespace Sample
        {
            [C4Component("Root controller using the beta notifier", isRoot: true)]
            public sealed class BetaNotifyController
            {
                private readonly Sample.Beta.INotifier _notifier;

                public BetaNotifyController(Sample.Beta.INotifier notifier) => _notifier = notifier;

                public async Task RunAsync(CancellationToken ct) => await _notifier.NotifyAsync(ct);
            }
        }
        """;

    private const string SameClassCallScenario =
        """
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample
        {
            [C4Component("Root controller with a call to its own method", isRoot: true)]
            public sealed class BasketController
            {
                [C4Component("Recalculate the basket")]
                public async Task RecalculateAsync(CancellationToken ct) => await ValidateAsync(ct);

                [C4Component("Validate the basket")]
                private Task ValidateAsync(CancellationToken ct) => Task.CompletedTask;
            }
        }
        """;

    private const string ReturnThroughAnotherComponentScenario =
        """
        using System.Threading;
        using System.Threading.Tasks;
        using C4ModelBuilder.Attributes;

        namespace Sample
        {
            [C4Component("Orders page", isRoot: true)]
            public sealed class OrderController
            {
                private readonly PaymentService _paymentService;

                public OrderController(PaymentService paymentService) => _paymentService = paymentService;

                [C4Component("Pay for the order")]
                public async Task PayAsync(CancellationToken ct) => await _paymentService.PayAsync(ct);

                [C4Component("Complete the order")]
                public Task CompleteAsync(CancellationToken ct) => Task.CompletedTask;
            }

            [C4Component("Payment service")]
            public sealed class PaymentService
            {
                private readonly OrderController _orderController;

                public PaymentService(OrderController orderController) => _orderController = orderController;

                [C4Component("Process the payment")]
                public async Task PayAsync(CancellationToken ct) => await _orderController.CompleteAsync(ct);
            }
        }
        """;

    [Test]
    public async Task Create_filters_projects_when_targetProject_is_specified()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSampleSolution(workspace),
            maxDepth: 15,
            targetProject: "NonExistentProject",
            excludeMask: null,
            Ct);

        var trees = await analyzer.AnalyzeComponents(Ct).ToListAsync(Ct);
        Assert.That(trees, Is.Empty);
    }

    [Test]
    public async Task Create_excludes_projects_matching_excludeMask()
    {
        using var workspace = new AdhocWorkspace();

        var analyzer = await SolutionAnalyzer.Create(
            NullLogger.Instance,
            CreateSampleSolution(workspace),
            maxDepth: 15,
            targetProject: null,
            excludeMask: "*",
            Ct);

        var trees = await analyzer.AnalyzeComponents(Ct).ToListAsync(Ct);
        Assert.That(trees, Is.Empty);
    }
}
