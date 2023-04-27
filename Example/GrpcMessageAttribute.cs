namespace System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GrpcMessageAttribute : System.Attribute
{
    public string? Name { get; }

    public GrpcMessageAttribute(string? name = null)
    {
        Name = name;
    }
}
