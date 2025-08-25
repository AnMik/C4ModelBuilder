#pragma warning disable CA1502
#pragma warning disable CA1505
#pragma warning disable CA1506
using Afisha.Tickets.Core.C4;
using Afisha.Tickets.Core.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using С4ModelBuilder;

using var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync("C:\\Repos\\kassa\\Afisha.Tickets.All.sln");

var parsedProjects = await SolutionParser.Parse(solution);

var requestHandlersMapping = CqrsAnalyzer.GetRequestHandlersMapping(parsedProjects);

foreach (var parsedProject in parsedProjects)
{
    foreach (var doc in parsedProject.Docs)
    {
        // var containerAttributeName = nameof(C4ContainerAttribute)[..^9];
        // var classesWithAttribute = mapiRoot
        //     .DescendantNodes()
        //     .OfType<ClassDeclarationSyntax>()
        //     .Where(
        //         @class => @class
        //             .AttributeLists
        //             .SelectMany(attributeList => attributeList.Attributes)
        //             .Any(attribute => attribute.Name.ToString() == containerAttributeName));
        //
        // foreach (var classDecl in classesWithAttribute)
        // {
        //     Console.WriteLine($"Class with C4ContextAttribute: {classDecl.Identifier.Text}");
        // }

        var componentAttributeName = nameof(C4ComponentAttribute)[..^(nameof(Attribute).Length)];

        var mapiMethodsWithAttribute = doc
            .SyntaxRootNode
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(
                method => method
                    .AttributeLists
                    .SelectMany(attributeList => attributeList.Attributes)
                    .Any(attribute => attribute.Name.ToString() == componentAttributeName));

        foreach (var method in mapiMethodsWithAttribute)
        {
            await AnalyzeMethod(parsedProjects, requestHandlersMapping, doc.SemanticModel, method, depth: 0);
        }
    }
}

Console.WriteLine("Finished");
return;

static async Task AnalyzeMethod(
    IReadOnlyCollection<ParsedProject> projects,
    Dictionary<string, INamedTypeSymbol> requestHandlersMapping,
    SemanticModel semanticModel,
    MethodDeclarationSyntax method,
    int depth)
{
    if (depth > 15)
    {
        Console.WriteLine($"{GetTabs(depth + 1)}\u2514\u2500\u2500<Limit>");
        return;
    }

    var methodSymbol = semanticModel.GetDeclaredSymbol(method);
    if (methodSymbol == null)
    {
        throw new Exception("");
        return;
    }

    Console.WriteLine($"{GetTabs(depth)}\u2514\u2500\u2500{methodSymbol.ContainingType.ToDisplayString()}.{methodSymbol.Name}() -> {GetMethodSignatureKey(method, semanticModel)}");

    var methodsWithInterfaces = GetInterfacesUsedInMethod(requestHandlersMapping, method, semanticModel).ToArray();

    if (methodsWithInterfaces.IsNullOrEmpty())
    {
        Console.WriteLine($"{GetTabs(depth + 1)}\u2514\u2500\u2500<Empty>");
        return;
    }

    foreach (var (methodName, methodContainer, methodSignature) in methodsWithInterfaces)
    {
        if (methodContainer.TypeKind == TypeKind.Interface)
        {
            var implementingClasses = FindImplementingClasses(projects, methodContainer).ToArray(); // todo: use classsyntax instead string

            if (implementingClasses.IsNullOrEmpty())
            {
                Console.WriteLine($"{GetTabs(depth + 1)}\u2514\u2500\u2500{methodContainer}.{methodName}() -> (no implementing classes found)");
                continue;
            }

            foreach (var implementingClass in implementingClasses)
            {
                var calledMethod = FindMethodInClass(projects, implementingClass, methodSignature);

                if (calledMethod == null)
                {
                    throw new InvalidOperationException($"Не найден метод {methodName} в классе {implementingClass}.");
                }

                var calledMethodSemanticModel = GetSemanticModelForMethod(projects, calledMethod);

                if (calledMethodSemanticModel == null)
                {
                    throw new Exception("");
                }

                await AnalyzeMethod(projects, requestHandlersMapping, calledMethodSemanticModel, calledMethod, depth + 1);
            }
        }
        else if (methodContainer.TypeKind == TypeKind.Class)
        {
            var calledMethod = FindMethodInClass(projects, methodContainer.ToDisplayString(), methodSignature);

            if (calledMethod == null)
            {
                throw new InvalidOperationException($"Не найден метод {methodName}{methodSignature} в классе {methodContainer.ToDisplayString()}.");
            }

            var calledMethodSemanticModel = GetSemanticModelForMethod(projects, calledMethod);

            if (calledMethodSemanticModel == null)
            {
                throw new Exception("");
            }

            await AnalyzeMethod(projects, requestHandlersMapping, calledMethodSemanticModel, calledMethod, depth + 1);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(methodContainer.TypeKind), methodContainer?.TypeKind, "Необрабатываемый тип контейнера метода.");
        }
    }

    return;

    static string GetTabs(int count) => Enumerable.Repeat("\t", count).JoinStrings(string.Empty);
}

