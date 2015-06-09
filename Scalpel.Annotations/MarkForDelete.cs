using Microsoft.CodeAnalysis;

namespace Scalpel
{
    internal static class MarkForDelete 
    {
        public const string Kind = "CodeAction_MarkForDelete";

        public static SyntaxAnnotation Create()
        {
            return new SyntaxAnnotation(Kind);
        }

    }
}