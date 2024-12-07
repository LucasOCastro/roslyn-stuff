using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<EventSystem.EventAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EventSystem.Tests;

using Utilities = Shared.Tests.AnalyzerTester<EventAnalyzer, EventCodeFixProvider>;

public class EventAnalyzerTests
{
    [Fact]
    public async Task WorkingEvent_Good()
    {
        await Utilities.VerifyAnalyzerAsync($@"
[Generators.Event(typeof(GoodHandler))]
public partial struct GoodEvent
{{
    public float X {{ get; set; }}
}}

[Generators.EventHandler]
public partial class GoodHandler{{}}

{Attributes.AttributesSourceCode}
");
    }
    
    [Fact]
    public async Task NonPartialEvent_Fix()
    {
        const string source = $@"
[Generators.Event(typeof(GoodHandler))]
{{|{Diagnostics.TargetTypeNotPartialId}:public struct BadEvent|}}
{{
    public float X {{ get; set; }}
}}

[Generators.EventHandler]
public partial class GoodHandler{{}}

{Attributes.AttributesSourceCode}
";
        const string fix = $@"
[Generators.Event(typeof(GoodHandler))]
public partial struct BadEvent
{{
    public float X {{ get; set; }}
}}

[Generators.EventHandler]
public partial class GoodHandler{{}}

{Attributes.AttributesSourceCode}
";
        
        await Utilities.VerifyCodeFixAsync(source, fix);
    }
    
    [Fact]
    public async Task NonPartialHandler_Bad()
    {
        DiagnosticResult[] expected =
        [
            Verify.Diagnostic(Diagnostics.TargetTypeNotPartial)
                .WithLocation(3, 1)
                .WithArguments(["BadHandler", "Generators.EventHandlerAttribute"])
        ];
        await Utilities.VerifyAnalyzerAsync($@"
[Generators.EventHandler]
public class BadHandler{{}}

{Attributes.AttributesSourceCode}
", expected);
    }
    
    [Fact]
    public async Task NullHandler_Bad()
    {
        DiagnosticResult[] expected =
        [
            Verify.Diagnostic("GDFEV003").WithLocation(2, 2)
        ];
        await Utilities.VerifyAnalyzerAsync($@"
[Generators.Event(null)]
public partial struct BadEvent
{{
    public float X {{ get; set; }}
}}

{Attributes.AttributesSourceCode}
", expected);
    }
    
    
    [Fact]
    public async Task HandlerWithoutAttribute_Bad()
    {
        DiagnosticResult[] expected =
        [
            Verify.Diagnostic("GDFEV002").WithLocation(2, 2)
        ];
        await Utilities.VerifyAnalyzerAsync($@"
[Generators.Event(typeof(NotHandler))]
public partial struct BadEvent
{{
    public float X {{ get; set; }}
}}

public class NotHandler{{}}

{Attributes.AttributesSourceCode}
", expected);
    }
}