static IEnumerable<(string, INamedTypeSymbol, string)> GetInterfacesUsedInMethod(
    Dictionary<string, INamedTypeSymbol> requestHandlersMapping,
    MethodDeclarationSyntax method,
    SemanticModel semanticModel)
{
    var parentClass = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

    if (parentClass == null)
    {
        yield break;
    }

    var parentClassFields = parentClass
        .Members
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(f => f.Declaration.Variables)
        .Select(
            x => (FieldName: x.Identifier.Text, FieldType: (semanticModel.GetDeclaredSymbol(x) as IFieldSymbol)?.Type as INamedTypeSymbol))
        .Where(
            tuple => tuple.FieldType is { TypeKind: TypeKind.Interface or TypeKind.Class }
                && !tuple.FieldType.Locations.Any(x => x.IsInMetadata)
                && !tuple.FieldType.ToDisplayString().StartsWith("Afisha.Tickets.Caching"))
        .ToDictionary(x => x.FieldName, x => x.FieldType);

    var parentClassMethods = parentClass
        .Members
        .OfType<MethodDeclarationSyntax>()
        .Select(x => x.Identifier.Text)
        .ToHashSet();

    foreach (var invocationExpressionSyntax in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
        var invocationChildNodes = invocationExpressionSyntax.ChildNodes().ToList();

        var identifierNameSyntax = invocationChildNodes
            .OfType<MemberAccessExpressionSyntax>()
            .SelectMany(x => x.ChildNodes())
            .OfType<IdentifierNameSyntax>()
            .FirstOrDefault();

        var fieldTypeFullName = identifierNameSyntax != null
            ? semanticModel.GetTypeInfo(identifierNameSyntax).Type?.ToDisplayString()
            : null;

        var isCqrsRequest = fieldTypeFullName is "Rds.Cqrs.Queries.IQueryService" or "Rds.Cqrs.Commands.ICommandProcessor";

        if (isCqrsRequest)
        {
            var queryName = invocationChildNodes
                .OfType<ArgumentListSyntax>()
                .SelectMany(x => x.ChildNodes())
                .OfType<ArgumentSyntax>()
                .SelectMany(x => x.ChildNodes())
                .OfType<ObjectCreationExpressionSyntax>()
                .SelectMany(x => x.ChildNodes())
                .OfType<IdentifierNameSyntax>()
                .FirstOrDefault()
                ?.Identifier
                .Text;

            if (queryName != null && requestHandlersMapping.TryGetValue(queryName, out var interfaceName))
            {
                yield return ("HandleAsync", interfaceName, $"HandleAsync({queryName},CancellationToken)");

                continue;
            }
        }

        var memberAccessExpressionSyntax = invocationChildNodes.OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

        if (memberAccessExpressionSyntax != null)
        {
            var fieldName = (memberAccessExpressionSyntax.Expression as IdentifierNameSyntax)?.Identifier.Text;

            if (fieldName != null && parentClassFields.TryGetValue(fieldName, out var fieldTypeSymbol) && fieldTypeSymbol != null)
            {
                yield return (memberAccessExpressionSyntax.Name.Identifier.Text, fieldTypeSymbol, GetInvocationSignatureKey(invocationExpressionSyntax, semanticModel));

                continue;
            }
        }

        var identifierNameSyntax2 = invocationChildNodes.OfType<IdentifierNameSyntax>().FirstOrDefault();

        if (identifierNameSyntax2 != null)
        {
            var symbol = semanticModel.GetDeclaredSymbol(parentClass);

            if (symbol != null && parentClassMethods.Contains(identifierNameSyntax2.Identifier.Text))
            {
                yield return (identifierNameSyntax2.Identifier.Text, symbol, GetInvocationSignatureKey(invocationExpressionSyntax, semanticModel)); // todo: method overloads
            }
        }
    }
}

