# Ironbrew Deobfuscator

Ironbrew Deobfuscator is a tool designed to deobfuscate scripts obfuscated with Ironbrew2.

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
![GitHub Release](https://img.shields.io/github/v/release/Asteriva/IronbrewDeobfuscator?include_prereleases)
![GitHub Issues or Pull Requests](https://img.shields.io/github/issues/Asteriva/IronbrewDeobfuscator)

## Overview

- **Version:** beta-1.0

## Features

- [X] Bytecode Deobfuscation
- [ ] Control Flow Graph Generation
- [ ] String Decryption
- [ ] Full Macro Support

## Table of Contents

- [Installation](#installation)
- [Command-Line Interface](#command-line-interface)
- [Use directly in your C# application](#use-directly-in-your-c-application)
- [Options](#options)
- [Examples](#examples)
- [Dependencies](#dependencies)
- [Contributing](#contributing)
- [License](#license)
- [Images and Results](#images-and-results)


## Installation

Clone the repository:

```bash
git clone https://github.com/Asteriva/IronbrewDeobfuscator.git
```
Build the project using your preferred method (e.g., Visual Studio, dotnet CLI).

## Command-Line Interface
The deobfuscator provides a simple command-line interface (CLI) for interaction. You can use various commands to control the deobfuscation process.

```plaintext
Usage: deobf -t ib2 -f <input file path>
Optional Parameter #1: --enable-maxcflow  --> Enables the maximum cflow deobfuscation [unstable]
Optional Parameter #2: --debug  --> Enables the debug mode
```
## Use directly in your C# application

You can integrate the deobfuscation process into your own C# application by directly calling the `HandleIb2Deobfuscation` method from the `Deobfuscation` class. 

Here's an example of how you can do this:

```csharp
using IronbrewDeobfuscator;

public class YourClass
{
    public void DeobfuscateScript()
    {
        string source = System.IO.File.ReadAllText("<input file path>");
        bool enableMaxCflowDeobfuscation = false; // Set to true if you want to enable max cflow deobfuscation
        bool isDebug = false; // Set to true if you want to enable debug mode

        Deobfuscation.HandleIb2Deobfuscation(source, enableMaxCflowDeobfuscation, isDebug);
    }
}
```

## Options

- -t<target>: Specify the deobfuscation target. Only 'ib2' is supported.
- -f<file path>: Specify the input file path to the obfuscated script.
- -o<output file path>: Specify the path for the output file.
- --enable-maxcflow: Enables the maximum cflow deobfuscation (unstable).
- --debug: Enables the debug mode. (prints debug information to the console)

## Examples
```bash
# Basic deobfuscation
deobf -t ib2 -f path/to/obfuscated/script.lua -o path/to/deobfuscated/script.luac

# Deobfuscation with maxcflow and debug mode
deobf -t ib2 -f path/to/obfuscated/script.lua -o path/to/deobfuscated/script.luac --enable-maxcflow --debug
```

## Dependencies
- [Loretta](https://github.com/LorettaDevs/Loretta): A Lua parser for .NET.
- [NLua](http://nlua.org): Bridge between Lua world and the .NET.

## Contributing
Contributions are welcome! If you have any ideas, suggestions, or bug reports, please open an issue or create a pull request.

## License
This project is licensed under the GPL-3.0 License.


## Images and Results
A example deobfuscation process that uses `--debug` option
![Capture](https://github.com/Asteriva/IronbrewDeobfuscator/assets/67519722/b9d8db29-3998-4985-bd1c-448df06586e7)

### Obfuscated Script
```lua
print('a', 5, 'c')
for i, v in next, {"g", "h", "i"} do
    print(i, v);
end
```

### Deobfuscated Script
```lua
local r0_0 = "This file is deobfuscated by an open-source Ironbrew Deobfuscator. Please check the GitHub repository for more information."
print("a", 5, "c")
for r3_0, r4_0 in next, {"g","h","i"}, nil do
  print(r3_0, r4_0)
end
```


