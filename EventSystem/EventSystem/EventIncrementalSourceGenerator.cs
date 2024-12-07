using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Shared;
using Shared.CodeBuilder;

namespace EventSystem;

[Generator]
public class EventIncrementalSourceGenerator : IIncrementalGenerator
{
    private const string AnyEventDelegateName = "OnEventRaisedDelegate";
    private const string AnyEventName = "OnEventRaised";
    private const string ParameterlessSuffix = "NoArgs";
    
    private const string EnumName = "Events";

    private const string InterfaceName = "IEvent";
    private const string InterfaceEnumTypeName = "EventType";
    
    private const string InvokeMethodName = "Raise";
    private const string AddListenerMethodName = "AddListener";
    private const string RemoveListenerMethodName = "RemoveListener";
    
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute to the compilation.
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            Attributes.AttributesFileName,
            SourceText.From(Attributes.AttributesSourceCode, Encoding.UTF8)));
        
        // Filter structs annotated with the [Event] attribute and groups by event handler.
        var eventProvider = context.SyntaxProvider.ForAttributeWithMetadataName(Attributes.EventAttributeMetadataName,
                static (s, _) => s is StructDeclarationSyntax,
                static (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(static record => record != null)
            .Select(static (record, _) => record!.Value)
            .Collect()
            .Select(static (records, _) => GetEventHandlerRecords(records))
            .SelectMany((records, _) => records);
        
        context.RegisterSourceOutput(eventProvider, static (spc, source) => Execute(source, spc));
    }

    private static void Execute(EventHandlerRecord source, SourceProductionContext spc)
    {
        if (DiagnosticRecord.ReportMany(source.AllDiagnostics, spc))
            return;
        
        Dictionary<EventStructRecord, (string Trim, string Delegate, string Event, string Full)> nameCache = [];

        string staticStr = source.Type.IsStatic ? "static " : "";
        
        var builder = new CodeBuilder();
        using (new TypeScope(builder, source.Type))
        {
            // Any delegate
            builder.PushLine($"public delegate void {AnyEventDelegateName}({EnumName} eventType, {InterfaceName} ev);");
            
            // Any event line
            builder.PushLine($"public {staticStr}event {AnyEventDelegateName} {AnyEventName};");

            foreach (var e in source.Events)
            {
                string typeName = e.Type.TypeName;
                string trimmedTypeName = typeName;
                // ExampleEvent -> Example
                if (typeName.EndsWith("event", StringComparison.OrdinalIgnoreCase))
                    trimmedTypeName = trimmedTypeName.Substring(0, typeName.Length - 5);
                // OnExample -> Example
                if (typeName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                    trimmedTypeName = trimmedTypeName.Substring(2);
                string delegateName = $"On{trimmedTypeName}Delegate";
                string eventName = $"On{trimmedTypeName}";
                string fullName = e.Type.DisplayName;
                nameCache[e] = (trimmedTypeName, delegateName, eventName, fullName);

                //Delegate declaration
                builder.PushLine($"public delegate void {delegateName}({fullName} ev);");

                //Event line
                builder.PushLine($"public {staticStr}event {delegateName} {eventName};");

                //Parameterless event
                builder.PushLine($"public {staticStr}event System.Action {eventName}{ParameterlessSuffix};");

                //Invoke method
                builder.PushLine($"public {staticStr}void {InvokeMethodName}({fullName} ev)");
                using (new BracesScope(builder))
                {
                    builder.PushLine($"{AnyEventName}?.Invoke({EnumName}.{eventName}, ev);");
                    builder.PushLine($"{eventName}?.Invoke(ev);");
                    builder.PushLine($"{eventName}{ParameterlessSuffix}?.Invoke();");
                }
            }

            // Generate enum
            builder.PushLine($"public enum {EnumName}");
            using (new BracesScope(builder))
            {
                foreach (var e in source.Events)
                {
                    builder.PushLine($"{nameCache[e].Event},");
                }
            }
            
            // Add listener method
            builder.PushLine($"public {staticStr}void {AddListenerMethodName}({EnumName} eventType, System.Action<{InterfaceName}> listener)");
            using (new BracesScope(builder))
            {
                using (new SwitchScope(builder, "eventType"))
                {
                    foreach (var e in source.Events)
                    {
                        string caseStr = $"{EnumName}.{nameCache[e].Event}";
                        using (new SwitchScope.CaseScope(builder, caseStr))
                        {
                            builder.PushLine($"{nameCache[e].Event}{ParameterlessSuffix} += listener;");
                        }
                    }
                }
            }
            
            // Remove listener method
            builder.PushLine($"public {staticStr}void {RemoveListenerMethodName}({EnumName} eventType, System.Action<{InterfaceName}> listener)");
            using (new BracesScope(builder))
            {
                using (new SwitchScope(builder, "eventType"))
                {
                    foreach (var e in source.Events)
                    {
                        string caseStr = $"{EnumName}.{nameCache[e].Event}";
                        using (new SwitchScope.CaseScope(builder, caseStr))
                        {
                            builder.PushLine($"{nameCache[e].Event}{ParameterlessSuffix} -= listener;");
                        }
                    }
                } 
            }
            
            // Generate event interface
            builder.PushLine($"public interface {InterfaceName}");
            using (new BracesScope(builder))
            {
                builder.PushLine($"{EnumName} {InterfaceEnumTypeName} {{ get; }}");
            }
        }

        string srcCode = "// <auto-generated/>\n" + builder.End();
        spc.AddSource($"{source.Type.DisplayName}.g.cs", SourceText.From(srcCode, Encoding.UTF8));

        // ---- Partial event ---- //
        
        string interfaceName = $"{source.Type.DisplayName}.{InterfaceName}";
        //Generate event partial
        foreach (var e in source.Events)
        {
            CodeBuilder eventBuilder = new();
            using (new TypeScope(eventBuilder, e.Type, implements: [interfaceName]))
            {
                string enumType = $"{source.Type.DisplayName}.{EnumName}";
                string enumValue = $"{enumType}.{nameCache[e].Event}";
                
                //Const enum
                eventBuilder.PushLine($"public const {enumType} EnumValue = {enumValue};");
                
                //Overriden enum
                eventBuilder.PushLine($"public {enumType} {InterfaceEnumTypeName} => EnumValue;");
            }
            
            string eventSrcCode = "// <auto-generated/>\n" + eventBuilder.End();
            spc.AddSource($"{nameCache[e].Full}.g.cs", SourceText.From(eventSrcCode, Encoding.UTF8));
        }
    }

    private static EventStructRecord? GetClassDeclarationForSourceGen(GeneratorAttributeSyntaxContext ctx)
    {
        var classDef = (StructDeclarationSyntax)ctx.TargetNode;
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        if (ctx.Attributes.First().ConstructorArguments.First().Value is not INamedTypeSymbol handlerType)
        {
            return new()
            {
                Diagnostic = new(Diagnostics.HandlerTypeMissing, classDef.GetLocation())
            };
        }

        //Event struct must be partial
        if (!symbol.IsPartial())
        {
            return new()
            {
                Diagnostic = new(Diagnostics.TargetTypeNotPartial, classDef.GetLocation())
            };
        }
        
        var handlerTypeRecord = TypeRecord.FromSymbol(handlerType);
        if (handlerTypeRecord == null) return null;

        var typeRecord = TypeRecord.FromSymbol(symbol);
        if (typeRecord == null) return null;
        
        return new(typeRecord.Value, handlerTypeRecord.Value, null);
    }

    private static EquatableArray<EventHandlerRecord> GetEventHandlerRecords(ImmutableArray<EventStructRecord> records)
    {
        return new(records
            .GroupBy(record => record.HandlerType)
            .Select(group =>
                new EventHandlerRecord(group.Key, new(group), null)
            ));
    }
}