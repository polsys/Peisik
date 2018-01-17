using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Compiler.Optimizing;

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
    }
}
