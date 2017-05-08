namespace Polsys.Peisik.Parser
{
    internal class IdentifierSyntax : ExpressionSyntax
    {
        public string Name { get; private set; }

        public IdentifierSyntax(TokenPosition position, string name)
            : base(position)
        {
            Name = name;
        }
    }
}
