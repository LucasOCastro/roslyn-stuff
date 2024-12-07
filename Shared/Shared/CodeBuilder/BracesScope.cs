using System;

namespace Shared.CodeBuilder;

public readonly struct BracesScope : IDisposable
{
    private readonly CodeBuilder _builder;
    
    public BracesScope(CodeBuilder builder) 
    {
        _builder = builder;
        builder.PushLine("{");
        builder.IncreaseIndent();
    }
    
    public void Dispose()
    {
        _builder.DecreaseIndent();
        _builder.PushLine("}");
    }
}