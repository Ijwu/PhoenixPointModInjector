using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using PhoenixPointModLoader.Mods;

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
				?? throw new InvalidOperationException("Could not determine operating directory. Is your folder structure correct? " +
				"Try verifying game files in the Epic Games Launcher, if you're using it.");

			ModDirectory = Path.GetFullPath(Path.Combine(manifestDirectory, Path.Combine(@"..\..\Mods")));

			if (!Directory.Exists(ModDirectory))
				Directory.CreateDirectory(ModDirectory);

			Logger.InitializeLogging(Path.Combine(ModDirectory, "PPModLoader.log"));

			IList<IPhoenixPointMod> allMods = RetrieveAllMods();
			InitializeMods(allMods);
		}

		private static IList<IPhoenixPointMod> RetrieveAllMods()
		{
			List<string> dllPaths = Directory.GetFiles(ModDirectory, "*.dll", SearchOption.AllDirectories).ToList();
			List<IPhoenixPointMod> allMods = new List<IPhoenixPointMod>();
			IncludeDefaultMods(allMods);
			foreach (var dllPath in dllPaths)
			{
				if (!IGNORE_FILE_NAMES.Contains(Path.GetFileName(dllPath)))
					allMods.AddRange(LoadDll(dllPath));
			}
			return allMods;
		}

		private static void IncludeDefaultMods(IList<IPhoenixPointMod> allMods)
		{
			allMods.Add(new EnableConsoleMod());
			allMods.Add(new LoadConsoleCommandsFromAllAssembliesMod());
		}

		private static void InitializeMods(IList<IPhoenixPointMod> allMods)
		{
			var prioritizedModList = allMods.ToLookup(x => x.Priority);
			ModLoadPriority[] loadOrder = new[] { ModLoadPriority.High, ModLoadPriority.Normal, ModLoadPriority.Low };
			foreach (ModLoadPriority priority in loadOrder)
			{
				Logger.Log("Attempting to initialize `{0}` priority mods.", priority.ToString());
				foreach (var mod in prioritizedModList[priority])
				{
					try
					{
						mod.Initialize();
						Logger.Log("Mod class `{0}` from DLL `{1}` was successfully initialized.",
							mod.GetType().Name,
							Path.GetFileName(mod.GetType().Assembly.Location));
					}
					catch (Exception e)
					{
						Logger.Log("Mod class `{0}` from DLL `{1}` failed to initialize.",
							mod.GetType().Name,
							Path.GetFileName(mod.GetType().Assembly.Location));
						Logger.Log(e.ToString());
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
				Logger.Log("No mod classes found in DLL: {0}", Path.GetFileName(path));
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
					Logger.Log("Error has occurred when instantiating mod class`{0}` in DLL `{1}`.", modClass.Name, Path.GetFileName(path));
					Logger.Log(e.ToString());
					continue;
				}

				if (modInstance == null)
				{
					Logger.Log("Instantiated mod class `{0}` from DLL `{1}` was null for unknown reason. " +
						"Please ensure you have a default constructor defined for your type.", modClass.Name, Path.GetFileName(path));
				}

				modInstances.Add(modInstance);
			}
			Environment.CurrentDirectory = originalDirectory;
			return modInstances;
		}
	}
}

