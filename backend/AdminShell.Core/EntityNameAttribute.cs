namespace AdminShell.Core;

[AttributeUsage(AttributeTargets.Class)]
public sealed class EntityNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
