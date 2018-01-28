using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Polsys.Peisik.Compiler.Optimizing;
using Polsys.Peisik.Parser;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    partial class OptimizingCompilerTests : CompilerTestBase
    {
        [Test]
        public void ConstantReturningProgram()
        {
            var source = @"
private int Result 42

public int Main()
begin
  return Result
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            // The compiler replaces the constant with its value, so the renaming is expected
            var disasm = @"Int main() [0 locals]
PushConst   $literal_42
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void ConstantFromOtherModule_Works()
        {
            var otherModule = @"
public int Value 100

private int Function()
begin
  return Value
end
";
            var mainModule = @"
import OtherModule
public int Main()
begin
  return OtherModule.Value
end";
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(mainModule), "", "").module,
                ModuleParser.Parse(new StringReader(otherModule), "", "OtherModule").module,
            }, Optimization.None);
            (var program, var diagnostics) = compiler.Compile();
            
            Assert.That(diagnostics, Is.Empty);
            var disasm = @"Int main() [0 locals]
PushConst   $literal_100
Return";
            VerifyDisassembly(program.Functions[0], program, disasm);
            var disasmForOtherFunction = @"Int othermodule.function() [0 locals]
PushConst   $literal_100
Return";
            VerifyDisassembly(program.Functions[1], program, disasmForOtherFunction);
        }

        [Test]
        public void ConstantFromOtherModule_PrivateFails()
        {
            var otherModule = @"
private int Value 100";
            var mainModule = @"
import OtherModule
public int Main()
begin
  return OtherModule.Value
end";
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(otherModule), "", "OtherModule").module,
                ModuleParser.Parse(new StringReader(mainModule), "", "").module
            }, Optimization.None);
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            //
            // The old compiler returned NameIsPrivate, but this version does not.
            // This is an acceptable regression.
            //
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("OtherModule.Value"));
        }

        [Test]
        public void FunctionFromOtherModule_Works()
        {
            var otherModule = @"
public int Function()
begin
  return 2600
end
";
            var mainModule = @"
import OtherModule
public int Main()
begin
  return OtherModule.Function()
end";
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(mainModule), "", "").module,
                ModuleParser.Parse(new StringReader(otherModule), "", "OtherModule").module,
            }, Optimization.None);
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(diagnostics, Is.Empty);
            var disasm = @"Int main() [0 locals]
Call        othermodule.function
Return";
            VerifyDisassembly(program.Functions[0], program, disasm);
            var disasmForOtherFunction = @"Int othermodule.function() [0 locals]
PushConst   $literal_2600
Return";
            VerifyDisassembly(program.Functions[1], program, disasmForOtherFunction);
        }

        [Test]
        public void FunctionFromOtherModule_PrivateFails()
        {
            var otherModule = @"
private int Function()
begin
  return 2600
end";
            var mainModule = @"
import OtherModule
public int Main()
begin
  return OtherModule.Function()
end";
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>()
            {
                ModuleParser.Parse(new StringReader(otherModule), "", "OtherModule").module,
                ModuleParser.Parse(new StringReader(mainModule), "", "").module
            }, Optimization.None);
            (var program, var diagnostics) = compiler.Compile();

            Assert.That(program, Is.Null);
            Assert.That(diagnostics, Has.Exactly(1).Items);
            //
            // The old compiler returned NameIsPrivate, but this version does not.
            // This is an acceptable regression.
            //
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(diagnostics[0].AssociatedToken, Is.EqualTo("OtherModule.Function"));
        }

        [Test]
        public void IntLiteralPromotedToReal_Constant()
        {
            // The parser actually makes sure '1' is parsed as a Real, but having this test for completeness.
            var source = @"
private real Value 1

public real Main()
begin
  return Value
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            var disasm = @"Real main() [0 locals]
PushConst   $literal_1r
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void IntLiteralPromotedToReal_FunctionParameter()
        {
            // FIXME: I have no idea where this test belongs.

            var source = @"
private void Function(real a)
begin
end

public void Main()
begin
  Function(1)
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);
            
            var disasm = @"Void main() [0 locals]
PushConst   $literal_1r
Call        function
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void IntLiteralPromotedToReal_LocalOps()
        {
            // FIXME: I have no idea where this test belongs.
            var source = @"
public real Main()
begin
  real value 1
  value = 2
  return value
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.None);

            var disasm = @"Real main() [1 locals]
PushConst   $literal_1r
PopLocal    value$1
PushConst   $literal_2r
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }

        [Test]
        public void Optimization_ConstantFolding()
        {
            var source = @"
public void Main()
begin
  Print(+(2, 3))
end";
            var program = CompileOptimizedWithoutDiagnostics(source, Optimization.Full);
            
            var disasm = @"Void main() [0 locals]
PushConst   $literal_5
CallI1      Print
Return";
            VerifyDisassembly(program.Functions[program.MainFunctionIndex], program, disasm);
        }
    }
}
