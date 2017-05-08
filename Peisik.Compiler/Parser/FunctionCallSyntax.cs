using System;
using System.Collections.Generic;

namespace Polsys.Peisik.Parser
{
    internal class FunctionCallSyntax : ExpressionSyntax
    {
        public string FunctionName { get; private set; }

        public List<ExpressionSyntax> Parameters { get; private set; }
        
        public FunctionCallSyntax(TokenPosition position, string targetName)
            : base(position)
        {
            FunctionName = targetName;
            Parameters = new List<ExpressionSyntax>();
        }

        public void AddParameter(ExpressionSyntax expression)
        {
            Parameters.Add(expression);
        }
    }
}
