using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Shared.Tests;

public static class TestUtils
{
    private static string Preprocess(string src) => src
        .Split('\n')
        .Select(l => l.Trim())
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .DefaultIfEmpty("")
        .Aggregate((a, b) => a + "\n" + b);

    
    /// <summary>
    /// Compares two source texts after preprocessing (trims each line, removes empty lines, and joins with newline).
    /// </summary>
    public static void AssertEqual(string expected, SyntaxTree generated)
    {
        Assert.Equal(Preprocess(expected), Preprocess(generated.GetText().ToString()));

        /*Assert.Equal(expected.Trim(), generated.GetText().ToString().Trim(),
            ignoreLineEndingDifferences: true,
            ignoreWhiteSpaceDifferences: true);*/
    }
    
    /*public static GeneratorDriverRunResult RunGenerator(ISourceGenerator generator, string source)
    {
        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // We need to create a compilation with the required source code.
        var compilation = CSharpCompilation.Create(generator.GetType().ToString(),
            [CSharpSyntaxTree.ParseText(source)],
            [
                // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);

        // Run generators and retrieve all results.
        return driver.RunGenerators(compilation).GetRunResult();
    }*/
}