static IEnumerable<string> FindImplementingClasses(IReadOnlyCollection<ParsedProject> projects, INamedTypeSymbol interfaceNamedTypeSymbol)
{
    var implementingClasses = new HashSet<string>();

    foreach (var (semanticModel, classDeclarationSyntaxes) in projects.SelectMany(x => x.Docs)
                 .Select(x => (x.SemanticModel, x.ClassDeclarationSyntaxes)))
    {
        foreach (var classDecl in classDeclarationSyntaxes)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

            if (classSymbol != null
                && classSymbol.AllInterfaces.Any(x => x.ToDisplayString() == interfaceNamedTypeSymbol.ToDisplayString()))
            {
                implementingClasses.Add(classSymbol.ToDisplayString());
            }
        }
    }

    return implementingClasses;
}

static MethodDeclarationSyntax FindMethodInClass(IReadOnlyCollection<ParsedProject> projects, string classFullName, string methodSignature)
{
    var doc = projects // todo: substrings intersection
            .OrderByDescending(x => x.Project.Name.Split('.').IntersectionFromStart(classFullName.Split('.')).Count())
            .SelectMany(x => x.Docs)
            .SelectMany(x => x.ClassDeclarationSyntaxes, (doc, syntax) => (Doc: doc, ClassDeclarationSyntax: syntax))
            .Where(x => x.Doc.SemanticModel.GetDeclaredSymbol(x.ClassDeclarationSyntax)?.ToDisplayString() == classFullName)
            .Select(x => x.Doc)
            .FirstOrDefault()
        ?? throw new InvalidOperationException($"Не найден класс {classFullName} в проектах.");

    return doc
            .SyntaxRootNode
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(x => GetMethodSignatureKey(x, doc.SemanticModel) == methodSignature)
        ?? throw new InvalidOperationException($"Не найден метод {methodSignature} в классе {classFullName}.");
}

static SemanticModel? GetSemanticModelForMethod(IReadOnlyCollection<ParsedProject> projects, MethodDeclarationSyntax method)
{
    // Получаем синтаксическое дерево метода
    var syntaxTree = method.SyntaxTree;

    // Проходим по всем проектам в решении
    foreach (var project in projects)
    {
        // Проверяем, принадлежит ли синтаксическое дерево текущему проекту
        if (project.Compilation.SyntaxTrees.Contains(syntaxTree))
        {
            return project.Compilation.GetSemanticModel(syntaxTree);
        }
    }

    return null;
}

static string GetMethodSignatureKey(MethodDeclarationSyntax method, SemanticModel semanticModel)
{
    var parameterTypes = method.ParameterList.Parameters.Select(x => GetTypeName(x.Type, semanticModel));

    return $"{method.Identifier.Text}({string.Join(",", parameterTypes)})";
}

