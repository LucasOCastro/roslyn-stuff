using System;
using System.Collections.Generic;

namespace Shared.CodeBuilder;

public readonly struct NamespaceScope : IDisposable
{
    private readonly BracesScope _bracesScope;

    public NamespaceScope(CodeBuilder builder, string namespaceName)
    {
        builder.Append("namespace ");
        builder.AppendLine(namespaceName);
        _bracesScope = new(builder);
    }

    public NamespaceScope(CodeBuilder builder, IEnumerable<string> namespaces) 
        : this(builder, string.Join(".", namespaces))
    {
    }
    
    public void Dispose()
    {
        _bracesScope.Dispose();
    }
}