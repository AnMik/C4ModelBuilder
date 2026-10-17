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

    public static async Task<SolutionAnalyzer> Create(
        ILogger logger,
        Solution solution,
        int maxDepth,
        string? targetProject = null,
        string? excludeMask = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(solution);

        var solutionParser = new SolutionParser(logger, targetProject, excludeMask);
        var rdsCqrsAnalyzer = new RdsCqrsRequestsAnalyzer(logger);

        var parsedSolution = await solutionParser.Parse(solution, ct);
        var rdsCqrsRequests = rdsCqrsAnalyzer.Analyze(parsedSolution, ct);

        var methodAnalyzer = new MethodAnalyzer(logger, parsedSolution, rdsCqrsRequests, maxDepth);

        return new SolutionAnalyzer(parsedSolution, methodAnalyzer);
    }

    public async IAsyncEnumerable<InvocationTree> AnalyzeComponents([EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var (classSyntax, componentAttribute) in FindRootClasses())
        {
            yield return await BuildClassRootTree(classSyntax, componentAttribute, ct);
        }

        foreach (var rootMethod in FindRootMethods())
        {
            var rootClassSymbol = _parsedSolution.GetDeclaredSymbols(rootMethod).ClassSymbol;
            var methodRootTree = await _methodAnalyzer.AnalyzeMethod(
                rootMethod,
                currentDepth: 0,
                excludedClassSymbol: rootClassSymbol,
                ct: ct);

            if (methodRootTree != null)
            {
                yield return methodRootTree;
            }
        }
    }

    private IEnumerable<(ClassDeclarationSyntax ClassSyntax, AttributeData? ComponentAttribute)> FindRootClasses()
        => _parsedSolution
           .AllClasses
           .Select(
               @class => (ClassSyntax: @class.ClassDeclarationSyntax,
                          ComponentAttribute: @class
                                              .SemanticModel
                                              .GetDeclaredSymbol(@class.ClassDeclarationSyntax)
                                              ?.GetC4ComponentAttribute()))
           .Where(x => x.ComponentAttribute.IsRootC4Component());

    private IEnumerable<ClassMethod> FindRootMethods()
        => _parsedSolution
           .AllClasses
           .SelectMany(
               @class => @class
                         .ClassDeclarationSyntax
                         .Members
                         .OfType<MethodDeclarationSyntax>()
                         .Select(
                             method => (ClassMethod: new ClassMethod(@class.ClassDeclarationSyntax, method),
                                        ComponentAttribute: @class
                                                            .SemanticModel
                                                            .GetDeclaredSymbol(method)
                                                            ?.GetC4ComponentAttribute()))
                         .Where(x => x.ComponentAttribute.IsRootC4Component())
                         .Select(x => x.ClassMethod));

    private async Task<InvocationTree> BuildClassRootTree(
        ClassDeclarationSyntax classSyntax,
        AttributeData? componentAttribute,
        CancellationToken ct)
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
                await _methodAnalyzer.AnalyzeMethod(new ClassMethod(classSyntax, publicMethod), currentDepth: 0, ct: ct);

            if (invokedMethodNode != null)
            {
                rootNode.AddInvocation(invokedMethodNode);
            }
        }

        return rootNode;
    }
}
