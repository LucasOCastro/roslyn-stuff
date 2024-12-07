using System;

namespace Shared.CodeBuilder;

public readonly struct SwitchScope : IDisposable
{
    private readonly BracesScope _bracesScope;

    public SwitchScope(CodeBuilder builder, string expression)
    {
        builder.PushLine($"switch ({expression})");
        _bracesScope = new(builder);
    }
    
    public void Dispose() => _bracesScope.Dispose();
    
    public readonly struct CaseScope : IDisposable
    {
        private readonly CodeBuilder _builder;
        private readonly BracesScope _bracesScope;
    
        public CaseScope(CodeBuilder builder, string caseExpression)
        {
            _builder = builder;
            builder.PushLine($"case {caseExpression}:");
            _bracesScope = new(builder);
        }
    
        public void Dispose()
        {
            _builder.PushLine("break;");
            _bracesScope.Dispose();
        }
    }
}