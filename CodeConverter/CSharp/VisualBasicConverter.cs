using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using ICSharpCode.CodeConverter.VB;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class VisualBasicConverter
    {
        public static async Task<SyntaxNode> ConvertCompilationTreeAsync(Document document,
            CSharpCompilation csharpViewOfVbSymbols, Project csharpReferenceProject,
            OptionalOperations optionalOperations, CancellationToken cancellationToken)
        {
            document = await document.WithExpandedRootAsync(cancellationToken);
            var root = await document.GetSyntaxRootAsync(cancellationToken) as VBSyntax.CompilationUnitSyntax ??
                       throw new InvalidOperationException(NullRootError(document));

            var compilation = await document.Project.GetCompilationAsync(cancellationToken);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);


            var csSyntaxGenerator = SyntaxGenerator.GetGenerator(csharpReferenceProject);
            var semanticModel = compilation.GetSemanticModel(tree, true);
            var visualBasicSyntaxVisitor = new
                DeclarationNodeVisitor(document, compilation, semanticModel, csharpViewOfVbSymbols, csSyntaxGenerator);
            var converted = (CSS.CompilationUnitSyntax)await root.AcceptAsync(visualBasicSyntaxVisitor.TriviaConvertingDeclarationVisitor);

            return optionalOperations.MapSourceTriviaToTargetHandled(root, converted, document);
        }

        private static string NullRootError(Document document)
        {
            string initial = document.Project.Language != LanguageNames.VisualBasic
                ? "Document cannot be converted because it's not within a VB project."
                : "Could not find valid VB within document.";
            return initial + " For best results, convert a VB document from within a VB project which compiles successfully.";
        }
    }
}
