using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using PhoenixPointModLoader.Mods;
using static PhoenixPointModLoader.Logger;

namespace PhoenixPointModLoader
{
	public static class PhoenixPointModLoader
	{
		private const BindingFlags PUBLIC_STATIC_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static;
		private static readonly List<string> IGNORE_FILE_NAMES = new List<string>()
		{
			"0Harmony.dll",
			"PhoenixPointModLoader.dll"
		};

		public static string ModDirectory { get; private set; }

	

		public static void Initialize()
		{
			string manifestDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
				?? throw new InvalidOperationException("Manifest path is invalid.");


			// this should be (wherever Phoenix Point is Installed)\PhoenixPoint\PhoenixPointWin64_Data\Managed
			ModDirectory = Path.GetFullPath(
				Path.Combine(manifestDirectory, Path.Combine(@"..\..\Mods")));

			LogPath = Path.Combine(ModDirectory, "PPModLoader.log");

			Version PPMLVersion = Assembly.GetExecutingAssembly().GetName().Version;

			if (!Directory.Exists(ModDirectory))
				Directory.CreateDirectory(ModDirectory);

			// create log file, overwriting if it's already there
			using (var logWriter = File.CreateText(LogPath))
			{
				logWriter.WriteLine($"PPModLoader -- PPML v{PPMLVersion} -- {DateTime.Now}");
			}

			// ReSharper disable once UnusedVariable
			var harmony = HarmonyInstance.Create("io.github.realitymachina.PPModLoader");

			// get all dll paths
			List<string> dllPaths = Directory.GetFiles(ModDirectory, "*.dll", SearchOption.AllDirectories).ToList();

			if (!dllPaths.Any())
			{
				Log(@"No .DLLs loaded. DLLs must be placed in the root of the folder \PhoenixPoint\Mods\.");
				return;
			}

			List<IPhoenixPointMod> allMods = new List<IPhoenixPointMod>();
			IncludeDefaultMods(allMods);
			foreach (var dllPath in dllPaths)
			{
				if (!IGNORE_FILE_NAMES.Contains(Path.GetFileName(dllPath)))
					allMods.AddRange(LoadDll(dllPath));
			}

			InitializeMods(allMods);	
		}

		private static void IncludeDefaultMods(List<IPhoenixPointMod> allMods)
		{
			allMods.AddRange(new IPhoenixPointMod[]
			{
				new EnableConsoleMod(),
				new LoadConsoleCommandsFromAllAssembliesMod()
			});
		}

		private static void InitializeMods(List<IPhoenixPointMod> allMods)
		{
			var prioritizedModList = allMods.ToLookup(x => x.Priority);
			ModLoadPriority[] loadOrder = new[] { ModLoadPriority.High, ModLoadPriority.Normal, ModLoadPriority.Low };
			foreach (ModLoadPriority priority in loadOrder)
			{
				Log("Attempting to initialize `{0}` priority mods.", priority.ToString());
				foreach (var mod in prioritizedModList[priority])
				{
					try
					{
						mod.Initialize();
						Log("Mod class `{0}` from DLL `{1}` was successfully initialized.",
							mod.GetType().Name,
							Path.GetFileName(mod.GetType().Assembly.Location));
					}
					catch (Exception e)
					{
						Log("Mod class `{0}` from DLL `{1}` failed to initialize.",
							mod.GetType().Name,
							Path.GetFileName(mod.GetType().Assembly.Location));
						Log(e.ToString());
					}
				}
			}
		}

		private static IList<IPhoenixPointMod> LoadDll(string path)
		{
			string originalDirectory = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(path);
			Assembly mod = Assembly.LoadFile(path);
			Type[] allClasses = mod.GetTypes();
			List<Type> modClasses = allClasses.Where(x => x.GetInterface("IPhoenixPointMod") != null).ToList();

			if (!modClasses.Any())
			{
				Log("No mod classes found in DLL: {0}", Path.GetFileName(path));
				return new List<IPhoenixPointMod>();
			}

			var modInstances = new List<IPhoenixPointMod>();
			foreach (Type modClass in modClasses)
			{
				IPhoenixPointMod modInstance = null;
				try
				{
					modInstance = Activator.CreateInstance(modClass) as IPhoenixPointMod;
				}
				catch (Exception e)
				{
					Log("Error has occurred when instantiating mod class`{0}` in DLL `{1}`.", modClass.Name, Path.GetFileName(path));
					Log(e.ToString());
					continue;
				}

				if (modInstance == null)
				{
					Log("Instantiated mod class `{0}` from DLL `{1}` was null for unknown reason.", modClass.Name, Path.GetFileName(path));
				}

				modInstances.Add(modInstance);
			}
			Environment.CurrentDirectory = originalDirectory;
			return modInstances;
		}
	}
}

