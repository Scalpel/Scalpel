using Microsoft.CodeAnalysis;

namespace ScalpelRef
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