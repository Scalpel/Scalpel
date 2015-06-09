using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeActions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace Scalpel.Test
{
    [TestClass]
    public class ExtractParameterTests : RefactoringVerifier
    {
        [TestMethod]
        public void WhenLiteralString_RefactoringShouldAddCodeAction()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {
        string Method1()
        {
            return ""out"";
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("out"));

            var refactoringProvider = new ExtractParameterProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            Assert.AreEqual(1, context.CodeActions.Count);
        }

        [TestMethod]
        public void WhenEligible_AddAsOptionalParameter()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {
        string Method1()
        {
            return ""out"";
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("out"));

            var refactoringProvider = new ExtractParameterProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            var action = context.CodeActions.First();
            var changeDocument = GetModifiedText(action).Result;

            Assert.AreEqual(file
                .Replace("string Method1()", "string Method1(string MyParameter = \"out\")")
                .Replace("return \"out\"", "return MyParameter")
                , changeDocument);
        }

        [TestMethod]
        public void WhenTwoConstants_BorderShouldBeInclusive()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {
        private void Method1()
        {
            var a = 1 + 2;
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("2"));

            var refactoringProvider = new ExtractParameterProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            var action = context.CodeActions.First();
            var changeDocument = GetModifiedText(action).Result;

            Assert.AreEqual(file
                .Replace("void Method1()", "void Method1(int MyParameter = 2)")
                .Replace("var a = 1 + 2", "var a = 1 + MyParameter")
                , changeDocument);
        }

        [TestMethod]
        public void WhenPrivateVariable_RefactoringShouldNotAddCodeAction()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {
        string Method1()
        {
            int a = 0;
            return a;
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("a;") - 1);

            var refactoringProvider = new ExtractParameterProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            Assert.AreEqual(0, context.CodeActions.Count);
        }

        [TestMethod]
        public void WhenLiteralIsAssignedToConstant_ThenItCannotBeExtracted()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {
        private void Method1()
        {
            const string a = ""out"";
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("out"));

            var refactoringProvider = new ExtractParameterProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            Assert.AreEqual(0, context.CodeActions.Count);
        }


        [TestMethod]
        public void WhenLiteralIsFieldInitializer_ThenItCannotBeExtracted()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {
        private string a = ""out"";

        private void Method1()
        {
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("out"));

            var refactoringProvider = new ExtractParameterProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            Assert.AreEqual(0, context.CodeActions.Count);
        }

        [TestMethod]
        public void WhenLiteralIsPartOfAProperty_ThenItCannotBeExtracted()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {
        public string OutProperty 
        {
            get
            {
                return ""out"";
            }
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("out"));

            var refactoringProvider = new ExtractParameterProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            Assert.AreEqual(0, context.CodeActions.Count);
        }

        [TestMethod]
        public void WhenLiteralIsAlreadyAParameter_ThenItCannotBeExtracted()
        {
            var cts = new CancellationTokenSource();
            var file = @"
namespace ClassLibrary1
{
    public class Class1
    {
        private void Method1(string a = ""out"")
        {
        }
    }
}
";
            var context = CreateContext(file, file.IndexOf("out"));

            var refactoringProvider = new ExtractParameterProvider();
            refactoringProvider.ComputeRefactoringsAsync(context.Context).Wait();

            Assert.AreEqual(0, context.CodeActions.Count);
        }
    }
}
