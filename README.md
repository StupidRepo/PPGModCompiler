# PPGModCompiler
An open-sourced version of the People Playground Mod Compiler ("PPGMC" or "PPGModCompiler"). This works on Apple Silicon Chip Macs (M1 MBP, M1 MBA, etc.)
**People Playground (PPG) and PPGMC was made by Studio Minus (https://studiominus.nl)**

# How to Use
* Download [.NET 6.0][net60], and install it.
* Grab the [source code for this repository][source] as a .ZIP file & extract it, or use git (`git clone https://github.com/StupidRepo/PPGModCompiler`).
* Run `build.sh`, if you're on a Linux or Mac machine.
* After it's finished, find the executable by looking in the `out` folder, and run it.
* The executable may ask you where your Steam folder is, if it couldn't find it by default.
* Open People Playground, and PPG should start compiling mods via the custom server. The first mod may fail to compile with the "asyncronous timeout" message. This is normal, and you can fix it by finding the mod that failed in the mod list, and pressing Recompile.

# Issues
If errors are occuring, either whilst building or running, [make a new issue][newi] and I'll respond ASAP.

# Contribution
Contribution is allowed and I recommend you do contribute. I'll accept PRs that:
- Patch security issues/fix vulnerable code
- Make compilation more efficient/reducing amount of code whilst being efficient
- Fix issues in other areas (such as this README file)

I'll ignore PRs that are:
- AI generated
- Adding unwanted or generally uneeded code
- Unoriginal and not creative

# Sources
* 100% Code from Visual Studio's Assembly Explorer thingy (on PPGModCompiler.dll from game)
    - Most of the variables were renamed to be more human-readable.

# Credits
All credits go to Studio Minus for the code of both PPG and PPGMC.
I'm just a person who decompiled it into a Visual Studio project for cross-platform building.

[newi]: https://github.com/StupidRepo/PPGModCompiler/issues
[source]: https://github.com/StupidRepo/PPGModCompiler/archive/refs/heads/main.zip
[net60]: https://dotnet.microsoft.com/en-us/download/dotnet/6.0
[vs]: https://visualstudio.microsoft.com/downloads/
