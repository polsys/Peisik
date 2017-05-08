using System;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik
{
    internal class CompilationDiagnostic
    {
        /// <summary>
        /// Gets the token that caused the error, if one has been specified.
        /// </summary>
        public string AssociatedToken { get; private set; }

        /// <summary>
        /// Gets the diagnostic code.
        /// </summary>
        public DiagnosticCode Diagnostic { get; private set; }

        /// <summary>
        /// Gets a string about the expected value, if one has been specified.
        /// </summary>
        public string Expected { get; private set; }

        /// <summary>
        /// If true, this was caused by an error that aborted the compilation.
        /// </summary>
        public bool IsError { get; private set; }

        /// <summary>
        /// Gets the associated position.
        /// </summary>
        public TokenPosition Position { get; private set; }

        public CompilationDiagnostic(DiagnosticCode code, bool isFatal, string token, string expected, TokenPosition position)
        {
            AssociatedToken = token;
            Diagnostic = code;
            Expected = expected;
            IsError = isFatal;
            Position = position;
        }

        public override string ToString()
        {
            return $"(L{Position.LineNumber}:{Position.Column}) {Diagnostic}('{AssociatedToken}')";
        }

        public string GetDescription()
        {
            switch (Diagnostic)
            {
                // Parser errors
                case DiagnosticCode.ExpectedBegin:
                    return $"Expected 'begin', but received '{AssociatedToken}'.";
                case DiagnosticCode.ExpectedEndOfLine:
                    return $"Expected end of line, but received '{AssociatedToken}'";
                case DiagnosticCode.ExpectedEndOfParameterList:
                    return $"Invalid parameter definition '{AssociatedToken}'.";
                case DiagnosticCode.ExpectedImportConstOrFunction:
                    return $"Expected import, constant or function, but received '{AssociatedToken}'.";
                case DiagnosticCode.ExpectedName:
                    return $"Expected name, but received '{AssociatedToken}'.";
                case DiagnosticCode.ExpectedStatement:
                    return $"Expected statement, but received '{AssociatedToken}'.";
                case DiagnosticCode.ExpectedTypeName:
                    return $"Expected type name, but received '{AssociatedToken}'.";
                case DiagnosticCode.InvalidBoolFormat:
                    return $"Invalid boolean format '{AssociatedToken}'.";
                case DiagnosticCode.InvalidIntFormat:
                    return $"Invalid integer format '{AssociatedToken}'.";
                case DiagnosticCode.InvalidRealFormat:
                    return $"Invalid real number format '{AssociatedToken}'.";
                case DiagnosticCode.InvalidName:
                    return $"Invalid name '{AssociatedToken}'.";
                case DiagnosticCode.ModuleNotImported:
                    return $"Module referenced in '{AssociatedToken}' was not imported before the use.";
                case DiagnosticCode.NameAlreadyDefined:
                    return $"Name '{AssociatedToken}' is already defined.";
                case DiagnosticCode.UnexpectedEndOfFile:
                    return $"Unexpected end of file.";
                case DiagnosticCode.VoidMayOnlyBeUsedForReturn:
                    return $"Void may only be used as a return type.";
                // Parser warnings
                case DiagnosticCode.ModuleAlreadyImported:
                    return $"The module '{AssociatedToken}' is already imported.";
                // Compiler errors
                case DiagnosticCode.MainMayNotHaveParameters:
                    return $"The main function may not have parameters.";
                case DiagnosticCode.MayNotAssignToConst:
                    return $"Trying to assign to constant '{AssociatedToken}'.";
                case DiagnosticCode.NameIsPrivate:
                    return $"The name '{AssociatedToken}' is private and may not be accessed from other modules.";
                case DiagnosticCode.NameNotFound:
                    return $"The name '{AssociatedToken}' does not exist.";
                case DiagnosticCode.NoMainFunction:
                    return $"There was no main function.";
                case DiagnosticCode.NotEnoughParameters:
                    return $"Expected {Expected} parameters, but received {AssociatedToken}.";
                case DiagnosticCode.ParamsMustBeSameType:
                    return $"The parameters must have the same type. Expected {Expected} but received {AssociatedToken}.";
                case DiagnosticCode.ReturnNotGuaranteed:
                    return $"Could not guarantee that the function {AssociatedToken} returns.";
                case DiagnosticCode.TooManyParameters:
                    return $"Expected {Expected} parameters, but received {AssociatedToken}.";
                case DiagnosticCode.WrongType:
                    return $"Expected {Expected}, but received {AssociatedToken}.";
                // Compiler warnings
                case DiagnosticCode.UnreachableCode:
                    return $"This line of code is unreachable.";
                default:
                    return $"{Diagnostic}({AssociatedToken})";
            }
        }
    }

    internal enum DiagnosticCode
    {
        // The error codes are volatile as long as Peisik is not documented
        // These could and maybe should be organized more categorically
        Unspecified,
        // Parser errors
        ExpectedBegin,
        ExpectedEndOfLine,
        ExpectedEndOfParameterList,
        ExpectedImportConstOrFunction,
        ExpectedName,
        ExpectedStatement,
        ExpectedTypeName,
        InvalidBoolFormat,
        InvalidIntFormat,
        InvalidRealFormat,
        InvalidName,
        ModuleNotImported,
        NameAlreadyDefined,
        UnexpectedEndOfFile,
        VoidMayOnlyBeUsedForReturn,
        // Parser warnings
        ModuleAlreadyImported,
        // Compiler errors
        MainMayNotHaveParameters,
        MayNotAssignToConst,
        NameIsPrivate,
        NameNotFound,
        NoMainFunction,
        NotEnoughParameters,
        ParamsMustBeSameType,
        ReturnNotGuaranteed,
        TooManyParameters,
        WrongType,
        // Compiler warnings
        UnreachableCode,
    }
}
