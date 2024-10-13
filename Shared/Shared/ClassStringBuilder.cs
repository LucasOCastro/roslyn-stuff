using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared;

/// <summary>
/// Wrapper for <see cref="StringBuilder"/> to write classes,
/// handling indentation level and context blocks.
/// </summary>
public class ClassStringBuilder
{
    private readonly StringBuilder _builder = new();
    
    private const string Tab = "    ";
    private int _level;
    private readonly StringBuilder _indent = new();

    public ClassStringBuilder()
    {
    }
    
    public ClassStringBuilder(string namespaceName)
    {
        OpenNamespace(namespaceName);
    }

    public ClassStringBuilder(string namespaceName, string className, bool isStruct = false, params string[] bases)
    {
        OpenNamespace(namespaceName);
        OpenPartialClass(className, isStruct, bases);
    }
    
    public ClassStringBuilder(IEnumerable<string> namespaces, string className, bool isStruct = false, params string[] bases)
    {
        foreach (var name in namespaces)
        {
            OpenNamespace(name);
        }
        OpenPartialClass(className, isStruct, bases);
    }

    /// <summary> Finishes the class, closing every open block and returning the string. </summary>
    public string End()
    {
        while (_level > 0)
        {
            Close();
        }

        return _builder.ToString();
    }
    
    /// <summary> Adds a correctly indented line. </summary>
    public void PushLine(string line)
    {
        InitLine();
        _builder.Append(line);
    }

    /// <summary> If in a line, break first. Then initializes the current line with correct indentation. </summary>
    public void InitLine()
    {
        _builder.AppendLine();
        _builder.Append(_indent);
    }
    
    /// <summary> Appends the provided string directly. </summary>
    public void Push(string str) => _builder.Append(str);

    /// <summary> Appends the provided char directly. </summary>
    public void Push(char c) => _builder.Append(c);

    /// <summary>
    /// Appends a new line with the assignment '<see cref="varName"/> =', or instead of '='
    /// the value passed as '<see cref="assignOperator"/>'.
    /// </summary>
    public void PushAssignment(string varName, string assignOperator = "=")
    {
        InitLine();
        _builder.Append(varName);
        _builder.Append(' ');
        _builder.Append(assignOperator);
        _builder.Append(' ');
    }
    
    /// <summary>
    /// Appends a new line with an instruction to assign a variable
    /// '<see cref="varName"/>' with the return of '<see cref="methodName"/>'.
    /// </summary>
    /// <remarks>If the variable was not defined yet, pass <see cref="varName"/> with 'var'.</remarks>
    public void PushAssignmentFromMethod(string varName, string methodName, params string[] args)
    {
        PushAssignment(varName);
        PushMethodInvocationInline(methodName, args);
    }

    /// <summary> Appends the invocation of a method. </summary>
    public void PushMethodInvocationInline(string methodName, params string[] args)
    {
        _builder.Append(methodName);
        PushArgumentList(args);
        _builder.Append(";");
    }
    
    /// <summary> Begins a new line and appends the invocation of a method. </summary>
    public void PushMethodInvocation(string methodName, params string[] args)
    {
        InitLine();
        PushMethodInvocationInline(methodName, args);
    }
    
    /// <summary> Appends the invocation of a generic method. </summary>
    public void PushGenericMethodInvocationInline(string methodName, IEnumerable<string> genericArguments, params string[] args)
    {
        _builder.Append(methodName);
        PushGenericArgumentList(genericArguments);
        PushArgumentList(args);
        _builder.Append(";");
    }
    
    /// <summary> Begins a new line and appends the invocation of a generic method. </summary>
    public void PushGenericMethodInvocation(string methodName, IEnumerable<string> genericArguments, params string[] args)
    {
        InitLine();
        PushGenericMethodInvocationInline(methodName, genericArguments, args);
    }

    /// <summary> Appends a (arg1, arg2, ...) block with the provided arguments. </summary>
    private void PushArgumentList(IEnumerable<string> arguments)
    {
        _builder.Append('(');
        string? lastArg = null;
        foreach (var arg in arguments)
        {
            if (lastArg != null) _builder.Append(", ");
            _builder.Append(arg);
            lastArg = arg;
        }
        _builder.Append(')');
    }

