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
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            var literal = node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>()?
                .FirstOrDefault(l => l.Span.ContainsInclusive(context.Span.Start));
            if (literal == null)
                return;

            var containingMethod = GetContainingMethod(literal);
            if (containingMethod == null)
                return;

            if (!CanBeExtracted(literal))
                return;

            var action = CodeAction.Create("Extract Parameter", c => ExtractParameter(context.Document, literal, containingMethod, c));
            context.RegisterRefactoring(action);
        }

        private bool CanBeExtracted(LiteralExpressionSyntax literal)
        {
            return (!literal.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>()?.FirstOrDefault()?.IsConst ?? true)
                && (!literal.AncestorsAndSelf().OfType<ParameterSyntax>()?.Any() ?? true);
        }

        private MethodDeclarationSyntax GetContainingMethod(SyntaxNode literal)
        {
            return literal.FirstAncestorOrSelf<MethodDeclarationSyntax>(t => t is MethodDeclarationSyntax);
        }

        private async Task<Solution> ExtractParameter(Document document, LiteralExpressionSyntax literal, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var rewriter = new ExtractParameterRewriter();
            return rewriter.Visit(document.Project.Solution, document, literal, methodDecl, semanticModel);
        }

        private class ExtractParameterRewriter : CSharpSyntaxRewriter
        {
            private MethodDeclarationSyntax methodDecl;
            private LiteralExpressionSyntax typeDecl;
            private SemanticModel semanticModel;
            private TypeInfo typeSymbol;

            private string ParameterName = "MyParameter";

            public Solution Visit(Solution solution, Document document, LiteralExpressionSyntax typeDecl, MethodDeclarationSyntax methodDecl, SemanticModel semanticModel)
            {
                this.semanticModel = semanticModel;
                this.typeDecl = typeDecl;
                this.methodDecl = methodDecl;

                this.typeSymbol = semanticModel.GetTypeInfo(typeDecl);

                var root = this.Visit(document.GetSyntaxRootAsync().Result);
                return solution.WithDocumentSyntaxRoot(document.Id, root);
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node != methodDecl)
                    return base.VisitMethodDeclaration(node);
                
                var parameters = methodDecl.ParameterList.Parameters.Add
                    (SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName).WithAdditionalAnnotations(RenameAnnotation.Create()))
                        .WithType(ScalpelFactory.PrefefinedTypeOrType(typeSymbol.ConvertedType.Name))
                        .WithDefault(SyntaxFactory.EqualsValueClause(typeDecl)));
                return node.WithParameterList(node.ParameterList.WithParameters(parameters))
                    .WithBody((BlockSyntax)Visit(node.Body));
            }

            public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                if (node != typeDecl)
                    return base.VisitLiteralExpression(node);

                return SyntaxFactory.IdentifierName(ParameterName);
            }
        }
    }
}