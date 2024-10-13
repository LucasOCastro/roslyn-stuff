using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Shared;

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
        
        string staticStr = source.Type.IsStatic ? "static " : "";
        
        Dictionary<EventStructRecord, (string Trim, string Delegate, string Event, string Full)> nameCache = [];
        
        var builder = ClassStringBuilder.FromTypeRecord(source.Type);
        
        //Any delegate
        builder.InitLine();
        builder.PushMethodSignature("public delegate void", AnyEventDelegateName, $"{EnumName} eventType", $"{InterfaceName} ev");
        builder.Push(';');
        //Any Event line
        builder.PushLine($"public {staticStr}event {AnyEventDelegateName} {AnyEventName};");
        
        
        foreach (var e in source.Events)
        {
            string typeName = e.Type.TypeName;
            string trimmedTypeName = typeName;
            if (typeName.EndsWith("event", StringComparison.OrdinalIgnoreCase))
                trimmedTypeName = trimmedTypeName.Substring(0, typeName.Length - 5);
            if (typeName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                trimmedTypeName = trimmedTypeName.Substring(2);
            string delegateName = $"On{trimmedTypeName}Delegate";
            string eventName = $"On{trimmedTypeName}";
            string fullName = e.Type.DisplayName;
            nameCache[e] = (trimmedTypeName, delegateName, eventName, fullName);
            
            //Delegate declaration
            builder.InitLine();
            builder.PushMethodSignature("public delegate void", delegateName, fullName + " ev");
            builder.Push(';');
            //Event line
            builder.PushLine($"public {staticStr}event {delegateName} {eventName};");
            //Parameterless event
            builder.PushLine($"public {staticStr}event System.Action {eventName}{ParameterlessSuffix};");
            
            //Invoke method
            builder.OpenMethod($"public {staticStr}void", InvokeMethodName, fullName + " ev");
            builder.PushMethodInvocation($"{AnyEventName}?.Invoke", $"{EnumName}.{eventName}", "ev");
            builder.PushMethodInvocation($"{eventName}?.Invoke", "ev");
            builder.PushMethodInvocation($"{eventName}{ParameterlessSuffix}?.Invoke", "ev");
            builder.Close();
        }
        
        //Generate enum
        builder.PushLine($"public enum {EnumName}");
        builder.Open();
        foreach (var e in source.Events)
        {
            builder.PushLine(nameCache[e].Event);
            builder.Push(',');
        }
        builder.Close();
        
        //Add listener method
        const string eventEnumParamName = "eventType";
        builder.OpenMethod($"public {staticStr}void", AddListenerMethodName, $"{EnumName} {eventEnumParamName}", $"System.Action<{InterfaceName}> listener");
        builder.OpenSwitch(eventEnumParamName);
        foreach (var e in source.Events)
        {
            builder.OpenSwitchCase(EnumName + '.' + nameCache[e].Event);
            builder.PushAssignment(nameCache[e].Event + ParameterlessSuffix, "+=");
            builder.Push("listener;");
            builder.CloseSwitchCase();
        }
        builder.Close();
        builder.Close();
        
        //Remove listener method
        builder.OpenMethod($"public {staticStr}void", RemoveListenerMethodName, $"{EnumName} {eventEnumParamName}", $"System.Action<{InterfaceName}> listener");
        builder.OpenSwitch(eventEnumParamName);
        foreach (var e in source.Events)
        {
            builder.OpenSwitchCase(EnumName + '.' + nameCache[e].Event);
            builder.PushAssignment(nameCache[e].Event + ParameterlessSuffix, "-=");
            builder.Push("listener;");
            builder.CloseSwitchCase();
        }
        builder.Close();
        builder.Close();
        
        //Generate event interface
        builder.PushLine($"public interface {InterfaceName}");
        builder.Open();
        builder.PushLine($"{EnumName} {InterfaceEnumTypeName} {{ get; }}");
        builder.Close();

        string srcCode = "// <auto-generated/>\n" + builder.End();
        spc.AddSource($"{source.Type.DisplayName}.g.cs", SourceText.From(srcCode, Encoding.UTF8));

        string interfaceName = $"{source.Type.DisplayName}.{InterfaceName}";
        //Generate event partial
        foreach (var e in source.Events)
        {
            ClassStringBuilder eventBuilder = ClassStringBuilder.FromTypeRecord(e.Type, isStruct: true, interfaceName);

            string enumType = $"{source.Type.DisplayName}.{EnumName}";
            string enumValue = $"{enumType}.{nameCache[e].Event}";
            //Const enum
            eventBuilder.PushAssignment($"public const {enumType} EnumValue");
            eventBuilder.Push(enumValue);
            eventBuilder.Push(';');
            
            //Overriden enum
            eventBuilder.PushLine($"public {enumType} {InterfaceEnumTypeName} => {enumValue};");
            
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