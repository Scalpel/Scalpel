using Microsoft.CodeAnalysis.CodeActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Threading;

namespace Scalpel
{
    public class AnnotatedCodeAction : CodeAction
    {
        private Func<CancellationToken, Task<Solution>> createChangedSolution;

        public override string Title { get; }

        public AnnotatedCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
        {
            Title = title;
            this.createChangedSolution = createChangedSolution;
        }

        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return createChangedSolution(cancellationToken);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            var baseOperations = await base.ComputeOperationsAsync(cancellationToken);
            if (baseOperations == null)
                return null;

            var annotatedOperations = new List<CodeActionOperation>(baseOperations);

            return annotatedOperations;
        }
    }

}
