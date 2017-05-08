using System;

namespace Polsys.Peisik.Parser
{
    internal class VariableDeclarationSyntax : StatementSyntax
    {
        public string Name { get; private set; }
        public PrimitiveType Type { get; private set; }

        public ExpressionSyntax InitialValue { get; set; }
        
        public VariableDeclarationSyntax(TokenPosition position, PrimitiveType type, string name)
            : base(position)
        {
            Name = name;
            Type = type;
        }
    }
}
