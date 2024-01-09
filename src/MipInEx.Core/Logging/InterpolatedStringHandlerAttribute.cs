namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
internal sealed class InterpolatedStringHandlerAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
{
    public InterpolatedStringHandlerArgumentAttribute(string argument)
    {
        this.Arguments = new string[] { argument };
    }

    public InterpolatedStringHandlerArgumentAttribute(params string[] arguments)
    {
        this.Arguments = arguments;
    }

    public string[] Arguments { get; }
}
