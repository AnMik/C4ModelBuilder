using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class ParsedProject
{
    public class Doc
    {
        public Document Document { get; }
        public SyntaxTree SyntaxTree { get; }
        public SemanticModel SemanticModel { get; }
        public IReadOnlyCollection<ClassDeclarationSyntax> ClassDeclarationSyntaxes { get; }
        public SyntaxNode SyntaxRootNode { get; }

        public Doc(
            Document document,
            SyntaxTree syntaxTree,
            SyntaxNode syntaxRootNode,
            SemanticModel semanticModel,
            IReadOnlyCollection<ClassDeclarationSyntax> classDeclarationSyntaxes)
        {
            Document = document;
            SyntaxTree = syntaxTree;
            SemanticModel = semanticModel;
            ClassDeclarationSyntaxes = classDeclarationSyntaxes;
            SyntaxRootNode = syntaxRootNode;
        }
    }

    public Project Project { get; }
    public Compilation Compilation { get; }
    public IReadOnlyCollection<Doc> Docs { get; }

    public ParsedProject(Project project, Compilation compilation, IReadOnlyCollection<Doc> docs)
    {
        Project = project;
        Compilation = compilation;
        Docs = docs;
    }
}
