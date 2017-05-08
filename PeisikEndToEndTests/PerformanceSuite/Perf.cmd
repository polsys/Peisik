@echo off

rem Peisik performance test suite
rem Compiles and runs each test using a single compiler/interpreter instance
rem The tests are tuned to take about 10 seconds each on my i5-4670 + 16 GB RAM system,
rem when compiled using the non-optimizing compiler.
rem TODO: Variations of this script for optimizing/non-optimizing.

echo.
echo COMPILER
echo.
peisikc Warmup BuiltinTest MonteCarloPi Trig Sum_NoTail Sum_Tail --timing
echo.
echo INTERPRETER
echo.
peisik Warmup BuiltinTest MonteCarloPi Trig Sum_NoTail Sum_Tail --timing
