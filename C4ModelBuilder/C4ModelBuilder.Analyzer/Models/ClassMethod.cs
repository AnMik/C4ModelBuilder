using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed record ClassMethod(ClassDeclarationSyntax ClassSyntax, MethodDeclarationSyntax MethodSyntax)
{
    public string Name => $"{ClassSyntax.Identifier.Text}.{MethodSyntax.Identifier.Text}";
};
