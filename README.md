# Ironbrew Deobfuscator

Ironbrew Deobfuscator is a tool designed to deobfuscate scripts obfuscated with Ironbrew2.

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/Asteriva/IronbrewDeobfuscator)
![GitHub issues](https://img.shields.io/github/issues/Asteriva/IronbrewDeobfuscator)

## Overview

- **Version:** beta-1.0

## Table of Contents

- [Installation](#installation)
- [Command-Line Interface](#command-line-interface) 
- [Options](#options)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

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

## Options

- -t<target>: Specify the deobfuscation target. Only 'ib2' is supported.
- -f<file path>: Specify the input file path to the obfuscated script.
- --enable-maxcflow: Enables the maximum cflow deobfuscation (unstable).
- --debug: Enables the debug mode.

## Examples
```bash
# Basic deobfuscation
deobf -t ib2 -f path/to/obfuscated/script.lua

# Deobfuscation with maxcflow and debug mode
deobf -t ib2 -f path/to/obfuscated/script.lua --enable-maxcflow --debug
```

Dependencies
[Loretta](https://github.com/LorettaDevs/Loretta): A Lua parser for .NET.
[NLua](http://nlua.org): Bridge between Lua world and the .NET.

## Contributing
Contributions are welcome! If you have any ideas, suggestions, or bug reports, please open an issue or create a pull request.

## License
This project is licensed under the GPL-3.0 License.




