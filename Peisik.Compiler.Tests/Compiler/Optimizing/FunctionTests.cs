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
            Assert.That(function.Locals[0].Name, Is.EqualTo("$result"));
            Assert.That(function.Locals[0].Type, Is.EqualTo(PrimitiveType.Void));
            Assert.That(function.Locals[1].Name, Is.EqualTo("intparam"));
            Assert.That(function.Locals[1].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(function.Locals[2].Name, Is.EqualTo("boolparam"));
            Assert.That(function.Locals[2].Type, Is.EqualTo(PrimitiveType.Bool));
            Assert.That(function.Locals[3].Name, Is.EqualTo("realparam"));
            Assert.That(function.Locals[3].Type, Is.EqualTo(PrimitiveType.Real));
        }
    }
}
