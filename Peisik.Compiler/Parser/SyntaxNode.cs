using System;

namespace Polsys.Peisik.Parser
{
    /// <summary>
    /// Base class for all syntax tree nodes.
    /// </summary>
    internal abstract class SyntaxNode
    {
        /// <summary>
        /// Gets the source code position of this node.
        /// </summary>
        public TokenPosition Position
        {
            get;
            private set;
        }

        public SyntaxNode(TokenPosition position)
        {
            Position = position;
        }
    }
}
