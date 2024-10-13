using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Shared;

public readonly record struct DiagnosticRecord(DiagnosticDescriptor Descriptor, 
    LocationRecord? Location, 
    EquatableArray<string> MessageArgs)
{
    public DiagnosticRecord(DiagnosticDescriptor descriptor, Location? location, params string[] messageArgs) :
        this(descriptor, location != null ? LocationRecord.CreateFrom(location) : null, new(messageArgs))
    {
    }

    public Diagnostic ToDiagnostic() =>
        Diagnostic.Create(Descriptor, Location?.ToLocation(), MessageArgs.OfType<object?>().ToArray());

    /// <summary>
    /// Reports the diagnostic and verifies if it should stop execution.
    /// </summary>
    /// <returns><c>true</c> if it should stop execution.</returns>
    public bool Report(SourceProductionContext spc)
    {
        var diagnostic = ToDiagnostic();
        spc.ReportDiagnostic(diagnostic);
        return diagnostic.Severity == DiagnosticSeverity.Error;
    }

    /// <summary>
    /// Reports the diagnostics and verifies if any should stop execution.
    /// </summary>
    /// <returns><c>true</c> if any should stop execution.</returns>
    public static bool ReportMany(IEnumerable<DiagnosticRecord> diagnostics, SourceProductionContext spc)
    {
        bool shouldStop = false;
        
        foreach (var diagnostic in diagnostics)
            if (diagnostic.Report(spc))
                shouldStop = true;

        return shouldStop;
    }
}