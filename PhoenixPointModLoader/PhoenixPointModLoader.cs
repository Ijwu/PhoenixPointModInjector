using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PhoenixPointModLoader.Infrastructure;
using PhoenixPointModLoader.Mods;
using SimpleInjector;

namespace PhoenixPointModLoader
{
	public static class PhoenixPointModLoader
	{
		private static readonly List<string> IgnoredFiles = new List<string>()
		{
			"0Harmony.dll",
			"PhoenixPointModLoader.dll"
		};
		private static Container Container = new Container();

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

			CompositionRoot.ConfigureContainer(Container);

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
				if (!IgnoredFiles.Contains(Path.GetFileName(dllPath)))
					allMods.AddRange(LoadDll(dllPath));
			}
			return allMods;
		}

		private static void IncludeDefaultMods(IList<IPhoenixPointMod> allMods)
		{
			allMods.Add(Container.GetInstance<EnableConsoleMod>());
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
					LegacyPhoenixPointMod legacyMod = mod as LegacyPhoenixPointMod;
					Type modType = legacyMod != null ? legacyMod.ModClass : mod.GetType();
					try
					{
						mod.Initialize();
						Logger.Log("Mod class `{0}` from DLL `{1}` was successfully initialized.",
							modType.Name, Path.GetFileName(modType.Assembly.Location));
					}
					catch (Exception e)
					{
						Logger.Log("Mod class `{0}` from DLL `{1}` failed to initialize.",
							modType.Name, Path.GetFileName(modType.Assembly.Location));
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

			var modInstances = new List<IPhoenixPointMod>();
			foreach (Type modClass in allClasses)
			{
				if (modClass.GetInterface("IPhoenixPointMod") != null)
				{
					IPhoenixPointMod modInstance;
					try
					{
						modInstance = Container.GetInstance(modClass) as IPhoenixPointMod;
					}
					catch (Exception e)
					{
						Logger.Log("Error has occurred when instantiating mod class`{0}` in DLL `{1}`.", modClass.Name, Path.GetFileName(path));
						Logger.Log(e.ToString());
						continue;
					}

					if (modInstance != null)
					{
						modInstances.Add(modInstance);
					}
					else
					{
						Logger.Log("Instantiated mod class `{0}` from DLL `{1}` was null for unknown reason. " +
							"Please ensure you have a default constructor defined for your type.", modClass.Name, Path.GetFileName(path));
					}
				}
				else
				{
					MethodInfo initMethod = modClass.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
					if (initMethod != null)
					{
						modInstances.Add(new LegacyPhoenixPointMod(modClass, initMethod));
					}
				}
			}
			if (!modInstances.Any())
			{
				Logger.Log("No mod classes found in DLL: {0}", Path.GetFileName(path));
			}
			Environment.CurrentDirectory = originalDirectory;
			return modInstances;
		}
	}
}

