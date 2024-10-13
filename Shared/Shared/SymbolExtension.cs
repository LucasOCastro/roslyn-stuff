using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shared;

public static class SymbolExtension
{
    /// <returns><c>true</c> if the symbol is partial, <c>false</c> otherwise.</returns>
    public static bool IsPartial(this ISymbol symbol) => symbol.DeclaringSyntaxReferences.Any(syntax =>
        syntax.GetSyntax() is BaseTypeDeclarationSyntax declaration &&
        declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
    
    /// <returns>
    /// The containing namespace symbols, from closest to furthest.
    /// </returns>
    public static IEnumerable<INamespaceOrTypeSymbol> GetContainingNamespaces(this ISymbol symbol)
    {
        var parentNamespace = symbol.ContainingNamespace;
        while (parentNamespace != null && !string.IsNullOrEmpty(parentNamespace.MetadataName))
        {
            yield return parentNamespace;
            parentNamespace = parentNamespace.ContainingNamespace;
        }
    }
    
    private static bool IsRootNamespace(ISymbol? symbol) => symbol is INamespaceSymbol { IsGlobalNamespace: true }; 
    
    /// <returns>
    /// The fully qualified metadata name, including namespaces (ns1.ns2.), nested types (t1+t2+) and arity (t`1).
    /// </returns>
    public static string GetFullMetadataName(this ISymbol? symbol)
    {
        if (symbol == null || IsRootNamespace(symbol))
        {
            return string.Empty;
        }

        StringBuilder sb = new(symbol.MetadataName);
        var lastSymbol = symbol;
        symbol = symbol.ContainingSymbol;
        while (!IsRootNamespace(symbol))
        {
            if (symbol is ITypeSymbol && lastSymbol is ITypeSymbol)
                sb.Insert(0, '+');
            else
                sb.Insert(0, '.');
            sb.Insert(0, symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            symbol = symbol.ContainingSymbol;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the type of the expression, if any.
    /// </summary>
    /// <example>
    /// The expression <c>obj.Bar</c> with the declaration:
    /// <code>
    /// class Foo { public int Bar {get; set;} }
    /// </code>
    /// will return <c>int</c> because <c>Bar</c> is of type <c>int</c>.
    /// </example>
    public static ITypeSymbol? GetExpressionValueType(this ExpressionSyntax syntax, SemanticModel model, CancellationToken cancellationToken)
        => model.GetSymbolInfo(syntax, cancellationToken).Symbol switch
        {
            IFieldSymbol field => field.Type,
            ILocalSymbol local => local.Type,
            IParameterSymbol param => param.Type,
            _ => null
        };
    
    
    /// <summary>
    /// Checks if the <see cref="MemberAccessExpressionSyntax"/> accesses
    /// a member from a type identified by its full metadata name.
    /// </summary>
    /// <example>
    /// Given the following expression <c>obj.Foo</c> with the declaration:
    /// <code>
    /// class Bar { public int Foo {get; set;} }
    /// </code>
    /// and the typeFullMetadataName <c>Bar</c>, this method will return <c>true</c> 
    /// because <c>foo</c> is a member of <c>Bar</c>.
    /// </example>
    /// 
    public static bool AccessesMemberFromType(this MemberAccessExpressionSyntax syntax, string typeFullMetadataName, SemanticModel model, CancellationToken cancellationToken = default)
    {
        var type = GetExpressionValueType(syntax.Expression, model, cancellationToken);
        return type != null && type.GetFullMetadataName() == typeFullMetadataName;
    }
}