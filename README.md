# DDSTerminalExtensions

A mod for [Deadeye Deepfake Simulacrum](https://www.deaddeepsim.com/) that introduces some terminal QoL changes.

## But what does it do?

### Terminal QoL
- Allows navigating left and right in the input to modify mid-input
- Adds empty values at the start and end of the terminal's history
- Clears input on CTRL+C
- Modifies the input caret to work with the new input

### New Commands
- `helpsecret` for secret commands built into the game
- `helpext` for commands built into this mod
- Commands to dump the map (will be used in a later tool of mine)
- Commands for teleportation

## Installing
First we need to install BepInEx. On Linux, follow the [official instructions](https://docs.bepinex.dev/articles/user_guide/installation/index.html?tabs=tabid-nix).

On Windows, download [BepInEx5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.22) for your computer, and extract the files into the games directory.
Make sure the `BepInEx` folder and the game executable `DeadeyeDeepfakeSimulacrum.exe` are sharing the same folder.

Launch the game once until the main menu is visible, then close it again.
A `plugins` folder should have appeared in the `BepInEx` folder, drag the [`DDSTerminalExtension.dll`](https://github.com/sw1tchbl4d3r/DDSTerminalExtensions/releases) into this directory.

And that's it! Verify the mod is running by using the `help` command.

## Building
Should be as simple as `dotnet build -c Release`, after you've changed the references in the cproj of the `Assembly-CSharp.dll` and others to your games folder.
Alternatively make your IDE do both of those things.
