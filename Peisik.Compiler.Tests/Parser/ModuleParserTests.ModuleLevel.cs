using System;
using NUnit.Framework;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Tests.Parser
{
    partial class ModuleParserTests : CompilerTestBase
    {
        [Test]
        public void EmptyModule()
        {
            var syntaxTree = ParseStringWithoutDiagnostics("");

            Assert.That(syntaxTree, Is.Not.Null);
            Assert.That(syntaxTree.Position.Filename, Is.EqualTo("Filename"));
            Assert.That(syntaxTree.Position.LineNumber, Is.EqualTo(0));
            Assert.That(syntaxTree.Position.Column, Is.EqualTo(0));
        }

        [Test]
        public void CaseInsensitive()
        {
            var source = @"PUBLIC INT PEOPLE 3
PRIVATE BOOL HAPPY TRUE

IMPORT HAPPINESS

PRIVATE REAL MAIN()
BEGIN
  WHILE HAPPY
  BEGIN
    IF <(PEOPLE, 3)
    BEGIN
      RETURN 3.0
    END
    ELSE
    BEGIN
      RETURN 4.0
    END
  END
END";

            // Just make sure there are no errors
            var syntaxTree = ParseStringWithoutDiagnostics(source);
        }

        [Test]
        public void ConstantDeclaration_PrivateInt()
        {
            var module = ParseStringWithoutDiagnostics("private int Pi 3");

            Assert.That(module.Constants, Is.Not.Null);
            Assert.That(module.Constants, Has.Exactly(1).Items);
            Assert.That(module.Constants[0].Name, Is.EqualTo("Pi"));
            Assert.That(module.Constants[0].Visibility, Is.EqualTo(Visibility.Private));
            Assert.That(module.Constants[0].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(module.Constants[0].GetIntValue(), Is.EqualTo(3));

            Assert.That(module.Constants[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(module.Constants[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(module.Constants[0].Position.Column, Is.EqualTo(1));
        }

        [Test]
        public void ConstantDeclaration_PublicBool()
        {
            var module = ParseStringWithoutDiagnostics("public bool IsTrue true");

            Assert.That(module.Constants, Is.Not.Null);
            Assert.That(module.Constants, Has.Exactly(1).Items);
            Assert.That(module.Constants[0].Name, Is.EqualTo("IsTrue"));
            Assert.That(module.Constants[0].Visibility, Is.EqualTo(Visibility.Public));
            Assert.That(module.Constants[0].Type, Is.EqualTo(PrimitiveType.Bool));
            Assert.That(module.Constants[0].GetBoolValue(), Is.EqualTo(true));

            Assert.That(module.Constants[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(module.Constants[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(module.Constants[0].Position.Column, Is.EqualTo(1));
        }

        [TestCase("private int A -1", -1, true)]
        [TestCase("private int A -9223372036854775808", -9223372036854775808, true)]
        [TestCase("private int A 0", 0, true)]
        [TestCase("private int A 9223372036854775807", 9223372036854775807, true)]
        // Overflow cases
        [TestCase("private int A -9223372036854775809", 0, false)]
        [TestCase("private int A 9223372036854775808", 0, false)]
        // Invalid stuff
        [TestCase("private int A 0xDEAD", 0, false)]
        public void ConstantDeclaration_Integers(string source, long value, bool shouldPass)
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            if (shouldPass)
            {
                Assert.That(diagnostics, Is.Empty);
                Assert.That(module.Constants, Has.Exactly(1).Items);
                Assert.That(module.Constants[0].Name, Is.EqualTo("A"));
                Assert.That(module.Constants[0].Type, Is.EqualTo(PrimitiveType.Int));
                Assert.That(module.Constants[0].GetIntValue(), Is.EqualTo(value));
            }
            else
            {
                Assert.That(module, Is.Null);
                Assert.That(diagnostics, Has.Exactly(1).Items);
                Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidIntFormat));
                Assert.That(diagnostics[0].IsError, Is.True);
                Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
                Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
                Assert.That(diagnostics[0].Position.Column, Is.EqualTo(15));
            }
        }

        [Test]
        public void ConstantDeclaration_InvalidBool()
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics("public bool Invalid ture");

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidBoolFormat));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(21));
        }


        [TestCase("private real A -1", -1, true)]
        [TestCase("private real A -1234.5", -1234.5, true)]
        [TestCase("private real A 0", 0, true)]
        [TestCase("private real A 922337", 922337, true)]
        [TestCase("private real A 2.0e5", 2.0e5, true)]
        // Invalid cases
        [TestCase("private real A 0xDEAD", 0, false)]
        [TestCase("private real A 0.0_1", 0, false)]
        public void ConstantDeclaration_Reals(string source, double value, bool shouldPass)
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            if (shouldPass)
            {
                Assert.That(diagnostics, Is.Empty);
                Assert.That(module.Constants, Has.Exactly(1).Items);
                Assert.That(module.Constants[0].Name, Is.EqualTo("A"));
                Assert.That(module.Constants[0].Type, Is.EqualTo(PrimitiveType.Real));
                Assert.That(module.Constants[0].GetRealValue(), Is.EqualTo(value).Within(0.00001));
            }
            else
            {
                Assert.That(module, Is.Null);
                Assert.That(diagnostics, Has.Exactly(1).Items);
                Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidRealFormat));
                Assert.That(diagnostics[0].IsError, Is.True);
                Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
                Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
                Assert.That(diagnostics[0].Position.Column, Is.EqualTo(16));
            }
        }

        [Test]
        public void ConstantDeclaration_Void_Fails()
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics("public void C");

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.VoidMayOnlyBeUsedForReturn));
        }

        [Test]
        public void ConstantDeclaration_WithComment()
        {
            Assert.That(() => ParseStringWithoutDiagnostics("public bool HelloAll true # Greet everybody"), Throws.Nothing);
        }

        [Test]
        public void ConstantDeclaration_MultipleDeclarations()
        {
            var module = ParseStringWithoutDiagnostics("public int People 4 # A small party\n" +
                "public bool Handshakes false # An informal party");

            Assert.That(module.Constants, Has.Exactly(2).Items);
            Assert.That(module.Constants[0].Name, Is.EqualTo("People"));
            Assert.That(module.Constants[1].Name, Is.EqualTo("Handshakes"));
        }

        [Test]
        public void ConstantDeclaration_UnexpectedEof()
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics("public int");

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.UnexpectedEndOfFile));
        }

        [Test]
        public void ConstantDeclaration_EndOfLineRequired()
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics("public int People 4 public bool Handshakes false");

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ExpectedEndOfLine));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(21));
        }

        [Test]
        public void ConstantDeclaration_MultiLine()
        {
            Assert.That(() => ParseStringWithoutDiagnostics("public bool\n  HelloAll true"), Throws.Nothing);
        }

        [Test]
        public void ConstantDeclaration_MultiLine_EndOfLineRequired()
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics("public int\n People 4 public bool Handshakes false");

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ExpectedEndOfLine));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(2));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(11));
        }

        [TestCase("public bool true true")]
        [TestCase("public bool public true")]
        [TestCase("public bool math.true true")]
        [TestCase("public bool 1 true")]
        [TestCase("public bool - true")]
        [TestCase("public bool a-b true")]
        public void ConstantDeclaration_InvalidNames(string source)
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(13));
        }

        [TestCase("public bool 1SmallStep true")]
        [TestCase("public bool ___ true")]
        [TestCase("public bool âllo true")]
        public void ConstantDeclaration_ValidNames(string source)
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Not.Null);
            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void InvalidVisibilityModifier_Fails()
        {
            (var module, var diagnostics) = ParseStringWithDiagnostics("invalid int Boo 0");

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ExpectedImportConstOrFunction));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(1));
        }

        [Test]
        public void Function_PublicIntWith2Params()
        {
            // Syntactically valid, semantically incorrect
            var source = @"public int DoNothing(int a, bool b)
begin
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module, Is.Not.Null);
            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].Name, Is.EqualTo("DoNothing"));
            Assert.That(module.Functions[0].ReturnType, Is.EqualTo(PrimitiveType.Int));
            Assert.That(module.Functions[0].Visibility, Is.EqualTo(Visibility.Public));

            Assert.That(module.Functions[0].Parameters, Has.Exactly(2).Items);
            Assert.That(module.Functions[0].Parameters[0].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(module.Functions[0].Parameters[0].Name, Is.EqualTo("a"));
            Assert.That(module.Functions[0].Parameters[1].Type, Is.EqualTo(PrimitiveType.Bool));
            Assert.That(module.Functions[0].Parameters[1].Name, Is.EqualTo("b"));

            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
        }

        [Test]
        public void Function_VoidParam()
        {
            var source = @"private void Stupid(void a)
begin
end";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.VoidMayOnlyBeUsedForReturn));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(21));
        }

        [Test]
        public void Function_VoidWithStatement()
        {
            var source = @"private void Stupid()
begin
  int a 0
end";

            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module, Is.Not.Null);
            Assert.That(module.Functions, Has.Exactly(1).Items);
            Assert.That(module.Functions[0].Name, Is.EqualTo("Stupid"));
            Assert.That(module.Functions[0].ReturnType, Is.EqualTo(PrimitiveType.Void));
            Assert.That(module.Functions[0].Visibility, Is.EqualTo(Visibility.Private));
            Assert.That(module.Functions[0].Parameters, Has.Exactly(0).Items);
            Assert.That(module.Functions[0].CodeBlock, Is.Not.Null);
        }

        [Test]
        public void Function_EndOfLineAfter()
        {
            var source = @"public int A()
begin
  return 4
end public int B()
begin
  return 3
end";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ExpectedEndOfLine));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(5));
        }

        [Test]
        public void Function_InvalidReturnType()
        {
            var source = @"public whatever A()
begin
  return 4
end";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ExpectedTypeName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(8));
        }

        [Test]
        public void Import_SingleModule()
        {
            var source = @"import ModuleName
";
            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.ModuleDependencies, Has.Exactly(1).Items);
            Assert.That(module.ModuleDependencies[0], Is.EqualTo("modulename"));
        }

        [Test]
        public void Import_DottedName()
        {
            var source = @"import Module.Submodule
";
            var module = ParseStringWithoutDiagnostics(source);

            Assert.That(module.ModuleDependencies, Has.Exactly(1).Items);
            Assert.That(module.ModuleDependencies[0], Is.EqualTo("module.submodule"));
        }

        [Test]
        public void Import_InvalidName()
        {
            var source = @"import @What
";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.InvalidName));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(8));
        }

        [Test]
        public void Import_MakesFullNameAvailable_Positive()
        {
            var source = @"import Module
private int Main()
begin
  return Module.Number
end";
            var module = ParseStringWithoutDiagnostics(source);
        }

        [Test]
        public void Import_MakesFullNameAvailable_Negative()
        {
            var source = @"private int Main()
begin
  return Module.Number
end

import Module";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ModuleNotImported));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Import_NoEndOfLine()
        {
            var source = @"import modulename anothermodule";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ExpectedEndOfLine));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(19));
        }

        [Test]
        public void Import_WarnIfImportedTwice()
        {
            var source = @"import modulename
import anothermodule
import modulename
";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Not.Null);
            Assert.That(module.ModuleDependencies, Has.Exactly(2).Items);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].IsError, Is.False);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ModuleAlreadyImported));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(1));
        }

        [Test]
        public void NameAlreadyDefined_Constant()
        {
            var source = @"private int Pi 3
public int PI 4";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameAlreadyDefined));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(2));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(12));
        }

        [Test]
        public void NameAlreadyDefined_Function()
        {
            var source = @"private int Pi 3
public int PI()
begin
  return 4
end";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameAlreadyDefined));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(2));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(12));
        }

        [Test]
        public void NameAlreadyDefined_GlobalFunction()
        {
            var source = @"public void Print()
begin
end";
            (var module, var diagnostics) = ParseStringWithDiagnostics(source);

            Assert.That(module, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameAlreadyDefined));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(13));
        }

        [Test]
        public void NameDefinedInMultipleFunctions_Succeeds()
        {
            var source = @"
public int A(int param)
begin
  return 1
end

public void Main()
begin
  int param 100
  A(param)
end";
            // Make sure it compiles without errors
            var module = ParseStringWithoutDiagnostics(source);
        }
    }
}
