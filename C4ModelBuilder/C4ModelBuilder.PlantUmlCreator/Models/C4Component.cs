namespace C4ModelBuilder.PlantUmlCreator;

public readonly record struct C4Component(
    string ComponentAlias,
    string ComponentName,
    string? Description,
    string? Technology);