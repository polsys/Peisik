using NUnit.Framework;

namespace PeisikEndToEndTests
{
    class SimpleBringUp : EndToEndTestBase
    {
        [Test]
        public void Hello100()
        {
            var source = @"public int Main()
begin
  return 100
end";
            var output = CompileAndRun(source, "Hello100.cpeisik", "");

            Assert.That(output.Trim(), Is.EqualTo("100"));
        }

        [Test]
        public void Hello100_DumpStats()
        {
            var source = @"public int Main()
begin
  return 100
end";
            var output = CompileAndRun(source, "Hello100_stats.cpeisik", "--dumpstats");

            var expected = @"-- Hello100_stats.cpeisik
   Constants: 1
   Functions: 1
   Main function index: 0
   Total code size: 2";
            Assert.That(output.Trim(), Is.EqualTo(expected));
        }

        [Test]
        public void FunctionCallNoParams()
        {
            var source = @"private bool DoesItWork()
begin
  return false
end

public bool Main()
begin
  return DoesItWork()
end";
            var output = CompileAndRun(source, "FunctionCallNoParams.cpeisik", "");

            Assert.That(output.Trim(), Is.EqualTo("false"));
        }

        [Test]
        public void FunctionCallNoParams_DumpStats()
        {
            var source = @"private bool DoesItWork()
begin
  return false
end

public bool Main()
begin
  return DoesItWork()
end";
            var output = CompileAndRun(source, "FunctionCallNoParams_stats.cpeisik", "--dumpstats");

            var expected = @"-- FunctionCallNoParams_stats.cpeisik
   Constants: 1
   Functions: 2
   Main function index: 1
   Total code size: 4";
            Assert.That(output.Trim(), Is.EqualTo(expected));
        }

        [Test]
        public void FunctionCallWithParams()
        {
            var source = @"private int Main()
begin
  int result 100
  return Func(result, true)
end

private int Func(int a, bool b)
begin
  return a
  # b is unused
end";
            var output = CompileAndRun(source, "FunctionCallWithParams.cpeisik", "");

            Assert.That(output.Trim(), Is.EqualTo("100"));
        }

        [Test]
        public void FunctionCallVoid()
        {
            var source = @"private int Main()
begin
  Func()
  return 100
end

private void Func()
begin
end";
            var output = CompileAndRun(source, "FunctionCallVoid.cpeisik", "");

            Assert.That(output.Trim(), Is.EqualTo("100"));
        }

        [Test]
        public void InternalFunctionCall()
        {
            var source = @"private int Main()
begin
  int a 30
  return +(a, 70)
end";
            var output = CompileAndRun(source, "InternalFunctionCall.cpeisik", "");

            Assert.That(output.Trim(), Is.EqualTo("100"));
        }

        [Test]
        public void FailFast()
        {
            var source = @"private void Main()
begin
  F()
end

private void F()
begin
  FailFast()
end";
            var output = CompileAndRun(source, "FailFast.cpeisik", "");

            var result = output.Trim();
            Assert.That(result, Does.Contain("FailFast"));
            Assert.That(result, Does.Contain("0, instruction 0"));
            Assert.That(result, Does.Contain("1, instruction 0"));
        }

        [Test]
        public void SimpleIf()
        {
            var source = @"private bool Hundred(int a)
begin
  return true
end

private int Main()
begin
  int result 0
  if Hundred(result)
  begin
    result = 100
  end
  else
  begin
    result = -100
  end
  return result
end";
            var output = CompileAndRun(source, "SimpleIf.cpeisik", "");

            Assert.That(output.Trim(), Is.EqualTo("100"));
        }

        [Test]
        public void Quadratic()
        {
            var source = @"
# This program solves the quadratic equation (x-8)(x+2) = x^2 - 6x - 16
# and returns the sum of its roots.
# The implemented method is not general but assumes that the two real roots exist.

private real RootA(real a, real b, real c)
begin
  real D +(*(b,b), *(-4,*(a,c)))
  return /(+(-(b), Math.Sqrt(D)), *(2,a))
end

private real RootB(real a, real b, real c)
begin
  real D +(*(b,b), *(-4,*(a,c)))
  return /(-(-(b), Math.Sqrt(D)), *(2,a))
end

private int Main()
begin
  real a 1
  real b -6
  real c -16

  return Math.Round(+(RootA(a,b,c), RootB(a,b,c)))
end";
            var output = CompileAndRun(source, "Quadratic.cpeisik", "");

            Assert.That(output.Trim(), Is.EqualTo("6"));
        }
        
        [Test]
        public void While_ArithmeticSum()
        {
            var source = @"
# Arithmetic sum of 0..9
private int Main()
begin
  int i 0
  int sum 0
  while <(i, 10)
  begin
    sum = +(sum, i)
    i = +(i, 1)
  end
  return sum
end";
            var output = CompileAndRun(source, "ArithmeticSum.cpeisik", "");

            Assert.That(output.Trim(), Is.EqualTo("45"));
        }
    }
}
