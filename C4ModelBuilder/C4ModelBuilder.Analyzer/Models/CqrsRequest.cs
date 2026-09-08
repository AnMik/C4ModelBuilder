using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C4ModelBuilder.Analyzer.Models;

internal sealed record CqrsRequest(string Name, ClassDeclarationSyntax HandlerClass, MethodDeclarationSyntax HandlerMethod);
