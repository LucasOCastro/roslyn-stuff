using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shared;

public readonly record struct TypeRecord(EquatableArray<string> Namespaces, string TypeName, bool IsStatic)
{
    public string DisplayName => Namespaces.Reverse().Append(TypeName).Aggregate(static (a, b) => a + '.' + b);

    public static TypeRecord? FromSyntaxNode(SemanticModel model, TypeDeclarationSyntax syntax)
    {
        var symbol = model.GetDeclaredSymbol(syntax);
        return FromSymbol(symbol as INamedTypeSymbol);
    }
    
    public static TypeRecord? FromSymbol(ITypeSymbol? symbol)
    {
        if (symbol == null) return null;
        
        var namespaces = symbol.GetContainingNamespaces().Select(n => n.Name);
        return new(new(namespaces), symbol.Name, symbol.IsStatic);
    }
}