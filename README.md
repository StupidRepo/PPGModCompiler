# PPGModCompiler
An open-sourced version of the People Playground Mod Compiler ("PPGMC" or "PPGModCompiler").
**People Playground (PPG) and PPGMC was made by Studio Minus (https://studiominus.nl)**
# Sources
* 95% code from dnSpy (on PPGModCompiler.dll from game)
* 5% code from Visual Studio's Assembly Explorer thingy
# Credits
All credits go to Studio Minus for the code of both PPG and PPGMC.
I'm just a person who decompiled it into a Visual Studio project for cross-platform building.
# How to Use
* Download [Visual Studio][vs] and [.NET 5.0][net50].
* Grab the [source code for this repository][source] in a .ZIP file, or use git (`git clone https://github.com/StupidRepo/PPGModCompiler`).
* Open project in Visual Studio, and then find the `Build -> Build Solution` button.
* Next, find the executable by looking in either `bin/Release/net5.0` or `bin/Debug/net5.0`, and run it.
* It will ask you where your People Playground folder is.
    - To find the folder, see "[Finding your PPG Folder](#finding-your-ppg-folder)".
* Open People Playground, and PPG should start compiling mods via the custom server. The first mod may fail to compile with the "asyncronous timeout" message. This is normal, and you can fix it by finding the mod that failed in the mod list, and pressing Recompile.
# Issues
If it doesn't work, and you only see this with NOTHING ELSE...:
```
[Info] [WatsonTcpServer] starting on 127.0.0.1:3251
Started listening on 127.0.0.1:3251
```
...then you may need to restart PPG.

If you get an error message such as:
```
Sent Error: Assembly referencing error: Could not find a part of the path '[path input by user]People Playground_Data/Managed/netstandard.dll'. to 127.0.0.1:59251
```
Then you may need to restart the server, and check that the path you provided was correct (don't forget that your path needs to have a space at the end!)

If you're still having errors occur (either whilst building or running), [make a new issue][newi] and I'll respond ASAP.
# Contribution
Contribution is allowed and I recommend you do contribute. I'll accept PRs that:
- Patch security issues/fix vulnerable code
- Make compilation more efficient/reducing amount of code whilst being efficient
- Fix issues in other areas (such as this README file)

I'll ignore PRs that are:
- AI generated
- Adding unwanted or generally uneeded code
- Unoriginal and not creative
# Finding your PPG Folder
1. Go to Steam, and find People Playground in your Library.
2. Right-click it, and press/hover over "Manage".
3. Press "Browse local files", and then somehow copy the path.
4. Make sure the path is something like this:
    - `C:/[program files thingy]/[steam dir with steamapps]/People Playground/`
5. MAKE SURE THE PATH HAS A SLASH AT THE END! THIS IS SO IMPORTANT!

[newi]: https://github.com/StupidRepo/PPGModCompiler/issues
[source]: https://github.com/StupidRepo/PPGModCompiler/archive/refs/heads/main.zip
[net50]: https://dotnet.microsoft.com/en-us/download/dotnet/5.0
[vs]: https://visualstudio.microsoft.com/downloads/