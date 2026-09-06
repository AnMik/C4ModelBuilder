using C4ModelBuilder.Models;

namespace C4ModelBuilder.PlantUmlCreator.Tests;

public class PlantUmlGeneratorTests
{
    [Test]
    public void Generate_WithEmptyTree_ShouldReturnOnlyHeaderAndFooter()
    {
        var root = new MemberNode("Root");

        var result = PlantUmlGenerator.Generate(root);

        Assert.That(result, Does.Contain("@startuml"));
        Assert.That(result, Does.Contain("@enduml"));
        Assert.That(result, Does.Not.Contain("Component("));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Generate_WithNullRoot_ShouldThrowException()
    {
        Assert.Throws<ArgumentNullException>(() => PlantUmlGenerator.Generate(null!));
    }

    [Test]
    public void Generate_WithSingleNodeNoChildren_ShouldProduceOneComponentNoRelations()
    {
        var root = new MemberNode("Root");
        root.AddChild(new MemberNode("MyApp.MyClass.DoSomething()"));

        var result = PlantUmlGenerator.Generate(root);

        Assert.That(result, Does.Contain("Component("));
        Assert.That(result, Does.Contain("\"MyApp\""));
        Assert.That(result, Does.Not.Contain("Rel("));
    }

    [Test]
    public void Generate_WithTwoNodesCallingEachOther_ShouldProduceTwoComponentsOneRelation()
    {
        var root = new MemberNode("Root");
        var caller = new MemberNode("ServiceA.Handle()");
        var callee = new MemberNode("ServiceB.Process()");
        caller.AddChild(callee);
        root.AddChild(caller);

        var result = PlantUmlGenerator.Generate(root);

        // Два компонента
        Assert.That(result, Does.Contain("\"ServiceA\""));
        Assert.That(result, Does.Contain("\"ServiceB\""));
        // Одна связь
        Assert.That(CountStringOccurrences(result, "Rel("), Is.EqualTo(1));
    }

    [Test]
    public void Generate_WithDuplicateClasses_ShouldDeduplicate()
    {
        var root = new MemberNode("Root");
        var caller = new MemberNode("A.DoWork()");
        var callee1 = new MemberNode("B.Helper()");
        var callee2 = new MemberNode("B.Calculate()");
        caller.AddChild(callee1);
        caller.AddChild(callee2);
        root.AddChild(caller);

        var result = PlantUmlGenerator.Generate(root);

        // Два компонента (A, B) — не три
        var componentLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.TrimStart().StartsWith("Component("));
        Assert.That(componentLines, Is.EqualTo(2));

        // Одна связь (A → B)
        var relLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.TrimStart().StartsWith("Rel("));
        Assert.That(relLines, Is.EqualTo(1));
    }

    [Test]
    public void Generate_WithMissingDotInMethodSignature_ShouldCorrectlySkip()
    {
        var root = new MemberNode("Root");
        var caller = new MemberNode("DoSomething()");
        root.AddChild(caller);

        var result = PlantUmlGenerator.Generate(root);

        Assert.That(result, Does.Contain("@startuml"));
        Assert.That(result, Does.Not.Contain("Component("));
    }

    private static int CountStringOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
