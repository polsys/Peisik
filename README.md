# Peisik

Peisik is a statically-typed procedural verbose slightly-pure programming language. On the current language spectrum, it falls somewhere between Excel cells and BASIC. It is a safe language - no actor, malicious or not, can achieve anything useful with it. The runtime, however, might not be safe.

This implementation compiles Peisik into custom bytecode which is then interpreted. The compiler is implemented in C# and the interpreter in C++.

This has been my tiny programming project and therefore purposefully simple. This could provide a nice starting point for implementing an optimizing compiler -- or just a more useful language.

## Features
- A verbose syntax designed to put off users
- Strict type system with reals, integers and booleans
- A powerful import system for modular code
- No goto statement

### Omitted features
- Textual, user-defined or array data types
- For loops
- Useful standard library (for example, I/O)

## Building
### Windows
* Visual Studio 2017

The only dependencies are `System.ValueTuple` NuGet package for the compiler and `NUnit` (also from NuGet, though you'll need a runner) for the tests. The compiler is written in C# 7, which requires the VS2017 compiler (or later). The interpreter is written in standard C++11 and _should_ compile and run on anything. 

### Linux
For building the compiler, you will need Mono. With minor changes, the compiler should also build nicely with .Net Core. (Note to self: Make it happen!)

The interpreter is easily built and accepts `.cpeisik` files compiled on Windows. As of now there is no build script or makefile. You can build the interpreter by executing the following in the `PeisikInterpreter` directory:
```
g++ *.cpp -std=c++11 -O2 -o peisik
```
Consult the compiler manual for using the precompiled header to speed up compilations.

## Usage
After building the solution and gathering the output together:
```
peisikc (Filenames without extension or .peisik)
peisik (Filenames without extension or .cpeisik)
```
Each input file is compiled/run in order. Imports are resolved automatically. Use the `--help` flag for information on command line parameters.

## Contributing
As this is a tiny side project, I'm not really expecting any contributions. However, if you do use or improve this in some way, I'm very interested!

Happy hacking.