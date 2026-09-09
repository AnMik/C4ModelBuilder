namespace C4ModelBuilder.Models.Attributes;

/// <summary>
/// Level 3
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class C4ComponentAttribute(bool isRoot = false, string description = "") : Attribute
{
    public bool IsRoot { get; } = isRoot;

    public string Description { get; } = description;
}