    /// <summary> Appends a &lt;T1, T2, ...&gt; block with the provided types. </summary>
    private void PushGenericArgumentList(IEnumerable<string> genericArguments)
    {
        _builder.Append('<');
        string? lastType = null;
        foreach (var type in genericArguments)
        {
            if (lastType != null) _builder.Append(", ");
            _builder.Append(type);
            lastType = type;
        }

        if (lastType == null) _builder.Remove(_builder.Length - 1, 1);
        else _builder.Append('>');
    }
    
    /// <summary>
    /// Appends a full method signature.<br/>
    /// '<see cref="signature"/>' '<see cref="methodName"/>' ('<see cref="args"/>')
    /// </summary>
    /// <example>
    /// signature = "public partial void"; methodName = "Foo", args = ["string Bar"]<br/>
    /// <b>Result:</b><br/>
    /// public partial void Foo(string Bar)
    /// </example>
    public void PushMethodSignature(string signature, string methodName, params string[] args)
    {
        _builder.Append(signature);
        _builder.Append(' ');
        _builder.Append(methodName);
        PushArgumentList(args);
    }
    
    /// <summary>
    /// Appends a full generic method signature.<br/>
    /// '<see cref="signature"/>' '<see cref="methodName"/>'&lt;T1, T2, ...&gt;('<see cref="args"/>') where T1 : x where T2 : y 
    /// </summary>
    /// <example>
    /// signature = "public partial void"; methodName = "Foo", genericArgs = [{"T1", "struct"}] args = ["string Bar"]<br/>
    /// <b>Result:</b><br/>
    /// public partial void Foo&lt;T1&gt;(string Bar) where T1: struct
    /// </example>
    public void PushGenericMethodSignature(string signature, string methodName, ICollection<GenericArgDef> genericArgs, params string[] args)
    {
        _builder.Append(signature);
        _builder.Append(' ');
        _builder.Append(methodName);
        PushGenericArgumentList(genericArgs.Select(a => a.Arg));
        PushArgumentList(args);
        
        foreach (var genericArg in genericArgs)
        {
            _builder.Append(" where ");
            _builder.Append(genericArg.Arg);
            _builder.Append(" : ");

            if (genericArg.Constraints.Length == 0) continue;
            bool first = true;
            foreach (var constraint in genericArg.Constraints)
            {
                if (!first) _builder.Append(',');
                first = false;
                _builder.Append(constraint);
            }
        }
    }
    
    /// <summary> Increases the indentation level, updating the indentation string properly. </summary>
    private void IncreaseLevel()
    {
        _level++;
        _indent.Append(Tab);
    }
    
    /// <summary> Decreases the indentation level, updating the indentation string properly. </summary>
    private void DecreaseLevel()
    {
        _level--;
        _indent.Remove(_indent.Length - Tab.Length, Tab.Length);
    }
    
    /// <summary>
    /// Opens a '{' block.
    /// </summary>
    public void Open()
    {
        InitLine();
        _builder.Append('{');
        IncreaseLevel();
    }
    
    /// <summary>
    /// Appends a namespace and opens a new block.<br/>
    /// namespace '<see cref="name"/>'<br/>{
    /// </summary>
    public void OpenNamespace(string name)
    {
        PushLine("namespace ");
        _builder.Append(name);
        Open();
    }

    /// <summary>
    /// Appends a partial class signature and opens a new block.<br/>
    /// partial class '<see cref="name"/>'<br/>{
    /// </summary>
    public void OpenPartialClass(string name, bool isStruct = false, params string[] bases)
    {
        PushLine("partial ");
        Push(isStruct ? "struct " : "class ");
        _builder.Append(name);
        if (bases.Length > 0)
        {
            _builder.Append(" : ");
            for (int i = 0; i < bases.Length; i++)
            {
                if (i > 0) _builder.Append(", ");
                _builder.Append(bases[i]);
            }
        }
        Open();   
    }
    
