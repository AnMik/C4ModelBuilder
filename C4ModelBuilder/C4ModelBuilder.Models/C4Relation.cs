namespace C4ModelBuilder.Models;

public readonly record struct C4Relation(
    string FromComponentAlias,
    string ToComponentAlias,
    string? Description = null,
    string? Technology = null);
