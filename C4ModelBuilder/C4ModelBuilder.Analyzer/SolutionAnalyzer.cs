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
    public static async Task<IReadOnlyCollection<C4ComponentDiagram>> AnalyzeComponents(
        string solutionPath,
        int maxDepth = 15,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: ct);

        var parsedSolution = await SolutionParser.Parse(solution, ct);

        var requestHandlersMapping = CqrsRequestsAnalyzer
            .Analyze(parsedSolution, ct)
            .GroupBy(
                x => x.Name,
                (x, items) => (x, RequestHandlerClass: items.First().HandlerClass, RequestHandlerMethod: items.First().HandlerMethod))
            .ToDictionary(x => x.x, x => (x.Item2, x.Item3));

        var methodAnalyzer = new MethodAnalyzer(parsedSolution, requestHandlersMapping, maxDepth);

        var rootClasses = parsedSolution
            .Projects
            .SelectMany(x => x.Classes)
            .Where(IsRootClass)
            .ToList();

        var diagrams = new List<C4ComponentDiagram>();

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

            var treeRoot = new MemberNode("Root");
            treeRoot.AddChild(classNode);

            MemberNodeVisualizer.WriteToConsole(treeRoot);
            diagrams.Add(C4ComponentDiagramBuilder.Build(treeRoot, ct));
        }

        return diagrams;
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