    /// <summary>
    /// Appends a full method signature and opens a new block.<br/>
    /// '<see cref="signature"/>' '<see cref="methodName"/>' ('<see cref="args"/>')<br/>{
    /// </summary>
    /// <example>
    /// signature = "public partial void"; methodName = "Foo", args = ["string Bar"]<br/>
    /// <b>Result:</b><br/>
    /// public partial void Foo(string Bar)<br/>{
    /// </example>
    public void OpenMethod(string signature, string methodName, params string[] args)
    {
        InitLine();
        PushMethodSignature(signature, methodName, args);
        Open();
    }

    /// <summary>
    /// Appends a full generic method signature and opens a new block.<br/>
    /// '<see cref="signature"/>' '<see cref="methodName"/>'&lt;T1, T2, ...&gt;('<see cref="args"/>') where T1 : x where T2 : y <br/>{ 
    /// </summary>
    /// <example>
    /// signature = "public partial void"; methodName = "Foo", genericArgs = [{"T1", "struct"}] args = ["string Bar"]<br/>
    /// <b>Result:</b><br/>
    /// public partial void Foo&lt;T1&gt;(string Bar) where T1: struct<br/>{
    /// </example>
    public void OpenGenericMethod(string signature, string methodName, ICollection<GenericArgDef> genericArgs, params string[] args)
    {
        InitLine();
        PushGenericMethodSignature(signature, methodName, genericArgs, args);
        Open();
    }

    /// <summary>
    /// Opens a switch block on the variable <see cref="varName"/>. <br/>
    /// <c> switch (varName) { </c>
    /// </summary>
    /// <param name="varName"></param>
    public void OpenSwitch(string varName)
    {
        PushLine($"switch ({varName})");
        Open();
    }

    /// <summary>
    /// Opens a case within a switch block with the value <see cref="value"/>. <br/>
    /// <c>case value: {</c>
    /// </summary>
    /// <param name="value"></param>
    public void OpenSwitchCase(string value)
    {
        PushLine("case ");
        Push(value);
        Push(':');
        Open();
    }

    /// <summary>
    /// Pushes a break instruction and closes a switch case block. <br/>
    /// <c>break; }</c>
    /// </summary>
    public void CloseSwitchCase()
    {
        PushLine("break;");
        Close();
    }

    /// <summary>
    /// Closes a block with '}'.
    /// </summary>
    public void Close()
    {
        DecreaseLevel();
        InitLine();
        _builder.Append('}');
    }
    
    /// <summary>
    /// Initializes a builder with the namespaces and nested classes specified.
    /// Does not support arity for generic classes.
    /// </summary>
    public static ClassStringBuilder FromFullMetadataName(string metadataName)
    {
        ClassStringBuilder builder = new();
        int lastNamespaceDot = metadataName.LastIndexOf('.');
        if (lastNamespaceDot >= 0)
        {
            foreach (var ns in metadataName.Substring(0, lastNamespaceDot).Split('.'))
                if (ns.Length > 0)
                    builder.OpenNamespace(ns);
            metadataName = metadataName.Substring(lastNamespaceDot + 1);
        }

        foreach (var className in metadataName.Split('+'))
            if (className.Length > 0)
                builder.OpenPartialClass(className);
        
        return builder;
    }
    

    /// <summary>
    /// Creates a new <see cref="ClassStringBuilder"/> from a <see cref="TypeRecord"/>, 
    /// opening the namespaces and the class specified in the record.
    /// </summary>
    public static ClassStringBuilder FromTypeRecord(TypeRecord typeRecord, bool isStruct = false, params string[] bases)
    {
        return new(typeRecord.Namespaces.Reverse(), typeRecord.TypeName, isStruct, bases);
    }
    
    /// <summary>
    /// Represents a generic argument, optionally constrained to base types.
    /// </summary>
    public readonly struct GenericArgDef
    {
        public readonly string Arg;
        public readonly string[] Constraints;

        public GenericArgDef(string arg, string[] constraints)
        {
            Arg = arg;
            Constraints = constraints;
        }
        
        public static implicit operator GenericArgDef(string arg) => new(arg, Array.Empty<string>());
    }
}