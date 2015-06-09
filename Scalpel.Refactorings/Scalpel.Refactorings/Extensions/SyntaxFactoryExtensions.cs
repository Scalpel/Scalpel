using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scalpel
{
    public static class ScalpelFactory
    {
        public static TypeSyntax PrefefinedTypeOrType(string v)
        {
            return SyntaxFactory.ParseTypeName(Translate(v));
        }

        private static string Translate(string v)
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
