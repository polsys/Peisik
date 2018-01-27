using System.Collections.Generic;
using NUnit.Framework;
using Polsys.Peisik.Parser;
using Polsys.Peisik.Compiler;
using Polsys.Peisik.Compiler.Optimizing;

namespace Polsys.Peisik.Tests.Compiler.Optimizing
{
    partial class ExpressionTreeTests : CompilerTestBase
    {
        private Function SingleFunctionFromSyntax(string source)
        {
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            return function;
        }

        [Test]
        public void ExplicitVoidReturn()
        {
            var source = @"
public void Main()
begin
  return void
end";
            var function = SingleFunctionFromSyntax(source);

            // The expression tree should be
            // (root)
            //   |-- Return (null)
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[0];
            Assert.That(ret.Value, Is.Null);
        }

        [Test]
        public void ImplicitVoidReturn()
        {
            var source = @"
public void Main()
begin
end";
            var function = SingleFunctionFromSyntax(source);

            // The expression tree should be
            // (root)
            //   |-- Return (null)
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[0];
            Assert.That(ret.Value, Is.Null);
        }

        [Test]
        public void LiteralReturning()
        {
            var source = @"
public int Main()
begin
  return 5
end";
            var function = SingleFunctionFromSyntax(source);

            // The expression tree should be
            // (root)
            //   |-- Return $result
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[0];
            Assert.That(ret.Value, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)ret.Value).Value, Is.EqualTo(5L));
        }

        [Test]
        public void LocalAssignment()
        {
            var source = @"
public void Main()
begin
  real local 4.0
  local = 5.0
end";
            var function = SingleFunctionFromSyntax(source);

            Assert.That(function.Locals, Has.Exactly(2).Items);
            var local = function.Locals[1];
            Assert.That(local.Name, Does.StartWith("local"));
            Assert.That(local.Type, Is.EqualTo(PrimitiveType.Real));

            // The expression tree should be
            // (root)
            //   |-- Constant -> local
            //   |-- Constant -> local
            //   |-- Return
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(3).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ConstantExpression>());
            Assert.That(sequence.Expressions[0].Store, Is.SameAs(local));

            Assert.That(sequence.Expressions[1], Is.InstanceOf<ConstantExpression>());
            Assert.That(sequence.Expressions[1].Store, Is.SameAs(local));

            Assert.That(sequence.Expressions[2], Is.InstanceOf<ReturnExpression>());
        }

        [Test]
        public void LocalReturning()
        {
            var source = @"
public int Main()
begin
  int LOCAL 5
  return local
end";
            var function = SingleFunctionFromSyntax(source);

            Assert.That(function.Locals, Has.Exactly(2).Items);
            var local = function.Locals[1];
            Assert.That(local.Name, Does.StartWith("local"));
            Assert.That(local.Type, Is.EqualTo(PrimitiveType.Int));

            // The expression tree should be
            // (root)
            //   |-- Constant -> local
            //   |-- Return local
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(2).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ConstantExpression>());
            Assert.That(sequence.Expressions[0].Store, Is.SameAs(local));

            Assert.That(sequence.Expressions[1], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[1];
            Assert.That(ret.Value, Is.InstanceOf<LocalLoadExpression>());
            Assert.That(((LocalLoadExpression)ret.Value).Local, Is.SameAs(local));
        }

        [Test]
        public void ConstantReturning()
        {
            var source = @"
private int Result 42 # Actually added to the compilation in code

public int Main()
begin
  return Result
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            compiler.AddConstant("Result", "", Visibility.Private, 42L);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            // The expression tree should be
            // (root)
            //   |-- Return ConstantExpression(42)
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = function.ExpressionTree as SequenceExpression;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[0];
            Assert.That(ret.Value, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)ret.Value).Value, Is.EqualTo(42L));
        }

        [Test]
        public void UnaryExpression()
        {
            var source = @"
public int Main()
begin
  return -(42)
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            // The expression tree should be
            // (root)
            //   |-- Return
            //       | -- UnaryExpression (-)
            //            | -- ConstantExpression (42)
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = (SequenceExpression)function.ExpressionTree;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[0];
            Assert.That(ret.Value, Is.InstanceOf<UnaryExpression>());
            var unaryExpr = (UnaryExpression)ret.Value;
            Assert.That(unaryExpr.InternalFunctionId, Is.EqualTo(InternalFunction.Minus));
            Assert.That(unaryExpr.Expression, Is.InstanceOf<ConstantExpression>());
            Assert.That(unaryExpr.Type, Is.EqualTo(PrimitiveType.Int));
        }

        [Test]
        public void BinaryExpression()
        {
            var source = @"
public real Main()
begin
  return -(42, 2.1)
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            // The expression tree should be
            // (root)
            //   |-- Return
            //       | -- BinaryExpression (-)
            //            | -- ConstantExpression (42)
            //            | -- ConstantExpression (2.1)
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = (SequenceExpression)function.ExpressionTree;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)sequence.Expressions[0];
            Assert.That(ret.Value, Is.InstanceOf<BinaryExpression>());
            var binaryExpr = (BinaryExpression)ret.Value;
            Assert.That(binaryExpr.InternalFunctionId, Is.EqualTo(InternalFunction.Minus));
            Assert.That(binaryExpr.Left, Is.InstanceOf<ConstantExpression>());
            Assert.That(binaryExpr.Right, Is.InstanceOf<ConstantExpression>());
            Assert.That(binaryExpr.Type, Is.EqualTo(PrimitiveType.Real));
        }

        [Test]
        public void FailFastExpression()
        {
            var source = @"
public void Main()
begin
  FailFast()
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            // The expression tree should be
            // (root)
            //   |-- FailFast
            //   |-- Return [redundant, but good to have]
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = (SequenceExpression)function.ExpressionTree;
            Assert.That(sequence.Expressions, Has.Exactly(2).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<FailFastExpression>());
            Assert.That(sequence.Expressions[1], Is.InstanceOf<ReturnExpression>());
        }

        [Test]
        public void PrintExpression()
        {
            var source = @"
public void Main()
begin
  print(true, 1, 1.0)
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            // The expression tree should be
            // (root)
            //   |-- PrintExpression
            //   |   | -- ConstantExpression (true)
            //   |   | -- ConstantExpression (1)
            //   |   | -- ConstantExpression (1.0)
            //   |
            //   |--Return
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = (SequenceExpression)function.ExpressionTree;
            Assert.That(sequence.Expressions, Has.Exactly(2).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<PrintExpression>());
            Assert.That(sequence.Expressions[1], Is.InstanceOf<ReturnExpression>());

            var print = (PrintExpression)sequence.Expressions[0];
            Assert.That(print.Expressions, Has.Exactly(3).Items.And.All.InstanceOf<ConstantExpression>());
            Assert.That(print.Expressions[0].Type, Is.EqualTo(PrimitiveType.Bool));
            Assert.That(print.Expressions[1].Type, Is.EqualTo(PrimitiveType.Int));
            Assert.That(print.Expressions[2].Type, Is.EqualTo(PrimitiveType.Real));
        }

        [Test]
        public void IfExpression()
        {
            var source = @"
public int Main()
begin
  if true
  begin
    return 1
  end
  else
  begin
    print(2)
    return 2
  end
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();

            // The expression tree should be
            // (root)
            //   |-- IfExpression
            //       | -- ConstantExpression (true)
            //       | -- SequenceExpression
            //       |    | -- ReturnExpression (1)
            //       |
            //       | -- SequenceExpression
            //            | -- PrintExpression (2)
            //            | -- ReturnExpression (2)
            Assert.That(function.ExpressionTree, Is.InstanceOf<SequenceExpression>());
            var sequence = (SequenceExpression)function.ExpressionTree;
            Assert.That(sequence.Expressions, Has.Exactly(1).Items);

            Assert.That(sequence.Expressions[0], Is.InstanceOf<IfExpression>());
            var expr = (IfExpression)sequence.Expressions[0];
            Assert.That(expr.Condition, Is.InstanceOf<ConstantExpression>());
            Assert.That(expr.Condition.Type, Is.EqualTo(PrimitiveType.Bool));

            Assert.That(expr.ThenExpression, Is.InstanceOf<SequenceExpression>());
            Assert.That(((SequenceExpression)expr.ThenExpression).Expressions, Has.Exactly(1).Items);
            Assert.That(expr.ElseExpression, Is.InstanceOf<SequenceExpression>());
            Assert.That(((SequenceExpression)expr.ElseExpression).Expressions, Has.Exactly(2).Items);
        }

        [Test]
        public void ConstantFolding_BinaryExpression()
        {
            var source = @"
public real Main()
begin
  return +(42, 2.1)
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();
            function.AnalyzeAndOptimizePreInlining(Optimization.ConstantFolding);

            // After constant folding, the expression tree should be
            // Return
            //   | -- ConstantExpression (44.1)
            Assert.That(function.ExpressionTree, Is.InstanceOf<ReturnExpression>());
            var ret = (ReturnExpression)function.ExpressionTree;

            Assert.That(ret.Value, Is.InstanceOf<ConstantExpression>());
            var result = (ConstantExpression)ret.Value;
            Assert.That(result.Type, Is.EqualTo(PrimitiveType.Real));
            Assert.That(result.Value, Is.EqualTo(44.1).Within(0.00001));
        }

        [Test]
        public void ConstantFolding_InIf()
        {
            var source = @"
public int Main()
begin
  if ==(1, 1)
  begin
    return +(1, 99)
  end
  else
  begin
    return +(-1, 1)
  end
end";
            var syntax = ParseStringWithoutDiagnostics(source);
            var compiler = new OptimizingCompiler(new List<ModuleSyntax>() { syntax }, Optimization.None);
            var function = Function.InitializeFromSyntax(syntax.Functions[0], compiler, "");
            function.Compile();
            function.AnalyzeAndOptimizePreInlining(Optimization.ConstantFolding);

            // After constant folding, the expression tree should be
            // If
            //   | -- ConstantExpression (true)
            //   | -- ReturnExpression (ConstantExpression 100)
            //   | -- ReturnExpression (ConstantExpression 0)
            Assert.That(function.ExpressionTree, Is.InstanceOf<IfExpression>());
            var cond = (IfExpression)function.ExpressionTree;

            Assert.That(cond.Condition, Is.InstanceOf<ConstantExpression>());
            Assert.That(((ConstantExpression)cond.Condition).Value, Is.EqualTo(true));

            Assert.That(cond.ThenExpression, Is.InstanceOf<ReturnExpression>());
            Assert.That(((ReturnExpression)cond.ThenExpression).Value, Is.InstanceOf<ConstantExpression>());

            Assert.That(cond.ElseExpression, Is.InstanceOf<ReturnExpression>());
            Assert.That(((ReturnExpression)cond.ElseExpression).Value, Is.InstanceOf<ConstantExpression>());
        }
    }
}
