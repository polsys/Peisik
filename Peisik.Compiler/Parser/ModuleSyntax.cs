using System;
using System.Collections.Generic;

namespace Polsys.Peisik.Parser
{
    internal class ModuleSyntax : SyntaxNode
    {
        public List<ConstantSyntax> Constants { get; private set; }

        public List<FunctionSyntax> Functions { get; private set; }

        public List<string> ModuleDependencies { get; private set; }

        public string ModuleName { get; set; }

        public ModuleSyntax(TokenPosition position) 
            : base(position)
        {
            Constants = new List<ConstantSyntax>();
            Functions = new List<FunctionSyntax>();
            ModuleDependencies = new List<string>();
            ModuleName = "";
        }
    }
}
