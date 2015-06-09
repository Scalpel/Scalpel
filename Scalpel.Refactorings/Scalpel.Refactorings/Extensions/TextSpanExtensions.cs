using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scalpel
{
    public static class TextSpanExtensions
    {
        public static bool ContainsInclusive(this TextSpan span, int position)
        {
            return span.Contains(position) || span.Contains(position - 1);
        }
    }
}
