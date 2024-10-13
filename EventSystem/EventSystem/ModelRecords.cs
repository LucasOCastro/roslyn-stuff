using System.Collections.Generic;
using System.Linq;
using Shared;

namespace EventSystem;

internal readonly record struct EventHandlerRecord(
    TypeRecord Type,
    EquatableArray<EventStructRecord> Events,
    DiagnosticRecord? Diagnostic)
{
    public IEnumerable<DiagnosticRecord> AllDiagnostics =>
        Events
            .Select(e => e.Diagnostic)
            .Append(Diagnostic)
            .OfType<DiagnosticRecord>();
}

internal readonly record struct EventStructRecord(
    TypeRecord Type,
    TypeRecord HandlerType,
    DiagnosticRecord? Diagnostic);