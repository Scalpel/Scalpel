﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scalpel.Test
{
    [TestClass]
    public class InlineLambdaTests : RefactoringVerifier
    {
        [TestMethod]
        public void WhenParameterIsMethod_ReplaceWithLambda()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {

        public int StrategyReplace(Func<int, int, int> action)
        {
            return action(1, 2);
        }

        public int Adder(int a, int b)
        {
            return a + b;
        }

        public void Method1()
        {
            var c = StrategyReplace(Adder);
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("(Adder)") + 1);

            var refactoringProvider = new InlineLambdaProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            Assert.AreEqual(1, context.CodeActions.Count, "CodeAction not found");

            var action = context.CodeActions.First();
            var changeDocument = GetModifiedDocument(action).Result;

            Assert.AreEqual(file
                .Replace("StrategyReplace(Adder);", "StrategyReplace((a, b) => a + b);")
                , changeDocument.GetTextAsync().Result.ToString(), "Replacement not done");

            var syntax = changeDocument.GetSyntaxRootAsync().Result;
            var node = syntax.FindNode(new TextSpan(file.IndexOf("int Adder"), 1)).AncestorsAndSelf().OfType<MethodDeclarationSyntax>()?.First();

            Assert.IsTrue(node.ContainsAnnotations, "Original method not marked for deletion");
        }

        //test parameter names, test complex expressions; add formatting annotation to lambda
    }
}
