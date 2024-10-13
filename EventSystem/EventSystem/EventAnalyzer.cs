using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Shared;

namespace EventSystem;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext ctx)
    {
        var symbol = (INamedTypeSymbol)ctx.Symbol;

        var diagnostics = symbol
            .GetAttributes()
            .SelectMany(a => AnalyzeFromAttribute(a, symbol));
            
        foreach (var diagnostic in diagnostics)
        {
            ctx.ReportDiagnostic(diagnostic.ToDiagnostic());
        }
    }

    private static IEnumerable<DiagnosticRecord> AnalyzeFromAttribute(
        AttributeData attribute, INamedTypeSymbol symbol) =>
        attribute.AttributeClass.GetFullMetadataName() switch
        {
            Attributes.EventAttributeMetadataName => AnalyzeEventType(symbol, attribute),
            Attributes.HandlerAttributeMetadataName => AnalyzeHandlerType(symbol, attribute),
            _ => []
        };

    private static IEnumerable<DiagnosticRecord> AnalyzeEventType(INamedTypeSymbol symbol, AttributeData attribute)
    {
        var attributeSyntax = attribute.ApplicationSyntaxReference?.GetSyntax();
        var attributeLocation = attributeSyntax?.GetLocation() ?? symbol.Locations[0];
        
        if (!symbol.IsPartial())
        {
            yield return Diagnostics.TargetTypeNotPartialRecord(symbol, attribute.AttributeClass!);
        }
        
        var arguments = attribute.ConstructorArguments;
        if (arguments.Length == 0 || arguments[0].IsNull || arguments[0].Value is not INamedTypeSymbol handlerType)
        {
            yield return new(Diagnostics.HandlerTypeMissing, attributeLocation);
            yield break;
        }

        bool handlerHasAttribute = handlerType.GetAttributes().Any(a =>
            a.AttributeClass.GetFullMetadataName() == Attributes.HandlerAttributeMetadataName);
        if (!handlerHasAttribute)
        {
            yield return new(Diagnostics.HandlerTypeMissingAttribute, attributeLocation);
        }
    }
    
    private static IEnumerable<DiagnosticRecord> AnalyzeHandlerType(INamedTypeSymbol symbol, AttributeData attribute)
    {
        if (!symbol.IsPartial())
        {
            yield return Diagnostics.TargetTypeNotPartialRecord(symbol, attribute.AttributeClass!);
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        Diagnostics.AllDiagnostics.ToImmutableArray();
}