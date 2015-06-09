using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Scalpel
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(InlineLambdaProvider)), Shared]
    internal class InlineLambdaProvider : CodeRefactoringProvider
    {
        public async sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            var argument = node.DescendantNodesAndSelf().OfType<ArgumentSyntax>()?
                .FirstOrDefault(l => l.Span.ContainsInclusive(context.Span.Start));
            if (argument == null)
                return;

            var semantic = await context.Document.GetSemanticModelAsync();
            var symbol = semantic.GetSymbolInfo(argument.Expression);

            IMethodSymbol method = symbol.Symbol as IMethodSymbol;
            if (symbol.CandidateSymbols.Count() == 1)
                method = symbol.CandidateSymbols.First() as IMethodSymbol;

            if (method == null)
                return;

            var syntax = method.DeclaringSyntaxReferences.First()?.GetSyntax() as MethodDeclarationSyntax;
            if (syntax == null || method.DeclaringSyntaxReferences.Count() > 1)
                return;

            var action = new AnnotatedCodeAction("Inline Lambda", c => InlineLambda(context.Document, argument, syntax));
            context.RegisterRefactoring(action);

        }

        private async Task<Solution> InlineLambda(Document document, ArgumentSyntax argument, MethodDeclarationSyntax method)
        {
            var rewriter = new InlineLambdaRewriter();
            return await rewriter.Visit(document.Project.Solution, document, argument, method);
        }

        private class InlineLambdaRewriter : CSharpSyntaxRewriter
        {
            private ArgumentSyntax argument;
            private MethodDeclarationSyntax method;

            public async Task<Solution> Visit(Solution solution, Document document, ArgumentSyntax argument, MethodDeclarationSyntax method)
            {
                this.argument = argument;
                this.method = method;

                var root = await document.GetSyntaxRootAsync();
                var newRoot = this.Visit(root);
                return solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node != method)
                    return base.VisitMethodDeclaration(node);

                return base.VisitMethodDeclaration(node).WithAdditionalAnnotations(MarkForDelete.Create());
            }

            public override SyntaxNode VisitArgument(ArgumentSyntax node)
            {
                if (node != argument)
                    return base.VisitArgument(node);

                return node.WithExpression(
                    SyntaxFactory.ParenthesizedLambdaExpression(
                        SimplifyList(method.ParameterList), 
                        SimplifySyntax(method.Body)));
            }

            private CSharpSyntaxNode SimplifySyntax(BlockSyntax body)
            {
                if (body.Statements.Count == 1 && body.Statements.First() is ReturnStatementSyntax)
                    return ((ReturnStatementSyntax)body.Statements.First()).Expression;

                return body;
            }

            private ParameterListSyntax SimplifyList(ParameterListSyntax parameters)
            {
                var list = SyntaxFactory.ParameterList();
                var newParameters = list.Parameters;
                foreach (var parameter in parameters.Parameters)
                {
                    newParameters = newParameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.Identifier.Text)));
                }
                return list.WithParameters(newParameters);
            }
        }
    }
}