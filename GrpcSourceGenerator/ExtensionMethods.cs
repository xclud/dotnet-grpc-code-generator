using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace GrpcSourceGenerator;

internal static class ExtensionMethods
{
    private const string GrpcMessageAttributeName = "GrpcMessage";
    private const string GrpcServiceAttributeName = "GrpcService";

    private static ImmutableArray<ClassDeclarationSyntax> GetClassesWithAttribute(this Compilation compilation, string attributeName)
    {
        // Get all classes
        IEnumerable<SyntaxNode> allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
        IEnumerable<ClassDeclarationSyntax> allClasses = allNodes
            .Where(d => d.IsKind(SyntaxKind.ClassDeclaration))
            .OfType<ClassDeclarationSyntax>();

        return allClasses
            .Where(cls => cls.HasAttribute(attributeName))
            .Where(IsPartial)
            .ToImmutableArray();
    }

    public static ImmutableArray<ClassDeclarationSyntax> GetGrpcMessages(this Compilation compilation)
    {
        return GetClassesWithAttribute(compilation, GrpcMessageAttributeName);
    }

    public static ImmutableArray<ClassDeclarationSyntax> GetGrpcServices(this Compilation compilation)
    {
        return GetClassesWithAttribute(compilation, GrpcServiceAttributeName);
    }

    private static bool IsPartial(this ClassDeclarationSyntax component)
    {
        return component.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static bool HasAttribute(this ClassDeclarationSyntax component, string attributeName)
    {
        var attributes = component.AttributeLists
            .SelectMany(x => x.Attributes)
            .Where(attr => attr.Name.ToString() == attributeName)
            .ToList();

        var attributeWithName = attributes.FirstOrDefault(attr => attr.Name.ToString() == attributeName);

        if (attributeWithName is null)
        {
            return false;
        }

        //var semanticModel = compilation.GetSemanticModel(component.SyntaxTree);

        //var name = component.Identifier.ValueText;

        //if (menuItemAttribute.ArgumentList != null && menuItemAttribute.ArgumentList.Arguments.Count > 0)
        //{
        //    var nameArg = menuItemAttribute.ArgumentList.Arguments[0];
        //    var nameExpr = nameArg.Expression;
        //    name = semanticModel.GetConstantValue(nameExpr).ToString();
        //}


        return true;
    }
}
