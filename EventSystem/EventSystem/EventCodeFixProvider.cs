using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventSystem;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EventCodeFixProvider))]
public class EventCodeFixProvider : CodeFixProvider
{
    private const string MakePartialFixTitle = "Add partial modifier";
    
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Diagnostics.TargetTypeNotPartialId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var fixes = ctx.Diagnostics
            .Select(d => (GetFix(ctx, d, root), d))
            .OfType<(CodeAction Fix, Diagnostic Diagnostic)>();

        foreach (var (fix, diagnostic) in fixes)
        {
            ctx.RegisterCodeFix(fix, diagnostic);
        }
    }

    private static CodeAction? GetFix(CodeFixContext ctx, Diagnostic diagnostic, SyntaxNode root) =>
        diagnostic.Id switch
        {
            Diagnostics.TargetTypeNotPartialId => FixPartialType(ctx, diagnostic, root),
            _ => null
        };

    private static CodeAction? FixPartialType(CodeFixContext ctx, Diagnostic diagnostic, SyntaxNode root)
    {
        var type = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent
            ?.AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();
        if (type == null) return null;


        return CodeAction.Create(
            title: MakePartialFixTitle,
            createChangedDocument: ct => MakePartialAsync(ctx.Document, type, ct),
            equivalenceKey: MakePartialFixTitle
        );
    }
    
    private static async Task<Document> MakePartialAsync(Document document, TypeDeclarationSyntax type, CancellationToken ct)
    {
        var newTypeDeclaration = type.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

        var oldRoot = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        var newRoot = oldRoot?.ReplaceNode(type, newTypeDeclaration);
        return newRoot != null 
            ? document.WithSyntaxRoot(newRoot) 
            : document;
    }
}