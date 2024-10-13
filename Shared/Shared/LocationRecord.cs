using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Shared;

/// <summary>
/// Represents a location in a source file.
/// </summary>
public readonly record struct LocationRecord(SyntaxTree SyntaxTree, TextSpan TextSpan)
{
    public Location ToLocation() => Location.Create(SyntaxTree, TextSpan);

    public static LocationRecord? CreateFrom(SyntaxNode node) => CreateFrom(node.GetLocation());

    public static LocationRecord? CreateFrom(Location location)
    {
        if (location.SourceTree is null)
        {
            return null;
        }

        return new LocationRecord(location.SourceTree, location.SourceSpan);
    }
}