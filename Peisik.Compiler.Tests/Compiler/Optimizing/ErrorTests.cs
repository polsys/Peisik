using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Parser;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    partial class ErrorTests : CompilerTestBase
    {
        [Test]
        public void LocalAlreadyDefined()
        {
            var source = @"
public int Main()
begin
  int Local 5
  bool loCal true
  return local
end";
            (var _, var diagnostics) = CompileOptimizedWithDiagnostics(source, Optimization.None);

            Assert.That(diagnostics, Has.Exactly(1).Items);
            Assert.That(diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameAlreadyDefined));
        }
    }
}