static string GetInvocationSignatureKey(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
{
    var methodName = GetMethodName(invocation);
    var argumentTypes = invocation.ArgumentList.Arguments.Select(x => GetExpressionType(x.Expression, semanticModel));

    return $"{methodName}({string.Join(",", argumentTypes)})";
}

static string GetMethodName(InvocationExpressionSyntax invocation)
{
    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
    {
        return memberAccess.Name.Identifier.Text;
    }

    if (invocation.Expression is IdentifierNameSyntax identifier)
    {
        return identifier.Identifier.Text;
    }

    return "UnknownMethod";
}

static string GetTypeName(TypeSyntax typeSyntax, SemanticModel semanticModel)
{
    var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
    return typeInfo.Type?.ToDisplayString() ?? typeSyntax.ToString();
}

static string GetExpressionType(ExpressionSyntax expression, SemanticModel semanticModel)
{
    var typeInfo = semanticModel.GetTypeInfo(expression);
    return typeInfo.Type?.ToDisplayString() ?? "unknown";
}

public static class CqrsAnalyzer
{
    public static Dictionary<string, INamedTypeSymbol> GetRequestHandlersMapping(IReadOnlyCollection<ParsedProject> projects)
    {
        var handlerMap = new Dictionary<string, INamedTypeSymbol>();

        foreach (var requestClass in projects.SelectMany(x => x.Docs).SelectMany(x => GetQueryClassSyntaxes(x.ClassDeclarationSyntaxes)))
        {
            foreach (var (semanticModel, classDeclarationSyntaxes) in projects.SelectMany(x => x.Docs).Select(x => (x.SemanticModel, x.ClassDeclarationSyntaxes)))
            {
                var queryName = requestClass.Identifier.Text;

                var handlerClass = classDeclarationSyntaxes.FirstOrDefault(
                    classSyntax => classSyntax.BaseList?.Types.Any(x => DoesImplementQueryHandler(queryName, x.Type, semanticModel))
                        == true);

                if (handlerClass != null)
                {
                    var handlerSymbol = semanticModel.GetDeclaredSymbol(handlerClass);

                    if (handlerSymbol != null)
                    {
                        handlerMap[queryName] = handlerSymbol;
                        break;
                    }
                }
            }
        }

        return handlerMap;
    }

    private static IEnumerable<ClassDeclarationSyntax> GetQueryClassSyntaxes(IReadOnlyCollection<ClassDeclarationSyntax> classSyntaxes)
    {
        var queryClassSyntaxes = classSyntaxes
            .Where(@class => IsQuery(@class))
            .Where(
                @class => @class.Modifiers.All(
                    syntaxToken => !syntaxToken.IsKind(SyntaxKind.StructKeyword) && !syntaxToken.IsKind(SyntaxKind.PrivateKeyword)));

        foreach (var queryClassSyntax in queryClassSyntaxes)
        {
            yield return queryClassSyntax;
        }
    }

    private static bool IsQuery(ClassDeclarationSyntax classDeclaration)
        => classDeclaration.BaseList?.Types.Select(x => (x.Type as SimpleNameSyntax)?.Identifier.Text).Any(x => x is "IQuery" or "ICommand")
            == true;

    private static bool DoesImplementQueryHandler(string requestName, TypeSyntax typeSyntax, SemanticModel semanticModel)
    {
        if (semanticModel.GetTypeInfo(typeSyntax).Type is not INamedTypeSymbol typeSymbol)
        {
            return false;
        }

        return typeSymbol.IsGenericType
            && (typeSymbol.OriginalDefinition.ToString()!.Contains("Rds.Cqrs.Queries.IQueryHandler")
                || typeSymbol.OriginalDefinition.ToString()!.Contains("Rds.Cqrs.Commands.ICommandHandler"))
            && typeSymbol.TypeArguments.Any(argument => argument.ToDisplayString().Contains(requestName));
    }
}

#pragma warning restore CA1502
#pragma warning restore CA1505
#pragma warning restore CA1506
