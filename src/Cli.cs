namespace IronbrewDeobfuscator;

public static class Cli
{
    public static void Main()
    {
        var cliRunning = true;
        Console.WriteLine("Ironbrew Deobfuscator");
        Console.WriteLine("Version: beta-1.0");
        Console.WriteLine("Author: Asteriva");
        Console.WriteLine("GitHub: github.com/Asteriva");
        
        while (cliRunning)
        {
            Console.Write(">> ");
            var userInput = Console.ReadLine();
            switch (userInput)
            {
                case "exit":
                    cliRunning = false;
                    continue;
                case null:
                    Console.WriteLine("Maybe try running the help command?");
                    continue;
                case "help":
                    Console.WriteLine("Usage: deobf -t ib2 -f <input file path> -o <output file path>");
                    Console.WriteLine("Optional Parameter #1: --enable-maxcflow  --> Enables the maximum cflow deobfuscation [unstable]");
                    Console.WriteLine("Optional Parameter #2: --debug  --> Enables the debug mode");
                    break;
            }
            
            ParseCommandLine(userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            
        }
        
    }

    private static void ParseCommandLine(IReadOnlyList<string> args)
    {
        if (args.Count < 4 || args[0] != "deobf" || args[1] != "-t" || args[3] != "-f" || args[5] != "-o")
        {
            Console.WriteLine("Invalid command. Type 'help' for usage information.");
            return;
        }

        var deobfuscationTarget = args[2];
        if (deobfuscationTarget != "ib2")
        {
            Console.WriteLine("Invalid deobfuscation target. Only 'ib2' is supported.");
            return;
        }
        var inputFilePath = args[4];
        var outputFilePath = args[6];

        var enableMaxCFlow = args.Contains("--enable-maxcflow");
        var debugMode = args.Contains("--debug");

        // Call the obfuscation method with the provided parameters
        var inputFileString = File.ReadAllText(inputFilePath);
        var bytecode = Deobfuscation.HandleIb2Deobfuscation(inputFileString, enableMaxCFlow, debugMode);
        
        File.WriteAllBytes(outputFilePath, bytecode.ToArray());

        Console.WriteLine($"Deobfuscation complete. Please check {outputFilePath} !");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
        // close the program
        Environment.Exit(0);
    }
}