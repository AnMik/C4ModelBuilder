using System.Runtime.CompilerServices;
using C4ModelBuilder.Analyzer.Models;
using C4ModelBuilder.Models.Analysis;
using C4ModelBuilder.Models.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace C4ModelBuilder.Analyzer;

public sealed class SolutionAnalyzer(string solutionPath, int maxDepth)
{
    public async IAsyncEnumerable<C4ComponentDiagram> AnalyzeComponents([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: ct);

        var parsedSolution = await SolutionParser.Parse(solution, ct);

        var requestHandlersMapping = RdsCqrsRequestsAnalyzer.Analyze(parsedSolution, ct);

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
                var methodNode = await methodAnalyzer.AnalyzeMethod(new ClassMethod(classSyntax, publicMethod), currentDepth: 0, ct);
                if (methodNode != null)
                {
                    classNode.AddChild(methodNode);
                }
            }

            MemberNodeVisualizer.WriteToConsole(classNode);

            yield return C4ComponentDiagramBuilder.Build(classNode, ct);
        }
    }

    private static bool IsRootClass(ParsedSolution.Project.Class @class)
    {
        var symbol = @class.SemanticModel.GetDeclaredSymbol(@class.ClassDeclarationSyntax);
        var attribute = symbol?.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.Name == nameof(C4ComponentAttribute));

        if (attribute is null)
        {
            return false;
        }

        return attribute.NamedArguments.Any(
            argument => argument.Key == nameof(C4ComponentAttribute.IsRoot) && argument.Value.Value is true);
    }
}
