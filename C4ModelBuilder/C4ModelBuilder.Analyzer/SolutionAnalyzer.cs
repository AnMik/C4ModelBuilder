using System.Runtime.CompilerServices;
using C4ModelBuilder.Analyzer.Infrastructure;
using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace C4ModelBuilder.Analyzer;

public sealed class SolutionAnalyzer
{
    private readonly ParsedSolution _parsedSolution;
    private readonly MethodAnalyzer _methodAnalyzer;

    private SolutionAnalyzer(ParsedSolution parsedSolution, MethodAnalyzer methodAnalyzer)
    {
        _parsedSolution = parsedSolution;
        _methodAnalyzer = methodAnalyzer;
    }

    public static async Task<SolutionAnalyzer> Create(string solutionPath, int maxDepth, CancellationToken ct = default)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: ct);

        var parsedSolution = await SolutionParser.Parse(solution, ct);
        var rdsCqrsRequests = RdsCqrsRequestsAnalyzer.Analyze(parsedSolution, ct);
        var methodAnalyzer = new MethodAnalyzer(parsedSolution, rdsCqrsRequests, maxDepth);

        return new SolutionAnalyzer(parsedSolution, methodAnalyzer);
    }

    public async IAsyncEnumerable<C4ComponentDiagram> AnalyzeComponents([EnumeratorCancellation] CancellationToken ct = default)
    {
        var rootComponentClasses = _parsedSolution
            .Projects
            .SelectMany(x => x.Classes)
            .Where(IsRootComponent);

        foreach (var @class in rootComponentClasses)
        {
            var classSyntax = @class.ClassDeclarationSyntax;
            var rootNode = new InvocationTree(classSyntax.Identifier.Text, isC4Component: true, description: GetClassDescription(@class));

            var publicMethods = classSyntax
                .Members
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword));

            foreach (var publicMethod in publicMethods)
            {
                var invokedMethodNode =
                    await _methodAnalyzer.AnalyzeMethod(new ClassMethod(classSyntax, publicMethod), currentDepth: 0, ct);

                if (invokedMethodNode != null)
                {
                    rootNode.AddInvocation(invokedMethodNode);
                }
            }

            MemberNodeVisualizer.WriteToConsole(rootNode);

            yield return C4ComponentDiagramBuilder.Build(rootNode);
        }
    }

    private static bool IsRootComponent(ParsedSolution.Project.Class @class)
        => @class
            .SemanticModel
            .GetDeclaredSymbol(@class.ClassDeclarationSyntax)
            .IsRootComponent();

    private static string? GetClassDescription(ParsedSolution.Project.Class @class)
        => @class
            .SemanticModel
            .GetDeclaredSymbol(@class.ClassDeclarationSyntax)
            .GetC4ComponentDescription();
}
