#include "pch.h"
#include "Interpreter.h"
#include "PeisikException.h"
#include "Program.h"

void DumpModuleInfo(const Peisik::Program& program, const std::string& moduleName)
{
    std::cout << "-- " << moduleName << std::endl;
    std::cout << "   Constants: " << program.GetConstantCount() << std::endl;
    std::cout << "   Functions: " << program.GetFunctionCount() << std::endl;
    std::cout << "   Main function index: " << program.GetMainFunctionIndex() << std::endl;

    size_t totalCodeSize = 0;
    for (short i = 0; i < program.GetFunctionCount(); i++)
    {
        totalCodeSize += program.GetFunction(i).GetBytecode().size();
    }

    std::cout << "   Total code size: " << totalCodeSize << std::endl;
}

void PrintHelp()
{
    std::cout << "The Peisik interpreter" << std::endl;
    std::cout << "Usage: peisik [modules] [parameters]" << std::endl;
    std::cout << "Possible parameters:" << std::endl;
    std::cout << " --countops  Print statistics on executed operations." << std::endl;
    std::cout << " --dumpstats Instead of running the program, print basic bytecode statistics." << std::endl;
    std::cout << " --help      Show this help." << std::endl;
    std::cout << " --timing    Print timings." << std::endl;
    std::cout << " --trace     Print each executed instruction." << std::endl;
    std::cout << " --verbose   Print extended debugging information." << std::endl;
}

int main(int argc, char **argv)
{
    // Parse the command line

    std::vector<std::string> modulesToExecute;
    bool countOps = false;
    bool dumpStats = false;
    bool timing = false;
    bool trace = false;
    bool verbose = false;
    bool showHelp = (argc <= 1);

    for (int i = 1; i < argc; i++)
    {
        std::string arg(argv[i]);

        if (arg == "--verbose")
        {
            verbose = true;
        }
        else if (arg == "--countops")
        {
            countOps = true;
        }
        else if (arg == "--dumpstats")
        {
            dumpStats = true;
        }
        else if (arg == "--timing")
        {
            timing = true;
        }
        else if (arg == "--trace")
        {
            trace = true;
        }
        else if (arg == "--help")
        {
            showHelp = true;
        }
        else if (arg.find("--") == 0)
        {
            std::cout << "Unknown parameter: " << arg << std::endl;
            showHelp = true;
        }
        else
        {
            modulesToExecute.push_back(arg);
        }
    }

    if (showHelp)
    {
        PrintHelp();
        return 0;
    }

    auto totalStart = std::chrono::high_resolution_clock::now();

    // Load and execute each module
    for (auto modulePath : modulesToExecute)
    {
        // If the module name does not have an extension, add it
        if (modulePath.find(".") == -1)
        {
            modulePath += ".cpeisik";
        }

        // Load the module
        if (verbose)
            std::cout << "Loading module " << modulePath << std::endl;

        std::ifstream stream(modulePath, std::ifstream::binary);
        if (stream.fail())
        {
            std::cout << "Could not open the module " << modulePath << std::endl;
            return -1;
        }

        try
        {
            auto importStart = std::chrono::high_resolution_clock::now();
            auto program = Peisik::DeserializeProgram(stream);
            auto importEnd = std::chrono::high_resolution_clock::now();

            if (dumpStats)
            {
                // Just dump the module info
                DumpModuleInfo(program, modulePath);
            }
            else
            {
                // Execute the module
                auto executeStart = std::chrono::high_resolution_clock::now();
                Peisik::Interpreter interpreter(program);
                interpreter.SetTrace(trace);

                interpreter.Execute();
                auto executeEnd = std::chrono::high_resolution_clock::now();

                if (countOps)
                {
                    interpreter.PrintOpCount();
                }

                if (timing)
                {
                    std::cout << "-- Timings for " << modulePath << std::endl;
                    auto importTime = std::chrono::duration_cast<std::chrono::microseconds>(importEnd - importStart);
                    std::cout << "   Import: " << importTime.count() / 1000000.0 << " s" << std::endl;
                    auto executeTime = std::chrono::duration_cast<std::chrono::microseconds>(executeEnd - executeStart);
                    std::cout << "   Execution: " << executeTime.count() / 1000000.0 << " s" << std::endl;
                    auto totalTime = std::chrono::duration_cast<std::chrono::microseconds>(executeEnd - importStart);
                    std::cout << "   Total: " << totalTime.count() / 1000000.0 << " s" << std::endl;
                }
            }
        }
        catch (Peisik::ApplicationException& e)
        {
            // Application exceptions arise because of user code bugs

            std::cout << "Error: " << e.what() << std::endl;
            return -1;
        }
        catch (std::exception& e)
        {
            // The rest are because of invalid programs, failed invariants or other interpreter bugs.

            std::cout << "Interpreter error: " << e.what() << std::endl;
#if DEBUG
            throw;
#else
            return -1;
#endif
        }
    }
    if (timing)
    {
        auto totalEnd = std::chrono::high_resolution_clock::now();
        auto totalTimeForAll = std::chrono::duration_cast<std::chrono::microseconds>(totalEnd - totalStart);
        std::cout << "-- Total time for all modules: " << totalTimeForAll.count() / 1000000.0 << " s" << std::endl;
    }

    return 0;
}