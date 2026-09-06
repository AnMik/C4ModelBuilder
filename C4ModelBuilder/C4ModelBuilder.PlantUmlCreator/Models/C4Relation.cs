namespace C4ModelBuilder.PlantUmlCreator;

public readonly record struct C4Relation(
    string FromComponentAlias,
    string ToComponentAlias,
    string? Description = null,
    string? Technology = null);