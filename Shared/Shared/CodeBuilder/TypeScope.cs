using System;
using System.Linq;

namespace Shared.CodeBuilder;

public readonly struct TypeScope : IDisposable
{
    private readonly NamespaceScope _namespaceScope;
    private readonly BracesScope _bracesScope;

    public TypeScope(CodeBuilder builder, TypeRecord typeRecord, string[]? implements = null)
    {
        _namespaceScope = new(builder, typeRecord.Namespaces);

        if (typeRecord.IsPartial)
        {
            builder.Push("partial ");
        }
        else
        {
            string access = typeRecord.AccessModifier switch
            {
                TypeAccessModifier.Private => "private ",
                TypeAccessModifier.Public => "public ",
                TypeAccessModifier.Internal => "internal ",
                _ => throw new ArgumentOutOfRangeException(nameof(typeRecord.AccessModifier), typeRecord.AccessModifier, null)
            };
            builder.Push(access);
        
            string modifier = typeRecord.Modifier switch
            {
                TypeModifier.Static => "static ",
                TypeModifier.Abstract => "abstract ",
                TypeModifier.Sealed => "sealed ",
                _ => ""
            };
            builder.Append(modifier);
        }
        
        string typeType = typeRecord.Type switch
        {
            TypeType.Class => "class ",
            TypeType.Struct => "struct ",
            _ => throw new ArgumentOutOfRangeException(nameof(typeRecord.Type), typeRecord.Type, null)
        };
        builder.Append(typeType);
        
        builder.Append(typeRecord.TypeName);
        if (implements is { Length: > 0 })
        {
            builder.Append(" : ");
            builder.Append(implements.Aggregate((a,b) => a + ", " + b));
        }

        builder.AppendLine();
        
        _bracesScope = new(builder);
    }
    
    public void Dispose()
    {
        _bracesScope.Dispose();
        _namespaceScope.Dispose();
    }
}