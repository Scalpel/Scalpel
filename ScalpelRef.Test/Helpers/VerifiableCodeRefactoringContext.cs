using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scalpel.Test
{
    internal class VerifiableCodeRefactoringContext 
    {
        private CancellationTokenSource cts;

        public CodeRefactoringContext Context { get; private set; }
        public IList<CodeAction> CodeActions { get; }

        public VerifiableCodeRefactoringContext(Document document, TextSpan textSpan)
        {
            cts = new CancellationTokenSource();
            CodeActions = new List<CodeAction>();
            Context = new CodeRefactoringContext(document, textSpan, AddCodeAction, cts.Token);
        }

        private void AddCodeAction(CodeAction a)
        {
            CodeActions.Add(a);
        }
    }
}
