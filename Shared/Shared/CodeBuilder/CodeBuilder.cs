using System.Text;

namespace Shared.CodeBuilder;

public class CodeBuilder
{
    private const string Tab = "    ";
    
    private readonly StringBuilder _builder = new();
    private readonly StringBuilder _indent = new();
    
    private int _indentLevel;

    public void IncreaseIndent()
    {
        _indent.Append(Tab);
        _indentLevel++;
    }
    
    public void DecreaseIndent()
    {
        if (_indentLevel == 0) return;
        
        _indent.Remove(_indent.Length - Tab.Length, Tab.Length);
        _indentLevel--;
    }

    /// <summary>
    /// Begins an indented line, appends the provided string and breaks the line.
    /// </summary>
    /// <param name="line"></param>
    public void PushLine(string line)
    {
        _builder.Append(_indent);
        _builder.AppendLine(line);
    }
    
    /// <summary>
    /// Begins an indented line and appends the provided string.
    /// </summary>
    public void Push(string str)
    {
        _builder.Append(_indent);
        _builder.Append(str);
    }
    
    /// <summary>
    /// Breaks the line.
    /// </summary>
    public void AppendLine() => _builder.AppendLine();
    
    /// <summary>
    /// Appends the provided string and breaks the line.
    /// </summary>
    public void AppendLine(string line) => _builder.AppendLine(line);
    
    /// <summary>
    /// Appends the provided string.
    /// </summary>
    public void Append(string str) => _builder.Append(str);
    
    public override string ToString() => _builder.ToString();

    public string End()
    {
        while (_indentLevel > 0)
        {
            DecreaseIndent();
        }

        return ToString();
    }
}