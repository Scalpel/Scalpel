using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace ScalpelRef
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ScalpelRefCodeRefactoringProvider)), Shared]
    internal class ScalpelRefCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // TODO: Replace the following code with your own analysis, generating a CodeAction for each refactoring to offer

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a type declaration node.
            var typeDecl = node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>()?.FirstOrDefault();
            if (typeDecl == null)
                return;

            var containingMethod = GetContainingMethod(typeDecl);
            if (containingMethod == null)
                return;

            // For any type declaration node, create a code action to reverse the identifier text.
            var action = CodeAction.Create("Extract Parameter", c => ExtractParameter(context.Document, typeDecl, containingMethod, c));

            // Register this code action.
            context.RegisterRefactoring(action);
        }

        private MethodDeclarationSyntax GetContainingMethod(SyntaxNode typeDecl)
        {
            return typeDecl.FirstAncestorOrSelf<MethodDeclarationSyntax>(t => t is MethodDeclarationSyntax);
        }

        //if override or interface implementation, then make optional defaulting to current
        private async Task<Solution> ExtractParameter(Document document, LiteralExpressionSyntax typeDecl, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken);

            

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = originalSolution;// await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}