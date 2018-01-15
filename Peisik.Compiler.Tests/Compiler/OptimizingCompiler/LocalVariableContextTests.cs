using System;
using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    class LocalVariableContextTests : CompilerTestBase
    {
        [Test]
        public void Add_SingleContext()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            var local1 = context.AddLocal("local1", PrimitiveType.Int, new TokenPosition());
            Assert.That(local1.Name, Does.StartWith("local1"));

            var local2 = context.AddLocal("LOCAL2", PrimitiveType.Real, new TokenPosition());
            Assert.That(local2.Name, Does.StartWith("local2"));
        }

        [Test]
        public void Add_MayAddInOuterContext()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            context.Push();
            var local1 = context.AddLocal("local", PrimitiveType.Int, new TokenPosition());
            Assert.That(local1.Name, Does.StartWith("local"));

            context.Pop(); // Removes the local
            var local2 = context.AddLocal("LOCAL", PrimitiveType.Real, new TokenPosition());
            Assert.That(local2.Name, Does.StartWith("local"));
        }

        [Test]
        public void Add_NameAlreadyDefined_SameContext()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            context.AddLocal("local", PrimitiveType.Int, new TokenPosition());
            Assert.That(() => context.AddLocal("Local", PrimitiveType.Int, new TokenPosition("file", 1, 1)),
                Throws.InstanceOf<CompilerException>());

            Assert.That(compiler._diagnostics, Has.Exactly(1).Items);
            Assert.That(compiler._diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameAlreadyDefined));
            Assert.That(compiler._diagnostics[0].AssociatedToken, Is.EqualTo("Local"));
            Assert.That(compiler._diagnostics[0].Position.Filename, Is.EqualTo("file"));
        }

        [Test]
        public void Add_NameAlreadyDefined_InnerContext()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            context.AddLocal("local", PrimitiveType.Int, new TokenPosition());
            context.Push();
            Assert.That(() => context.AddLocal("Local", PrimitiveType.Int, new TokenPosition("file", 1, 1)),
                Throws.InstanceOf<CompilerException>());

            Assert.That(compiler._diagnostics, Has.Exactly(1).Items);
            Assert.That(compiler._diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameAlreadyDefined));
            Assert.That(compiler._diagnostics[0].AssociatedToken, Is.EqualTo("Local"));
            Assert.That(compiler._diagnostics[0].Position.Filename, Is.EqualTo("file"));
        }

        [Test]
        public void GetLocal_SingleContext()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            var added = context.AddLocal("local1", PrimitiveType.Int, new TokenPosition());

            var got = context.GetLocal("LOCAL1", new TokenPosition());
            Assert.That(got, Is.SameAs(added));
        }

        [Test]
        public void GetLocal_SameInnerContext()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            context.Push();
            var added = context.AddLocal("local1", PrimitiveType.Int, new TokenPosition());

            var got = context.GetLocal("LOCAL1", new TokenPosition());
            Assert.That(got, Is.SameAs(added));
        }

        [Test]
        public void GetLocal_FromOuterContext()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            var added = context.AddLocal("local1", PrimitiveType.Int, new TokenPosition());
            context.Push();

            var got = context.GetLocal("LOCAL1", new TokenPosition());
            Assert.That(got, Is.SameAs(added));
        }

        [Test]
        public void GetLocal_NameNotFound()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            Assert.That(() => context.GetLocal("Another", new TokenPosition("file", 1, 1)),
                Throws.InstanceOf<CompilerException>());

            Assert.That(compiler._diagnostics, Has.Exactly(1).Items);
            Assert.That(compiler._diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(compiler._diagnostics[0].AssociatedToken, Is.EqualTo("Another"));
            Assert.That(compiler._diagnostics[0].Position.Filename, Is.EqualTo("file"));
        }

        [Test]
        public void GetLocal_NotAvailableFromInnerContext()
        {
            var compiler = new OptimizingCompiler(new List<Peisik.Parser.ModuleSyntax>() { }, Optimization.None);
            var function = new Function(compiler);
            var context = new LocalVariableContext(function);

            context.Push();
            context.AddLocal("Local", PrimitiveType.Bool, new TokenPosition());
            context.Pop();

            Assert.That(() => context.GetLocal("Local", new TokenPosition("file", 1, 1)),
                Throws.InstanceOf<CompilerException>());

            Assert.That(compiler._diagnostics, Has.Exactly(1).Items);
            Assert.That(compiler._diagnostics[0].Diagnostic, Is.EqualTo(DiagnosticCode.NameNotFound));
            Assert.That(compiler._diagnostics[0].AssociatedToken, Is.EqualTo("Local"));
            Assert.That(compiler._diagnostics[0].Position.Filename, Is.EqualTo("file"));
        }
    }
}
