[![discord](https://discordapp.com/api/guilds/322630986431201283/widget.png?style=shield)](https://discord.gg/pPhUE9b)
# Phoenix Point Mod Loader

The Phoenix Point Mod Loader (PPML) may be used to easily patch or alter Phoenix Point (PP) game code. It is based off of the BattleTech Mod Loader (https://github.com/BattletechModders/BattleTechModLoader).

The project uses, and expects mods to use, the Harmony library (https://github.com/pardeike/Harmony) to patch game behavior. Modding in its current state consists mainly of assembly patching.

# Installation

Place the contents of the .zip file in your `PhoenixPoint\PhoenixPointWin64_Data\Managed` directory. Run the executable titled `PhoenixPointModLoaderInjector.exe`.

Run the game to have it generate the `Mods` folder for the Mod Loader. Place all mod directories/files into this folder.

# Contributing

All contributions welcome! I intend to enforce a few base guidelines for the repository:

1. Follow MSFT guidelines on code convention. Assume anything not currently following convention is legacy and will be changed.
2. All development happens in the `develop` branch (and preferably in feature branches ala Git Flow).
3. `master` branch is for releases only and may only be PR'd into.

After cloning the repository you will need to put your game assemblies into the `GamePackages` folder. These assemblies are from your `PhoenixPoint/PhoenixPointWin64_Data/Managed` folder. The assemblies that need to be copied are:

* Assembly-CSharp.dll
* UnityEngine.CoreModule.dll

The PPML relies on types from these assemblies and will not build without these references resolved. I have not included the references in the repository to preserve the copyright integrity of Snapshot Games.

# Mod Developer Information

The mod loader will recursively search the `Mods` directory in order to obtain all DLLs it can find. It will then look for all types implementing the `IPhoenixPointMod` interface.

Once all `IPhoenixPointMod` objects have been instantiated and sorted by `ModLoadPriority` in order of `High`, `Normal`, `Low`; the mod loader will execute the `Initialize()` method on each instance.

**NOTE:** Due to the fact that reflection is being used to instantiate mods, please ensure that your types have a default or parameterless constructor defined! Not doing so runs the risk of your mods not being usable.

# Development To-Do List

- [ ] Allow for more robust mod constructors and inject dependencies. (Use an IoC container for this?)
- [ ] More robust mod loading settings. (e.g. disable certain mods)
- [ ] More robust mod metadata. (e.g. game version compatibility, dependencies, etc.)
- [ ] Incorporate functionality of PPDefModifier with @tracktwo's blessing.
- [ ] Allow for asset replacement.
- [ ] Provide config file API for mods.
- [ ] Provide game behavior hooks, if possible.
- [ ] Provide cache of PP game objects (items, weapons, armor, etc.)
- [ ] Provide API for custom UI (such as menus, popups, etc.)
- [ ] [Vortex](https://www.nexusmods.com/about/vortex/) integration?
- [ ] [BepInEx](https://github.com/BepInEx/BepInEx) integration? I don't even know if this is a 'thing'

# Contact

Find me on the Phoenix Point discord under the username `Ijwu`. Please reach out as needed, however do not expect a guarantee of service.
