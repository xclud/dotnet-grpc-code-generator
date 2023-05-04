using Google.Protobuf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace GrpcSourceGenerator;

[Generator]
public class GrpcMessageSourceGenerator : ISourceGenerator
{
    private static FieldDescriptorProto.Types.Type GetType(TypeSyntax type)
    {
        if (type is NullableTypeSyntax nullableType)
        {
            return GetType(nullableType.ElementType);
        }

        if (type is PredefinedTypeSyntax pts)
        {
            var keyword = pts.Keyword.ValueText;

            switch (keyword)
            {
                case "int":
                    return FieldDescriptorProto.Types.Type.Int32;
                case "uint":
                    return FieldDescriptorProto.Types.Type.Uint32;
                case "double":
                    return FieldDescriptorProto.Types.Type.Double;
                case "float":
                    return FieldDescriptorProto.Types.Type.Float;
                case "string":
                    return FieldDescriptorProto.Types.Type.String;
            }
        }

        throw new NotImplementedException();
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var messages = context.Compilation.GetGrpcMessages();
        var services = context.Compilation.GetGrpcServices();

        foreach (var message in messages)
        {
            var className = message.Identifier.ValueText;
            var pros = message.GetProperties();

            var md = new DescriptorProto();
            md.Name = className;
            var calculateSizeLines = new List<string>();
            var mergeFromLines = new List<string>();
            var equalsLines = new List<string>();
            var cloneLines = new List<string>();


            foreach (var prop in pros)
            {
                var name = prop.Identifier.ValueText;
                var type = GetType(prop.Type);
                var nullable = prop.IsNullable();

                var fd = new FieldDescriptorProto
                {
                    Name = name,
                    Type = type,
                };

                md.Field.Add(fd);
                equalsLines.Add($@"
        if ({name} != other.{name})
        {{
            return false;
        }}");

                if (type == FieldDescriptorProto.Types.Type.Message)
                {
                    cloneLines.Add($"\t\t\t{name} = {name}.Clone(),");
                }
                else
                {
                    cloneLines.Add($"\t\t\t{name} = {name},");
                }

                if (type == FieldDescriptorProto.Types.Type.Int32)
                {
                    calculateSizeLines.Add($@"
        if ({name} != 0)
        {{
            size += 2 + pb::CodedOutputStream.ComputeInt32Size({name});
        }}");
                    var nullStatement = nullable ? $"other.{name} != null && " : string.Empty;
                    mergeFromLines.Add($@"
        if ({nullStatement}other.{name} != 0)
        {{
            {name} = other.{name};
        }}");
                }

                else if (type == FieldDescriptorProto.Types.Type.Uint32)
                {
                    calculateSizeLines.Add($@"
        if ({name} != 0)
        {{
            size += 2 + pb::CodedOutputStream.ComputeUInt32Size({name});
        }}");

                    var nullStatement = nullable ? $"other.{name} != null && " : string.Empty;
                    mergeFromLines.Add($@"
        if ({nullStatement}other.{name} != 0)
        {{
            {name} = other.{name};
        }}");
                }
                else if (type == FieldDescriptorProto.Types.Type.String)
                {
                    calculateSizeLines.Add($@"
        if (!string.IsNullOrEmpty({name}))
        {{
            size += 1 + pb::CodedOutputStream.ComputeStringSize({name});
        }}");

                    mergeFromLines.Add($@"
        if (!string.IsNullOrEmpty(other.{name}))
        {{
            {name} = other.{name};
        }}");



                }
            }

            var mdBytes = string.Join(", ", md.ToByteArray().Select(x => string.Format("0x{0:X2}", x)));


            var lines = new List<string>
            {
                "#nullable enable",
                "#pragma warning disable CS8981",
                "using pb = global::Google.Protobuf;",
                "using pbr = global::Google.Protobuf.Reflection;",
                "#pragma warning restore CS8981",
                "",
                "namespace Example;",
                "",
                $"partial class {className} : pb::IMessage<{className}> {{",


            };


            lines.Add($"    private static readonly byte[] descriptorData = new byte[] {{{mdBytes}}};");
            lines.Add($"    private static readonly pb::MessageParser<{className}> _parser = new pb::MessageParser<{className}>(() => new {className}());");
            lines.Add($"    private static readonly pbr::FileDescriptor descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData, new pbr::FileDescriptor[] {{ }}, new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {{   new pbr::GeneratedClrTypeInfo(typeof({className}), _parser, new[]{{ \"Shortcode\", \"Url\", \"StaticUrl\", \"VisibleInPicker\", \"Category\" }}, new[]{{ \"StaticUrl\", \"VisibleInPicker\", \"Category\" }}, null, null, null) }}));");

            lines.Add($@"
    pbr::MessageDescriptor pb::IMessage.Descriptor => descriptor.MessageTypes[0];

    int pb::IMessage.CalculateSize()
    {{
        int size = 0;
{string.Join("\n", calculateSizeLines)}

        return size;
    }}

    {className} pb::IDeepCloneable<{className}>.Clone()
    {{
        return new {className}
        {{
{string.Join("\n", cloneLines)}
        }};
    }}

    public bool Equals({className}? other)
    {{
        if (ReferenceEquals(other, null)) {{
            return false;
        }}
        if (ReferenceEquals(other, this)) {{
            return true;
        }}
{string.Join("\n", equalsLines)}

        return true;
    }}

    void pb::IMessage<{className}>.MergeFrom({className}? other)
    {{
        if (other == null)
        {{
            return;
        }}
{string.Join("\n", mergeFromLines)}
    }}

    public void MergeFrom(pb::CodedInputStream input)
    {{
        throw new NotImplementedException();
    }}

    public void WriteTo(pb::CodedOutputStream output)
    {{
        throw new NotImplementedException();
    }}");

            lines.Add("}"); // class
            lines.Add("#nullable restore");

            var source = SourceText.From(string.Join("\n", lines), Encoding.UTF8);

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
