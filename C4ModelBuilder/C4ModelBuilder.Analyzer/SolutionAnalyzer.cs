using System.Runtime.CompilerServices;
using C4ModelBuilder.Analyzer.Infrastructure;
using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    public static async Task<SolutionAnalyzer> Create(Solution solution, int maxDepth, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solution);

        var parsedSolution = await SolutionParser.Parse(solution, ct);
        var rdsCqrsRequests = RdsCqrsRequestsAnalyzer.Analyze(parsedSolution, ct);
        var methodAnalyzer = new MethodAnalyzer(parsedSolution, rdsCqrsRequests, maxDepth);

        return new SolutionAnalyzer(parsedSolution, methodAnalyzer);
    }

    public async IAsyncEnumerable<InvocationTree> AnalyzeComponents([EnumeratorCancellation] CancellationToken ct = default)
    {
        var rootClasses = _parsedSolution
            .Projects
            .SelectMany(
                x => x.Classes,
                (_, @class) =>
                    (ClassSyntax: @class.ClassDeclarationSyntax,
                     ComponentAttribute: @class.SemanticModel.GetDeclaredSymbol(@class.ClassDeclarationSyntax)?.GetC4ComponentAttribute()))
            .Where(x => x.ComponentAttribute.IsRootC4Component());

        foreach (var (classSyntax, componentAttribute) in rootClasses)
        {
            var rootNode = new InvocationTree(
                nodeName: classSyntax.Identifier.Text,
                c4ComponentDescription: componentAttribute.GetC4ComponentDescription());

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

            yield return rootNode;
        }
    }
}
