namespace Polsys.Peisik
{
    /// <summary>
    /// Represents the source code position of a token.
    /// </summary>
    internal struct TokenPosition
    {
        /// <summary>
        /// Gets the line column where the token starts.
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// Gets the source code file name containing the token.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Gets the line number where the token starts.
        /// </summary>
        public int LineNumber { get; private set; }

        public TokenPosition(string filename, int line, int column)
        {
            Column = column;
            Filename = filename;
            LineNumber = line;
        }
    }
}
