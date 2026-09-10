using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed record ParsedProject(Compilation Compilation, IReadOnlyCollection<ParsedProject.Class> Classes)
{
    public sealed record Class(
        Document Document,
        SyntaxNode SyntaxRootNode,
        SemanticModel SemanticModel,
        ClassDeclarationSyntax ClassDeclarationSyntax);
}
