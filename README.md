# PPGModCompiler
An open-sourced version of the People Playground Mod Compiler ("PPGMC" or "PPGModCompiler").
**People Playground (PPG) and PPGMC was made by Studio Minus (https://studiominus.nl)
### THIS IS A WIP I HAVEN'T FINISHED IT YET LOL
# Sources
* 95% code from dnSpy (on PPGModCompiler.dll from game)
* 5% code from Visual Studio's Assembly Explorer thingy
# Credits
All credits go to Studio Minus for the code of both PPG and PPGMC.
I'm just a person who decompiled it into a Visual Studio project for cross-platform building.
# How to Use
* Download [Visual Studio][vs] and [.NET 5.0][net50]
* Grab the [source code for this repository][source] in a .ZIP file, or use git (`git clone https://github.com/StupidRepo/PPGModCompiler`)
* Open project in Visual Studio, and then find the `Build -> Build Solution` button.
* Next, find the executable by looking in either `bin/Release/net5.0` or `bin/Debug/net5.0`, and run it.
* It will ask you where your People Playground folder is.
    - To find the folder, see "Finding your PPG Folder"
* Open People Playground, and PPG should start compiling mods via the custom server. The first mod may fail to compile with the "asyncronous timeout" message. This is normal, and you can fix it by going to PPG -> Mods -> Finding the mod -> Press Recompile.
If it doesn't work, and you only see this with NOTHING ELSE:
```
[Info] [WatsonTcpServer] starting on 127.0.0.1:3251
Started listening on 127.0.0.1:3251
```
Then you may need to restart PPG. If that doesn't fix it, [make a new issue][newi].
If you get an error message such as:
```
Sent Error: Assembly referencing error: Could not find a part of the path '[path input by user]People Playground_Data/Managed/netstandard.dll'. to 127.0.0.1:59251
```
Then you may need to restart the server, and check that the path you provided was correct (don't forget that your path needs to have a space at the end!)
# Finding your PPG Folder
Go to Steam, and find People Playground in your Library. Right-click it and (wait for more insturctions and links hold on)