@echo off

set __Files=Warmup BuiltinTest MonteCarloPi Trig Swap Fibonacci FermatPrimality BellardPi Binomial Sum_NoTail Sum_Tail
set __Countops=
set __Optimize=-O

if "%1" == "compare" goto Compare
if "%1" == "countops" goto CountOps
if "%1" == "diff" goto Diff
if "%1" == "noopt" goto NoOpt
if "%1" == "run" goto Run
goto Usage



:CountOps
set __Countops=--countops

:Compare
echo.
echo Base - Compiling...
echo.
peisikc %__Files% --timing
echo.
echo.
echo Base - Running...
echo.
peisik %__Files% --timing %__Countops%

echo.
echo Optimized - Compiling...
echo.
peisikc %__Files% -O --timing
echo.
echo.
echo Optimized - Running...
echo.
peisik %__Files% --timing %__Countops%

goto :eof



:Diff
peisikc %__Files% --disasm > base.txt
peisikc %__Files% --disasm -O > new.txt
git diff --no-index base.txt new.txt > asmdiff.diff
echo Diff is available in asmdiff.diff

goto :eof



:NoOpt
set __Optimize=

:Run
echo.
echo Compiling...
echo.
peisikc %__Files% --timing %__Optimize%
echo.
echo.
echo Running...
echo.
peisik %__Files% --timing

goto :eof

:Usage
echo Usage: perf (option)
echo Possible options:
echo   compare  Compiles and runs the test suite with and without optimization,
echo            collecting timings.
echo   countops Compiles and runs the test suite with and without optimization,
echo            collecting both timings and instruction counts.
echo   diff     Compiles the test suite with and without optimization,
echo            then diffs the two with "git diff" and stores the result
echo            in asmdiff.diff. Creates files base.txt and new.txt.
echo   noopt    Compiles and runs the test suite without optimization,
echo            outputting timings.
echo   run      Compiles and runs the test suite with optimization,
echo            outputting timings.
