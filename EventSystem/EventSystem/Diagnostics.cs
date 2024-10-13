using Microsoft.CodeAnalysis;
using Shared;

namespace EventSystem;

public static class Diagnostics
{
    public const string TargetTypeNotPartialId = "GDFEV001"; 
    public static readonly DiagnosticDescriptor TargetTypeNotPartial = new(
        id: TargetTypeNotPartialId,
        title: "Target type is not partial",
        messageFormat: "Missing partial modifier on declaration of type '{0}' marked with '{1}'",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static DiagnosticRecord TargetTypeNotPartialRecord(INamedTypeSymbol type, INamedTypeSymbol attribute)
        => new(TargetTypeNotPartial, type.GetTypeDeclarationLine(), 
            type.ToDisplayString(), attribute.ToDisplayString());

    public const string HandlerTypeMissingAttributeId = "GDFEV002";
    public static readonly DiagnosticDescriptor HandlerTypeMissingAttribute = new(
        id: HandlerTypeMissingAttributeId,
        title: "Handler type missing attribute",
        messageFormat: $"Handler type needs to have the {Attributes.HandlerAttributeMetadataName} attribute",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public const string HandlerTypeMissingId = "GDFEV003"; 
    public static readonly DiagnosticDescriptor HandlerTypeMissing = new(
        id: HandlerTypeMissingId,
        title: "Handler type is missing",
        messageFormat: "Handler type is missing",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor[] AllDiagnostics =
    [
        TargetTypeNotPartial,
        HandlerTypeMissingAttribute,
        HandlerTypeMissing
    ];
}