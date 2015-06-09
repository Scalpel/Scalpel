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

namespace Scalpel
{
    //when the only thing an overload does is introducing an optional parameter, merge them
    //also, the opposite: split methods with optionals into one that calls the other
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MergeOptionalOverloads)), Shared]
    internal class MergeOptionalOverloads : CodeRefactoringProvider
    {
        public sealed async override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            return;
        }
    }
}