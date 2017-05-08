using System;
using System.Collections.Generic;

namespace Polsys.Peisik.Parser
{
    internal class FunctionSyntax : SyntaxNode
    {
        public string Name { get; private set; }
        public PrimitiveType ReturnType { get; private set; }
        public Visibility Visibility { get; private set; }

        public BlockSyntax CodeBlock { get; private set; }
        public List<VariableDeclarationSyntax> Parameters { get; private set; }

        public FunctionSyntax(TokenPosition position, PrimitiveType returnType, Visibility visibility, string name)
            : base(position)
        {
            Name = name;
            ReturnType = returnType;
            Visibility = visibility;

            Parameters = new List<VariableDeclarationSyntax>();
        }

        public void AddParameter(VariableDeclarationSyntax variable)
        {
            Parameters.Add(variable);
        }

        public void SetBlock(BlockSyntax block)
        {
            CodeBlock = block;
        }
    }
}
