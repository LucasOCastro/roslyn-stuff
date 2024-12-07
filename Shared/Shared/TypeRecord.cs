using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shared;

public record struct TypeRecord(
    EquatableArray<string> Namespaces,
    string TypeName,
    TypeAccessModifier AccessModifier,
    TypeType Type,
    bool IsPartial = false,
    TypeModifier Modifier = TypeModifier.None)
{
    public string DisplayName => Namespaces.Reverse().Append(TypeName).Aggregate(static (a, b) => a + '.' + b);
    
    public bool IsStatic => Modifier == TypeModifier.Static;

    public static TypeRecord? FromSyntaxNode(SemanticModel model, TypeDeclarationSyntax syntax)
    {
        var symbol = model.GetDeclaredSymbol(syntax);
        return FromSymbol(symbol as INamedTypeSymbol);
    }
    
    public static TypeRecord? FromSymbol(ITypeSymbol? symbol)
    {
        if (symbol == null) return null;
        
        var namespaces = symbol.GetContainingNamespaces().Select(n => n.Name).Reverse();
        var name = symbol.Name;
        
        var accessModifier = symbol.DeclaredAccessibility switch
        {
            Accessibility.Private => TypeAccessModifier.Private,
            Accessibility.Public => TypeAccessModifier.Public,
            Accessibility.Internal => TypeAccessModifier.Internal,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var type = symbol.TypeKind switch
        {
            TypeKind.Struct => TypeType.Struct,
            TypeKind.Class => TypeType.Class,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var modifier = TypeModifier.None;
        if (symbol.IsAbstract) modifier = TypeModifier.Abstract;
        else if (symbol.IsSealed) modifier = TypeModifier.Sealed;
        else if (symbol.IsStatic) modifier = TypeModifier.Static;
        
        bool isPartial = symbol.IsPartial();
        return new(new(namespaces), name, accessModifier, type, isPartial, modifier);
    }
}

public enum TypeAccessModifier
{
    Private,
    Public,
    Internal
}
    
public enum TypeType
{
    Struct,
    Class
}
    
public enum TypeModifier
{
    None,
    Static,
    Abstract,
    Sealed
}