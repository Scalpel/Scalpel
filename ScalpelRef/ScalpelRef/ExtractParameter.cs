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

            var rewriter = new ExtractParameterRewriter();

            // Return the new solution with the now-uppercase type name.
            return rewriter.Visit(document.Project.Solution, document, typeDecl, methodDecl, semanticModel);
        }

        private class ExtractParameterRewriter : CSharpSyntaxRewriter
        {
            private MethodDeclarationSyntax methodDecl;
            private LiteralExpressionSyntax typeDecl;
            private SemanticModel semanticModel;
            private TypeInfo typeSymbol;

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
                    (SyntaxFactory.Parameter(SyntaxFactory.Identifier("MyParameter").WithAdditionalAnnotations(RenameAnnotation.Create()))
                        .WithType(SyntaxFactory.ParseTypeName(GetPredefinedTypeOrType(typeSymbol.ConvertedType.Name)))
                        .WithDefault(SyntaxFactory.EqualsValueClause(typeDecl)));
                return node.WithParameterList(node.ParameterList.WithParameters(parameters))
                    .WithBody((BlockSyntax)Visit(node.Body));
            }

            public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                if (node != typeDecl)
                    return base.VisitLiteralExpression(node);

                return SyntaxFactory.IdentifierName("MyParameter");
            }

            private string GetPredefinedTypeOrType(string v)
            {
                switch (v)
                {
                    case "Boolean":
                        return "bool";
                    case "Byte":
                        return "byte";
                    case "SByte":
                        return "sbyte";
                    case "Char":
                        return "char";
                    case "Decimal":
                        return "decimal";
                    case "Double":
                        return "double";
                    case "Single":
                        return "float";
                    case "Int32":
                        return "int";
                    case "Int64":
                        return "long";
                    case "UInt32":
                        return "uint";
                    case "UInt64":
                        return "ulong";
                    case "Int16":
                        return "short";
                    case "UInt16":
                        return "ushort";
                    case "String":
                        return "string";
                    default:
                        return v;
                }
            }

        }
    }
}