using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScalpelRef.Test
{
    public abstract class RefactoringVerifier
    {

        internal VerifiableCodeRefactoringContext CreateContext(string document, int position)
        {
            return new VerifiableCodeRefactoringContext(GetDocument(document), new TextSpan(position, 1));
        }

        protected Document GetDocument(string file)
        {
            return CreateProject(new[] { file }).Documents.First();
        }

        protected async Task<string> GetModifiedText(CodeAction action)
        {
            var cts = new CancellationTokenSource();
            var operation = await action.GetOperationsAsync(cts.Token);

            var changeDocument = operation.OfType<ApplyChangesOperation>().First().ChangedSolution.Projects.First().Documents.First();

            var text = await changeDocument.GetTextAsync();
            return text.ToString();
        }

        private static Project CreateProject(string[] sources, string language = LanguageNames.CSharp)
        {
            var projectId = ProjectId.CreateNewId(Guid.NewGuid().ToString());

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "testProject", "testProject", language)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference);

            int count = 0;
            foreach (var source in sources)
            {
                var newFileName = "test" + count + ".cs";
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }
            return solution.GetProject(projectId);
        }


        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromAssembly(typeof(Enumerable).Assembly);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromAssembly(typeof(CSharpCompilation).Assembly);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromAssembly(typeof(Compilation).Assembly);


    }
}
