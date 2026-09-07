namespace C4ModelBuilder.Models;

/// <summary>
/// Level 2
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class C4ContainerAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;

    public string Description { get; } = description;
}
