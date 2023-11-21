using System;

/// <summary>
/// Attribute to apply to SnapshotProcessor classes.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SnapshotProcessorAttribute : Attribute
{
    public readonly int id;

    public SnapshotProcessorAttribute (int id) {
        this.id = id;
    }
}