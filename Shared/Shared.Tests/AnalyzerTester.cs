using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Shared.Tests;

public static class AnalyzerTester<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static async Task VerifyAnalyzerAsync(string source) => await VerifyAnalyzerAsync(source, []);
    
    public static async Task VerifyAnalyzerAsync(string source, DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            TestBehaviors = TestBehaviors.SkipSuppressionCheck
        };
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    public static Task VerifyCodeFixAsync(string source, string fix) => VerifyCodeFixAsync(source, fix, []);
    
    public static async Task VerifyCodeFixAsync(string source, string fix, DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fix,
            TestBehaviors = TestBehaviors.SkipSuppressionCheck
        };
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
}