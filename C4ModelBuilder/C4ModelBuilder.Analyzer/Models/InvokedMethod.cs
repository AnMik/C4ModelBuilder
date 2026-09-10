namespace C4ModelBuilder.Analyzer.Models;

internal sealed class InvokedMethod
{
    public ClassMethod? ClassMethod { get; }

    public string ClassName { get; }

    public string MethodName { get; }

    private InvokedMethod(ClassMethod classMethod)
    {
        ClassMethod = classMethod;
        ClassName = classMethod.ClassSyntax.Identifier.Text;
        MethodName = classMethod.MethodSyntax.Identifier.Text;
    }

    private InvokedMethod(string className, string methodName)
    {
        ClassName = className;
        MethodName = methodName;
    }

    public static InvokedMethod From(ClassMethod classMethod) => new(classMethod);

    public static InvokedMethod From(string @class, string method) => new(@class, method);
}
