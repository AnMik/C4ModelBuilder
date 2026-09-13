namespace C4ModelBuilder.Models.Attributes;

/// <summary>
/// Level 3
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class C4ComponentAttribute : Attribute
{
    public bool IsRoot { get; set; }

    public string? Description { get; set; }
}
