using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Polsys.Peisik.Parser
{
    internal class ModuleParser
    {
        private List<CompilationDiagnostic> Diagnostics = new List<CompilationDiagnostic>();

        private TextReader _source;
        private string _nextToken;

        private string _filename;
        private int _tokenStartColumn = 0;
        private int _tokenStartLine = 0;
        private int _currentColumn = 1;
        private int _currentLine = 1;
        private int _lastLine = 1;
        private HashSet<string> _seenGlobals = new HashSet<string>();
        private HashSet<string> _importedModules = new HashSet<string>();

        private const string KeywordBegin = "begin";
        private const string KeywordBool = "bool";
        private const string KeywordElse = "else";
        private const string KeywordEnd = "end";
        private const string KeywordIf = "if";
        private const string KeywordImport = "import";
        private const string KeywordInt = "int";
        private const string KeywordPrivate = "private";
        private const string KeywordPublic = "public";
        private const string KeywordReal = "real";
        private const string KeywordReturn = "return";
        private const string KeywordVoid = "void";
        private const string KeywordWhile = "while";
        private readonly List<string> _keywords = new List<string>()
        {
            KeywordBegin, KeywordBool, "false", KeywordElse, KeywordEnd, KeywordInt, KeywordIf, KeywordImport,
            KeywordPrivate, KeywordPublic, KeywordReal, KeywordReturn, "true", KeywordVoid, KeywordWhile,
            // Reserved for future
            "enum", "for", "foreach", "string", "struct", "type"
        };
        private readonly List<string> _globalFunctions = new List<string>() {
            "+", "-", "*", "/", "//", "%", "==", "!=", "<", "<=", ">", ">=",
            "and", "or", "not", "xor", "print",
        };
        private readonly List<string> _languageNamespaces = new List<string>()
        {
            "math"
        };
        private const string SingleCharTokens = "(),";

        private TokenPosition GetCurrentPosition()
        {
            return new TokenPosition(_filename, _tokenStartLine, _tokenStartColumn);
        }

        private void AdvanceToNextToken()
        {
            var sb = new StringBuilder();
            
            while (_source.Peek() != -1)
            {
                if (SingleCharTokens.Contains(((char)_source.Peek()).ToString()))
                {
                    // Special handling for separating two tokens with no whitespace between them
                    if (sb.Length > 0)
                        break;
                    else
                    {
                        sb.Append((char)_source.Read());
                        _currentColumn++;
                        break;
                    }
                }

                var ch = (char)_source.Read();
                _currentColumn++;

                if (ch == '\r')
                {
                    // Only care about LF
                    continue;
                }
                else if (ch == '\n')
                {
                    _currentColumn = 1;
                    _currentLine++;

                    if (sb.Length > 0)
                        break;
                }
                else if (char.IsWhiteSpace(ch))
                {
                    // Do not stop because of leading whitespace
                    if (sb.Length == 0)
                        continue;
                    else
                        break;
                }
                else if (ch == '#')
                {
                    // Comment: ignore the rest of the line
                    var ignoredCh = _source.Read();
                    while (ignoredCh != -1 && ignoredCh != '\n')
                    {
                        ignoredCh = _source.Read();
                    }
                    _currentColumn = 1;
                    _currentLine++;
                }
                else
                {
                    // Update the source mapping at the start of the token
                    if (sb.Length == 0)
                    {
                        _tokenStartColumn = _currentColumn - 1; // -1 because column was already advanced above
                        _tokenStartLine = _currentLine;
                    }

                    sb.Append(ch);
                }
            }

            _nextToken = sb.Length > 0 ? sb.ToString() : null;
        }

        private string PeekToken()
        {
            return _nextToken;
        }

        private (string token, TokenPosition position) ReadToken()
        {
            if (_nextToken == null)
                LogError(DiagnosticCode.UnexpectedEndOfFile, "", GetCurrentPosition());

            var result = (_nextToken, GetCurrentPosition());
            _lastLine = _tokenStartLine;
            AdvanceToNextToken();

            return result;
        }

        private void AssertEndOfLine()
        {
            // Throw if the next token starts on the same line as the last one
            if (_nextToken != null && _tokenStartLine == _lastLine)
                LogError(DiagnosticCode.ExpectedEndOfLine, PeekToken(), GetCurrentPosition());
        }

        private void AssertValidNameDefinition(string name, TokenPosition position, bool shouldAddGlobal)
        {
            // locals may be null

            if (string.IsNullOrWhiteSpace(name))
                LogError(DiagnosticCode.ExpectedName, name, position);

            bool containsLetter = false;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsLetter(name[i]) || name[i] == '_')
                {
                    containsLetter = true;
                }
                else if (char.IsDigit(name[i]))
                {
                    // Let decimal digits pass as long as there is at least one letter
                }
                else
                {
                    LogError(DiagnosticCode.InvalidName, name, position);
                }
            }

            string nameInLower = name.ToLowerInvariant();
            if (!containsLetter || _keywords.Contains(nameInLower))
                LogError(DiagnosticCode.InvalidName, name, position);

            // If the name is already defined as a global, fail
            // However, local name checking is left to the semantic compiler, because it depends on block state
            if (_seenGlobals.Contains(nameInLower) || _globalFunctions.Contains(nameInLower))
            {
                LogError(DiagnosticCode.NameAlreadyDefined, name, position);
            }
            else
            {
                if (shouldAddGlobal)
                    _seenGlobals.Add(nameInLower);
            }
        }

        private void AssertValidNameUse(string name, TokenPosition position, bool isNamespaceName = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                LogError(DiagnosticCode.ExpectedName, name, position);

            if (_globalFunctions.Contains(name))
                return;

            bool containsLetter = false;
            bool isFullName = false;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsLetter(name[i]) || name[i] == '_')
                {
                    containsLetter = true;
                }
                else if (char.IsDigit(name[i]))
                {
                    // Let decimal digits pass as long as there is at least one letter
                }
                else if (name[i] == '.')
                {
                    // . separates namespaces
                    // To disallow cases like Math. or .Something, check the part so far and
                    // reset the character encountered flag
                    if (!containsLetter)
                        LogError(DiagnosticCode.InvalidName, name, position);
                    containsLetter = false;
                    isFullName = true;
                }
                else
                {
                    LogError(DiagnosticCode.InvalidName, name, position);
                }
            }

            if (!containsLetter || _keywords.Contains(name.ToLowerInvariant()))
                LogError(DiagnosticCode.InvalidName, name, position);

            // Check that the namespace has been imported (unless in an import statement)
            // There has to be some special casing for Math.XXX etc.
            if (isFullName && !isNamespaceName)
            {
                var modulePart = name.ToLowerInvariant().Substring(0, name.LastIndexOf('.'));
                if (!_importedModules.Contains(modulePart) && !_languageNamespaces.Contains(modulePart))
                    LogError(DiagnosticCode.ModuleNotImported, name, position);
            }
        }

        private void LogError(DiagnosticCode error, string token, TokenPosition position)
        {
            Diagnostics.Add(new CompilationDiagnostic(error, true, token, "", position));
            throw new CompilerException();
        }

        private void LogWarning(DiagnosticCode error, string token, TokenPosition position)
        {
            Diagnostics.Add(new CompilationDiagnostic(error, false, token, "", position));
        }

        private ModuleSyntax ParseModule()
        {
            try
            {
                var position = GetCurrentPosition();
                var module = new ModuleSyntax(position);

                while (PeekToken() != null)
                {
                    ParseModuleLevelStatement(module);
                }

                return module;
            }
            catch (CompilerException)
            {
                // The error has already been logged
                return null;
            }
        }

        private void ParseModuleLevelStatement(ModuleSyntax module)
        {
            (var firstToken, var firstPosition) = ReadToken();
            var visibility = Visibility.Unspecified;
            switch (firstToken.ToLowerInvariant())
            {
                case KeywordImport:
                    {
                        (var originalModuleName, var moduleNamePosition) = ReadToken();
                        var moduleName = originalModuleName.ToLowerInvariant();
                        AssertValidNameUse(moduleName, moduleNamePosition, true);

                        if (module.ModuleDependencies.Contains(moduleName))
                        {
                            LogWarning(DiagnosticCode.ModuleAlreadyImported, originalModuleName, firstPosition);
                        }
                        else
                        {
                            module.ModuleDependencies.Add(moduleName);
                            _importedModules.Add(moduleName);
                        }
                        AssertEndOfLine();
                        return;
                    }
                case KeywordPrivate:
                    visibility = Visibility.Private;
                    break;
                case KeywordPublic:
                    visibility = Visibility.Public;
                    break;
                default:
                    LogError(DiagnosticCode.ExpectedImportConstOrFunction, firstToken, firstPosition);
                    break;
            }

            var type = ParseTypeName();
            (var name, var namePosition) = ReadToken();
            AssertValidNameDefinition(name, namePosition, true);

            if (PeekToken() == "(")
            {
                // Function
                var function = new FunctionSyntax(firstPosition, type, visibility, name);
                ParseParameterDeclarations(function);
                AssertEndOfLine();
                function.SetBlock(ParseBlock());
                module.Functions.Add(function);
            }
            else
            {
                // Constant
                switch (type)
                {
                    case PrimitiveType.Bool:
                        module.Constants.Add(ConstantSyntax.CreateBoolConstant(name, ParseBool(), visibility, firstPosition));
                        break;
                    case PrimitiveType.Int:
                        module.Constants.Add(ConstantSyntax.CreateIntConstant(name, ParseInt(), visibility, firstPosition));
                        break;
                    case PrimitiveType.Real:
                        module.Constants.Add(ConstantSyntax.CreateRealConstant(name, ParseReal(), visibility, firstPosition));
                        break;
                    case PrimitiveType.Void:
                        LogError(DiagnosticCode.VoidMayOnlyBeUsedForReturn, firstToken, firstPosition);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                AssertEndOfLine();
            }
        }

        private void ParseParameterDeclarations(FunctionSyntax function)
        {
            if (ReadToken().token != "(")
                throw new InvalidOperationException("Not within a parameter list.");

            // Early out if there are no parameters at all
            if (PeekToken() == ")")
            {
                ReadToken();
                return;
            }

            while (PeekToken() != null)
            {
                var position = GetCurrentPosition();

                var typeToken = PeekToken();
                var type = ParseTypeName();
                if (type == PrimitiveType.Void)
                    LogError(DiagnosticCode.VoidMayOnlyBeUsedForReturn, typeToken, position);

                (var name, var namePosition) = ReadToken();
                AssertValidNameDefinition(name, namePosition, false);

                function.AddParameter(new VariableDeclarationSyntax(position, type, name));

                (var nextToken, var nextPosition) = ReadToken();
                if (nextToken == ",")
                {
                    continue;
                }
                else if (nextToken == ")")
                {
                    break;
                }
                else
                {
                    LogError(DiagnosticCode.ExpectedEndOfParameterList, nextToken, position);
                }
            }
        }

        private BlockSyntax ParseBlock()
        {
            (var beginToken, var beginPosition) = ReadToken();
            if (beginToken.ToLowerInvariant() != KeywordBegin)
                LogError(DiagnosticCode.ExpectedBegin, beginToken, beginPosition);

            var block = new BlockSyntax(beginPosition);

            while (PeekToken()?.ToLowerInvariant() != KeywordEnd)
            {
                block.AddStatement(ParseStatement());
            }

            ReadToken(); // end
            AssertEndOfLine();
            return block;
        }

        private StatementSyntax ParseStatement()
        {
            (var firstToken, var firstPosition) = ReadToken();
            var tokenInLower = firstToken.ToLowerInvariant();

            if (tokenInLower == KeywordReturn)
            {
                if (PeekToken()?.ToLowerInvariant() == KeywordVoid)
                {
                    // Void return
                    ReadToken();

                    AssertEndOfLine();
                    return new ReturnSyntax(firstPosition, null);
                }
                else
                {
                    // Return expression
                    var expr = ParseExpression();

                    AssertEndOfLine();
                    return new ReturnSyntax(firstPosition, expr);
                }
            }
            else if (tokenInLower == KeywordIf)
            {
                var condition = ParseExpression();
                AssertEndOfLine();
                var thenBlock = ParseBlock();

                // The 'else' block is optional
                BlockSyntax elseBlock = null;
                if (PeekToken()?.ToLowerInvariant() == KeywordElse)
                {
                    ReadToken(); // else
                    elseBlock = ParseBlock();
                }
                else
                    elseBlock = new BlockSyntax(firstPosition);

                return new IfSyntax(firstPosition, condition, thenBlock, elseBlock);
            }
            else if (tokenInLower == KeywordWhile)
            {
                var condition = ParseExpression();
                AssertEndOfLine();
                var block = ParseBlock();

                return new WhileSyntax(firstPosition, condition, block);
            }
            else if (PrimitiveTypeFromTypeName(firstToken) != PrimitiveType.NoType)
            {
                // Variable declaration
                var type = PrimitiveTypeFromTypeName(firstToken);
                if (type == PrimitiveType.Void)
                    LogError(DiagnosticCode.VoidMayOnlyBeUsedForReturn, firstToken, firstPosition);

                (var name, var namePosition) = ReadToken();
                AssertValidNameDefinition(name, namePosition, false);

                var variableDecl = new VariableDeclarationSyntax(firstPosition, type, name) 
                {
                    InitialValue = ParseExpression()
                };
                AssertEndOfLine();
                return variableDecl;
            }
            else if (PeekToken() == "=")
            {
                // Assignment
                AssertValidNameUse(firstToken, firstPosition);
                ReadToken(); // Assignment operator

                var expression = ParseExpression();
                AssertEndOfLine();

                return new AssignmentSyntax(firstPosition, expression, firstToken);
            }
            else if (PeekToken() == "(")
            {
                // Function call
                AssertValidNameUse(firstToken, firstPosition);
                ReadToken(); // Open paren

                var call = new FunctionCallSyntax(firstPosition, firstToken);
                ParseFunctionCallParameterList(call);
                AssertEndOfLine();
                return new FunctionCallStatementSyntax(firstPosition, call);
            }
            else
            {
                (var token, var position) = ReadToken();
                LogError(DiagnosticCode.ExpectedStatement, token, position);
                return null; // Not reached
            }
        }

        private ExpressionSyntax ParseExpression()
        {
            (var token, var position) = ReadToken();

            if (long.TryParse(token, out var intValue))
            {
                return LiteralSyntax.CreateIntLiteral(position, intValue);
            }
            else if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var realValue))
            {
                return LiteralSyntax.CreateRealLiteral(position, realValue);
            }
            else if (bool.TryParse(token, out var boolValue))
            {
                return LiteralSyntax.CreateBoolLiteral(position, boolValue);
            }
            else
            {
                AssertValidNameUse(token, position);

                if (PeekToken() == "(")
                {
                    // A function call
                    ReadToken();
                    var call = new FunctionCallSyntax(position, token);
                    ParseFunctionCallParameterList(call);

                    return call;
                }
                else
                {
                    // A constant or variable
                    return new IdentifierSyntax(position, token);
                }
            }
        }

        private void ParseFunctionCallParameterList(FunctionCallSyntax call)
        {
            if (PeekToken() == ")")
            {
                ReadToken();
            }
            else
            {
                while (PeekToken() != null)
                {
                    call.AddParameter(ParseExpression());

                    if (PeekToken() == ",")
                    {
                        ReadToken();
                        continue;
                    }
                    else if (PeekToken() == ")")
                    {
                        ReadToken();
                        break;
                    }
                }
            }
        }

        private bool ParseBool()
        {
            (var token, var position) = ReadToken();
            if (bool.TryParse(token, out var value))
            {
                return value;
            }
            else
            {
                LogError(DiagnosticCode.InvalidBoolFormat, token, position);
                return false; // Not executed
            }
        }

        private long ParseInt()
        {
            (var token, var position) = ReadToken();
            if (long.TryParse(token, out var value))
            {
                return value;
            }
            else
            {
                LogError(DiagnosticCode.InvalidIntFormat, token, position);
                return 0; // Not executed
            }
        }

        private double ParseReal()
        {
            (var token, var position) = ReadToken();
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            else
            {
                LogError(DiagnosticCode.InvalidRealFormat, token, position);
                return 0; // Not executed
            }
        }

        private PrimitiveType ParseTypeName()
        {
            (var typeToken, var position) = ReadToken();
            var result = PrimitiveTypeFromTypeName(typeToken);
            
            if (result == PrimitiveType.NoType)
                LogError(DiagnosticCode.ExpectedTypeName, typeToken, position);
            return result;
        }

        private PrimitiveType PrimitiveTypeFromTypeName(string typeToken)
        {
            switch (typeToken.ToLowerInvariant())
            {
                case KeywordBool:
                    return PrimitiveType.Bool;
                case KeywordInt:
                    return PrimitiveType.Int;
                case KeywordReal:
                    return PrimitiveType.Real;
                case KeywordVoid:
                    return PrimitiveType.Void;
                default:
                    return PrimitiveType.NoType;
            }
        }

        #region Constructors and static methods

        private ModuleParser(TextReader source, string sourceName)
        {
            _source = source;
            _filename = sourceName;

            // Read the initial token
            AdvanceToNextToken();
        }

        public static (ModuleSyntax module, List<CompilationDiagnostic> diagnostics)
            Parse(TextReader source, string sourceName, string moduleName)
        {
            var parser = new ModuleParser(source, sourceName);
            var module = parser.ParseModule();
            if (module != null)
                module.ModuleName = moduleName.ToLowerInvariant();

            return (module, parser.Diagnostics);
        }

        #endregion
    }
}
