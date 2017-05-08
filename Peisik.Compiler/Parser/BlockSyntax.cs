using System;
using System.Collections.Generic;

namespace Polsys.Peisik.Parser
{
    internal class BlockSyntax : SyntaxNode
    {
        public List<StatementSyntax> Statements { get; private set; }

        public BlockSyntax(TokenPosition position)
            : base(position)
        {
            Statements = new List<StatementSyntax>();
        }

        public void AddStatement(StatementSyntax statement)
        {
            Statements.Add(statement);
        }
    }
}
