namespace C4ModelBuilder.Models;

public readonly record struct C4Component(
    string ComponentAlias,
    string ComponentName,
    string? Description,
    string? Technology);
