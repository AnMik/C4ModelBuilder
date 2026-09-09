using System.Runtime.CompilerServices;
using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;
using C4ModelBuilder.Models.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace C4ModelBuilder.Analyzer;

public static class SolutionAnalyzer
{
    public static async IAsyncEnumerable<C4ComponentDiagram> AnalyzeComponents(
        string solutionPath,
        int maxDepth,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: ct);

        var parsedSolution = await SolutionParser.Parse(solution, ct);

        var requestHandlersMapping = CqrsRequestsAnalyzer
            .Analyze(parsedSolution, ct)
            .GroupBy(
                cqrsRequest => cqrsRequest.Name,
                (name, cqrsRequests) =>
                {
                    var firstHandler = cqrsRequests.First();
                    return (RequestName: name,
                            RequestHandlerClass: firstHandler.HandlerClass,
                            RequestHandlerMethod: firstHandler.HandlerMethod);
                })
            .ToDictionary(x => x.RequestName, x => (x.RequestHandlerClass, x.RequestHandlerMethod));

        var methodAnalyzer = new MethodAnalyzer(parsedSolution, requestHandlersMapping, maxDepth);

        var rootClasses = parsedSolution.Projects.SelectMany(x => x.Classes).Where(IsRootClass);

        foreach (var rootClass in rootClasses)
        {
            ct.ThrowIfCancellationRequested();

            var classSyntax = rootClass.ClassDeclarationSyntax;
            var classNode = new MemberNode(classSyntax.Identifier.Text);

            var publicMethods = classSyntax
                .Members
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword));

            foreach (var publicMethod in publicMethods)
            {
                var methodNode = await methodAnalyzer.AnalyzeMethod(classSyntax, publicMethod, currentDepth: 0, ct);
                if (methodNode != null)
                {
                    classNode.AddChild(methodNode);
                }
            }

            MemberNodeVisualizer.WriteToConsole(classNode);

            yield return C4ComponentDiagramBuilder.Build(classNode, ct);
        }
    }

    private static bool IsRootClass(ParsedProject.Class @class)
    {
        var symbol = @class.SemanticModel.GetDeclaredSymbol(@class.ClassDeclarationSyntax);
        var attribute = symbol?.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.Name == nameof(C4ComponentAttribute));

        if (attribute == null)
        {
            return false;
        }

        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Value is true)
            {
                return true;
            }
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Key is "isRoot" or "IsRoot" && namedArgument.Value.Value is true)
            {
                return true;
            }
        }

        return false;
    }
}
