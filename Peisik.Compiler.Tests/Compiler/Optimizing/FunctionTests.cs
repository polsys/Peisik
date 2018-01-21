using System;
using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Parser;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class FunctionTests : CompilerTestBase
    {
        [Test]
        public void EmptyFunction_NoParams()
        {
            var syntax = new FunctionSyntax(new TokenPosition(), PrimitiveType.Void, Visibility.Public, "Main");
            syntax.SetBlock(new BlockSyntax(new TokenPosition()));
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>(), Optimization.None);

            var function = Function.InitializeFromSyntax(syntax, compiler, "");

            Assert.That(function.ResultValue, Is.Not.Null);
            Assert.That(function.ResultValue.Type, Is.EqualTo(PrimitiveType.Void));
            Assert.That(function.ResultValue.AssignmentCount, Is.Zero);
            Assert.That(function.ResultValue.UseCount, Is.Zero);
            Assert.That(function.ResultValue.Name, Is.EqualTo("$result"));
        }

        [Test]
        public void EmptyFunction_Params()
        {
            var syntax = new FunctionSyntax(new TokenPosition(), PrimitiveType.Void, Visibility.Public, "Main");
            syntax.SetBlock(new BlockSyntax(new TokenPosition()));
            syntax.AddParameter(new VariableDeclarationSyntax(new TokenPosition(), PrimitiveType.Int, "intParam"));
            syntax.AddParameter(new VariableDeclarationSyntax(new TokenPosition(), PrimitiveType.Bool, "boolParam"));
            syntax.AddParameter(new VariableDeclarationSyntax(new TokenPosition(), PrimitiveType.Real, "realParam"));
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>(), Optimization.None);

            var function = Function.InitializeFromSyntax(syntax, compiler, "");

            Assert.That(function.Locals, Has.Exactly(4).Items);
            // Parameters are passed as locals in order from left to right
            // These locals are marked special
            Assert.That(function.Locals[0].Name, Does.StartWith("intparam"));
            Assert.That(function.Locals[0].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(function.Locals[0].IsParameter, Is.True);
            Assert.That(function.Locals[1].Name, Does.StartWith("boolparam"));
            Assert.That(function.Locals[1].Type, Is.EqualTo(PrimitiveType.Bool));
            Assert.That(function.Locals[1].IsParameter, Is.True);
            Assert.That(function.Locals[2].Name, Does.StartWith("realparam"));
            Assert.That(function.Locals[2].Type, Is.EqualTo(PrimitiveType.Real));
            Assert.That(function.Locals[2].IsParameter, Is.True);

            Assert.That(function.Locals[3].Name, Is.EqualTo("$result"));
            Assert.That(function.Locals[3].Type, Is.EqualTo(PrimitiveType.Void));
            Assert.That(function.Locals[3].IsParameter, Is.False);
        }
    }
}
