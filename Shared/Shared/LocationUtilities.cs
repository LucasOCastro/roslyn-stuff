using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shared;

public static class LocationUtilities
{
    public static Location? GetModifiersLocation(this INamedTypeSymbol symbol)
    {
        var syntaxRef = symbol.DeclaringSyntaxReferences
            .FirstOrDefault(s => s.GetSyntax().GetLocation().IsInSource);

        if (syntaxRef?.GetSyntax() is not TypeDeclarationSyntax syntax) 
            return null;
        
        return syntaxRef.SyntaxTree.GetLocation(syntax.Modifiers.Span);
    }

    public static Location? GetTypeDeclarationLine(this INamedTypeSymbol symbol)
    {
        var syntaxRef = symbol.DeclaringSyntaxReferences
            .FirstOrDefault(s => s.GetSyntax().GetLocation().IsInSource);

        if (syntaxRef?.GetSyntax() is not TypeDeclarationSyntax syntax) 
            return null;
        
        var spanStart = syntax.Modifiers.Span.Start;
        var spanEnd = syntax.Identifier.Span.End;
        return syntaxRef.SyntaxTree.GetLocation(TextSpan.FromBounds(spanStart, spanEnd));
    }
}