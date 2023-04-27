using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;

namespace GrpcSourceGenerator;

[Generator]
public class GrpcMessageSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var messages = context.Compilation.GetGrpcMessages();
        var services = context.Compilation.GetGrpcServices();

        foreach (var message in messages)
        {
            var className = message.Identifier.ValueText;
            var line = $"partial class {className} {{}}";

            var source = SourceText.From(line, Encoding.UTF8);

            context.AddSource($"{className}.g.cs", source);
        }

        foreach (var service in services)
        {
            var className = service.Identifier.ValueText;
            var line = $"partial class {className} {{}}";

            var source = SourceText.From(line, Encoding.UTF8);

            context.AddSource($"{className}.g.cs", source);
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif
    }
}
