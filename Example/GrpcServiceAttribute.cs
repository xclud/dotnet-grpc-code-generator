namespace System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GrpcServiceAttribute : System.Attribute
{
    public string? Name { get; }

    public GrpcServiceAttribute(string? name = null)
    {
        Name = name;
    }
}
