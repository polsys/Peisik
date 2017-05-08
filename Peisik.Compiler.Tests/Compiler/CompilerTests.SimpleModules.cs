using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Tests.Compiler
{
    partial class CompilerTests : CompilerTestBase
    {
        [Test]
        public void Assignment_Positive()
        {
            var source = @"private void Main()
begin
  int a 3
  a = 4
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [1 locals]
PushConst   $literal_3
PopLocal    a
PushConst   $literal_4
PopLocal    a
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void Assignment_WrongType()
        {
            var source = @"private void Main()
begin
  int a 3
  a = true
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(7));
        }

        [Test]
        public void Assignment_BeforeDeclaration()
        {
            var source = @"private void Main()
begin
  a = 4
  int a 3
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void Assignment_ToConstFails()
        {
            var source = @"private real Pi 3.14

private void Main()
begin
  Pi = 4
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.MayNotAssignToConst));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(5));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(3));
        }

        [Test]
        public void EmptyModule_RaisesError()
        {
            var source = "";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NoMainFunction));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(0));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(0));
        }

        [Test]
        public void EmptyMain()
        {
            var source = @"private void Main()
begin
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(program.MainFunctionIndex, Is.EqualTo(0));
            Assert.That(program.Constants, Is.Empty);
            Assert.That(program.Functions, Has.Exactly(1).Items);
            Assert.That(program.Functions[0].FullName, Is.EqualTo("main"));
            Assert.That(program.Functions[0].FunctionTableIndex, Is.EqualTo(0));
            Assert.That(program.Functions[0].ReturnType, Is.EqualTo(PrimitiveType.Void));
            Assert.That(program.Functions[0].ParameterTypes, Is.Empty);
            Assert.That(program.Functions[0].Bytecode, Has.Exactly(1).Items);
            Assert.That(program.Functions[0].Bytecode[0].Opcode, Is.EqualTo(Opcode.Return));
        }

        [Test]
        public void FunctionCall_NameNotFound()
        {
            var source = @"private int Main()
begin
  return Ufo()
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Ufo"));
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void FunctionCall_IntPromotedToRealInParam()
        {
            var source = @"private void F(real a)
begin
end

private void Main()
begin
  F(1)
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [0 locals]
PushConst   $literal_1r
Call        f
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void FunctionCall_StandAlone()
        {
            var source = @"private int F()
begin
  return 2
end

private void Main()
begin
  F()
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [0 locals]
Call        f
PopDiscard
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void FunctionCall_StandAloneVoid()
        {
            var source = @"private void F()
begin
end

private void Main()
begin
  F()
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Void main() [0 locals]
Call        f
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void FunctionCall_Internal_2Param()
        {
            var source = @"private int Main()
begin
  return +(1, 99)
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            var disasm = @"Int main() [0 locals]
PushConst   $literal_1
PushConst   $literal_99
CallI2      Plus
Return";
            VerifyDisassembly(program.Functions[0], program, disasm);
        }

        [Test]
        public void FunctionCall_Internal_2Param_MustHaveSameType()
        {
            var source = @"private bool Main()
begin
  return ==(1, true)
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.ParamsMustBeSameType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(13));
        }

        [Test]
        public void FunctionCall_Internal_2Param_WrongType()
        {
            var source = @"private int Main()
begin
  return +(1, true)
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int|Real"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(12));
        }

        [Test]
        public void FunctionCall_Internal_2Param_WrongType2()
        {
            var source = @"private int Main()
begin
  return +(true, false)
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int|Real"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(12));
        }

        [Test]
        public void FunctionCall_Internal_2Param_WrongType3()
        {
            var source = @"private int Main()
begin
  return not(2.3)
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Real"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool|Int"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(14));
        }

        [Test]
        public void FunctionCall_Internal_2Param_WrongReturn()
        {
            var source = @"private bool Main()
begin
  return +(1, 2)
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Int"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void FunctionDecl_MainMustNotHaveArgs()
        {
            var source = @"private void Main(bool a)
begin
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.MainMayNotHaveParameters));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(1));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(1));
        }

        [Test]
        public void Import_FindsPublicItems()
        {
            var moduleSource = @"public real PI 3.14
public bool IsHappy(real a)
begin
  return true
end";
            var source = @"import Mod
public bool Main()
begin
  return Mod.IsHappy(Mod.Pi)
end";

            var compiler = new SemanticCompiler(new List<ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(moduleSource), "Module", "Mod").module,
                ModuleParser.Parse(new StringReader(source), "Source", "").module
            });
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(diagnostics, Is.Empty);
            Assert.That(program.Constants[1].FullName, Is.EqualTo("mod.pi"));
        }
        
        [Test]
        public void Import_PrivateVisibleInOwnModule()
        {
            var moduleSource = @"private real PI 3.14
private bool CheckHappiness(real a)
begin
  return true
end

public bool IsHappy()
begin
  return CheckHappiness(PI)
end";
            var source = @"import Mod
public bool Main()
begin
  return Mod.IsHappy()
end";

            var compiler = new SemanticCompiler(new List<ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(moduleSource), "Module", "Mod").module,
                ModuleParser.Parse(new StringReader(source), "Source", "").module
            });
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public void Import_DoesNotFindPrivates_Const()
        {
            var moduleSource = @"private bool IsHappy true";
            var source = @"import Mod
public bool Main()
begin
  return Mod.IsHappy
end";

            var compiler = new SemanticCompiler(new List<ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(moduleSource), "Module", "Mod").module,
                ModuleParser.Parse(new StringReader(source), "Source", "").module
            });
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameIsPrivate));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Import_DoesNotFindPrivates_Function()
        {
            var moduleSource = @"private bool IsHappy()
begin
  return true
end";
            var source = @"import Mod
public bool Main()
begin
  return Mod.IsHappy()
end";

            var compiler = new SemanticCompiler(new List<ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(moduleSource), "Module", "Mod").module,
                ModuleParser.Parse(new StringReader(source), "Source", "").module
            });
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameIsPrivate));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Import_AnythingFromMainNotVisible_Const()
        {
            var moduleSource = @"private bool IsHappy()
begin
  return ShouldBeHappy
end";
            var source = @"import Mod
public bool ShouldBeHappy true

public bool Main()
begin
  return Mod.IsHappy()
end";

            var compiler = new SemanticCompiler(new List<ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(moduleSource), "Module", "Mod").module,
                ModuleParser.Parse(new StringReader(source), "Source", "").module
            });
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Import_AnythingFromMainNotVisible_Function()
        {
            var moduleSource = @"private bool IsHappy()
begin
  return ShouldBeHappy()
end";
            var source = @"import Mod
public bool ShouldBeHappy()
begin
  return true
end

public bool Main()
begin
  return Mod.IsHappy()
end";

            var compiler = new SemanticCompiler(new List<ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(moduleSource), "Module", "Mod").module,
                ModuleParser.Parse(new StringReader(source), "Source", "").module
            });
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Import_DoesNotFindUnqualifiedName()
        {
            var moduleSource = @"private bool IsHappy()
begin
  return true
end";
            var source = @"import Mod
public bool Main()
begin
  return IsHappy()
end";

            var compiler = new SemanticCompiler(new List<ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(moduleSource), "Module", "Mod").module,
                ModuleParser.Parse(new StringReader(source), "Source", "").module
            });
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Literal_IntPromotedToDouble()
        {
            var source = @"private void Main()
begin
  int A 10
  real B 10
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program.Constants, Has.Exactly(2).Items);
            Assert.That(program.Constants[0].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(program.Constants[0].FullName, Is.EqualTo("$literal_10"));
            Assert.That(program.Constants[1].Type, Is.EqualTo(PrimitiveType.Real));
            Assert.That(program.Constants[1].FullName, Is.EqualTo("$literal_10r"));
        }

        [Test]
        public void Literal_NoDuplicates()
        {
            var source = @"private void Main()
begin
  int A 10
  int B 10
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program.Constants, Has.Exactly(1).Items);
        }

        [Test]
        public void Return_IntLiteral()
        {
            var source = @"private int Main()
begin
  return 100
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(program.Constants, Has.Exactly(1).Items);
            Assert.That(program.Constants[0].FullName, Is.EqualTo("$literal_100"));
            Assert.That(program.Constants[0].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(program.Constants[0].Value, Is.EqualTo(100));

            Assert.That(program.Functions, Has.Exactly(1).Items);
            Assert.That(program.Functions[0].FullName, Is.EqualTo("main"));
            Assert.That(program.Functions[0].FunctionTableIndex, Is.EqualTo(0));
            Assert.That(program.Functions[0].ReturnType, Is.EqualTo(PrimitiveType.Int));
            Assert.That(program.Functions[0].ParameterTypes, Is.Empty);
            Assert.That(program.Functions[0].Bytecode, Has.Exactly(2).Items);
            Assert.That(program.Functions[0].Bytecode[0].Opcode, Is.EqualTo(Opcode.PushConst));
            Assert.That(program.Functions[0].Bytecode[0].Parameter, Is.EqualTo(0));
            Assert.That(program.Functions[0].Bytecode[1].Opcode, Is.EqualTo(Opcode.Return));

            var disasm = @"Int main() [0 locals]
PushConst   $literal_100
Return";
            VerifyDisassembly(program.Functions[0], program, disasm);
        }
        
        [Test]
        public void Return_Constant()
        {
            var source = @"private int Result 100

private int Main()
begin
  return Result
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(program.Constants, Has.Exactly(1).Items);
            Assert.That(program.Constants[0].FullName, Is.EqualTo("result"));
            Assert.That(program.Constants[0].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(program.Constants[0].Value, Is.EqualTo(100));

            Assert.That(program.Functions, Has.Exactly(1).Items);
            Assert.That(program.Functions[0].FullName, Is.EqualTo("main"));
            var disasm = @"Int main() [0 locals]
PushConst   result
Return";
            VerifyDisassembly(program.Functions[0], program, disasm);
        }

        [Test]
        public void Return_Constant_MissingConstant()
        {
            var source = @"private int Main()
begin
  return Result
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Return_Constant_WrongType()
        {
            var source = @"private int Result 100

private bool Main()
begin
  return Result
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Int"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(5));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Return_WrongExpressionTypeRaisesError()
        {
            var source = @"private bool Main()
begin
  return 2
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(3));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Return_FunctionCall()
        {
            var source = @"private bool A()
begin
  return true
end

private bool Main()
begin
  return A()
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(program.Constants, Has.Exactly(1).Items);
            Assert.That(program.Constants[0].FullName, Is.EqualTo("$literal_true"));
            Assert.That(program.Constants[0].Type, Is.EqualTo(PrimitiveType.Bool));
            Assert.That(program.Constants[0].Value, Is.EqualTo(true));

            Assert.That(program.Functions, Has.Exactly(2).Items);
            var main = program.Functions[program.MainFunctionIndex];
            var a = program.Functions[1 - program.MainFunctionIndex];

            Assert.That(main.FullName, Is.EqualTo("main"));
            var mainDis = @"
Bool main() [0 locals]
Call        a
Return";
            VerifyDisassembly(main, program, mainDis);

            Assert.That(a.FullName, Is.EqualTo("a"));
            var aDis = @"
Bool a() [0 locals]
PushConst   $literal_true
Return";
            VerifyDisassembly(a, program, aDis);
        }

        [Test]
        public void Return_FunctionCall_WrongType()
        {
            var source = @"private bool A()
begin
  return true
end

private int Main()
begin
  return A()
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Int"));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(8));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Return_FunctionCallWithParams()
        {
            var source = @"private bool A(int b, bool c)
begin
  return true
end

private bool Main()
begin
  return A(12, true)
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(program.Functions, Has.Exactly(2).Items);
            var main = program.Functions[program.MainFunctionIndex];

            Assert.That(main.FullName, Is.EqualTo("main"));
            var mainDis = @"
Bool main() [0 locals]
PushConst   $literal_12
PushConst   $literal_true
Call        a
Return";
            VerifyDisassembly(main, program, mainDis);
        }

        [Test]
        public void Return_FunctionCallWithParams_WrongType()
        {
            var source = @"private bool A(int b, bool c)
begin
  return true
end

private bool Main()
begin
  return A(12, 3)
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(8));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(16));
        }

        [Test]
        public void Return_FunctionCallWithParams_TooFewParams()
        {
            var source = @"private bool A(int b, bool c)
begin
  return true
end

private bool Main()
begin
  return A(12)
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NotEnoughParameters));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("1"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("2"));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(8));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Return_FunctionCallWithParams_TooManyParams()
        {
            var source = @"private bool A(int b, bool c)
begin
  return true
end

private bool Main()
begin
  return A(12, true, 14)
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.TooManyParameters));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("3"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("2"));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(8));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Return_Local()
        {
            var source = @"private int Main()
begin
  int a 100
  return a
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(program.Functions, Has.Exactly(1).Items);
            var main = program.Functions[program.MainFunctionIndex];

            Assert.That(main.Locals.Count, Is.EqualTo(1));
            // Set the local and then return it
            // No optimizations are applied
            var mainDis = @"
Int main() [1 locals]
PushConst   $literal_100
PopLocal    a
PushLocal   a
Return";
            VerifyDisassembly(main, program, mainDis);
        }

        [Test]
        public void Return_Local_WrongType()
        {
            var source = @"private bool Main()
begin
  int a 100
  return a
end";
            (var program, var diagnostics) = CompileStringWithDiagnostics(source);

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.WrongType));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("Int"));
            Assert.That(diagnostics[0].Expected, Is.EqualTo("Bool"));
            Assert.That(diagnostics[0].IsError, Is.True);
            Assert.That(diagnostics[0].Position.Filename, Is.EqualTo("Filename"));
            Assert.That(diagnostics[0].Position.LineNumber, Is.EqualTo(4));
            Assert.That(diagnostics[0].Position.Column, Is.EqualTo(10));
        }

        [Test]
        public void Return_Parameter()
        {
            var source = @"private int Main()
begin
  return Func(100)
end

private int Func(int a)
begin
  return a
end";
            var program = CompileStringWithoutDiagnostics(source);

            Assert.That(program, Is.Not.Null);
            Assert.That(program.Functions, Has.Exactly(2).Items);

            var main = program.Functions[program.MainFunctionIndex];
            var mainDis = @"
Int main() [0 locals]
PushConst   $literal_100
Call        func
Return";
            VerifyDisassembly(main, program, mainDis);

            var func = program.Functions[1 - program.MainFunctionIndex];
            var funcDis = @"
Int func(Int) [1 locals]
PushLocal   a
Return";
            VerifyDisassembly(func, program, funcDis);
        }
    }
}
