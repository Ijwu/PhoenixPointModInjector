using Newtonsoft.Json;
using PhoenixPointModLoader.Config;
using PhoenixPointModLoader.Infrastructure;
using PhoenixPointModLoader.Mods;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PhoenixPointModLoader.Manager
{
	public class ModManager
	{
		private Container _container;
		private IFileConfigProvider _metadataProvider;

		public string ModsDirectory { get; }

		public ModManager(string modsDirectory, IFileConfigProvider metadataProvider)
		{
			ModsDirectory = modsDirectory;
			_metadataProvider = metadataProvider;
		}

		public void Initialize()
		{
			_container = CompositionRoot.GetContainer();

			IList<ModEntry> allMods = RetrieveAllMods();
			IList<ModEntry> dependencyResolvedModList = SolveForDependencies(allMods);
			InitializeMods(dependencyResolvedModList);
		}

		private IList<ModEntry> SolveForDependencies(IList<ModEntry> allMods)
		{
			Logger.Log("Attempting to resolve mod dependencies...");
			List<ModEntry> resolvedModsList = new List<ModEntry>(allMods);
			List<ModEntry> toBeRemoved = new List<ModEntry>();
			foreach (ModEntry mod in allMods)
			{
				try
				{
					bool resolved = mod.ModMetadata.TryResolveDependencies(allMods.Select(x =>x.ModMetadata).ToList());
					if (!resolved)
					{
						LogFailedDependencyResolution(mod);
						toBeRemoved.Add(mod);
					}
				}
				catch (ModCircularDependencyException)
				{
					LogCircularDependency(mod);
					toBeRemoved.Add(mod);
				}
			}
			resolvedModsList.RemoveAll((x) => toBeRemoved.Contains(x));
			return resolvedModsList;
		}

		private IList<ModEntry> RetrieveAllMods()
		{
			List<string> dllPaths = Directory.GetFiles(ModsDirectory, "*.dll", SearchOption.AllDirectories).ToList();
			List<ModEntry> allMods = new List<ModEntry>();
			IncludeDefaultMods(allMods);
			foreach (var dllPath in dllPaths)
			{
				allMods.AddRange(LoadDll(dllPath));
			}
			return allMods;
		}

		private void IncludeDefaultMods(IList<ModEntry> allMods)
		{
			allMods.Add(new ModEntry(
					_container.GetInstance<EnableConsoleMod>(),
					new ModMetadata("Enable Console", new Version(1, 0, 0, 0), "Turns on the in-game console.")
				));
			allMods.Add(new ModEntry(
					_container.GetInstance<LoadConsoleCommandsFromAllAssembliesMod>(),
					new ModMetadata("Load Console Commands", new Version(1, 0, 0, 0), "Enables mods to provide console commands.")
				));
		}

		private void InitializeMods(IList<ModEntry> allMods)
		{
			ModLoadPriority[] loadOrder = new[] { ModLoadPriority.High, ModLoadPriority.Normal, ModLoadPriority.Low };

			var prioritizedModList = allMods.ToLookup(x => x.ModInstance.Priority);

			foreach (ModLoadPriority priority in loadOrder)
			{
				Logger.Log("Attempting to initialize `{0}` priority mods.", priority.ToString());
				foreach (var mod in prioritizedModList[priority])
				{
					try
					{
						mod.ModInstance.Initialize();
						LogModLoaded(mod.ModInstance.GetType());
					}
					catch (Exception e)
					{
						LogModLoadFailure(mod.ModInstance.GetType(), e);
					}
				}
			}
		}

		private IList<ModEntry> LoadDll(string path)
		{
			string originalDirectory = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(path);

			IList<Type> modClasses = GetModClasses(path);

			if (!modClasses.Any())
			{
				Logger.Log("No mod classes found in DLL: {0}", Path.GetFileName(path));
				return new List<ModEntry>();
			}

			var modInstances = new List<ModEntry>();
			foreach (Type modClass in modClasses)
			{
				ModMetadata modMetadata;
				IPhoenixPointMod modInstance;
				try
				{
					modInstance = _container.GetInstance(modClass) as IPhoenixPointMod;
					if (!TryLoadMetadata(modClass.Name, out modMetadata))
					{
						modMetadata = new ModMetadata(modClass.Name);
						LogMissingMetadata(modClass);
					}
				}
				catch (Exception e)
				{
					LogModLoadFailure(modClass, e);
					continue;
				}

				if (modInstance == null)
				{
					LogNullInstance(path, modClass);
					continue;
				}

				modInstances.Add(new ModEntry(modInstance, modMetadata));
			}
			Environment.CurrentDirectory = originalDirectory;
			return modInstances;
		}

		private bool TryLoadMetadata(string modName, out ModMetadata metadata)
		{
			_metadataProvider.RelativeFilePath = $"{modName}.mod";
			try
			{
				metadata = _metadataProvider.Read<ModMetadata>();
				return true;
			}
			catch (FileNotFoundException)
			{
				metadata = null;
				return false;
			}
		}

		private static IList<Type> GetModClasses(string path)
		{
			Assembly modssembly = Assembly.LoadFile(path);
			Type[] allClasses = modssembly.GetTypes();
			return allClasses.Where(x => x.GetInterface("IPhoenixPointMod") != null).ToList();
		}

		private static void LogModLoadFailure(Type modType, Exception e)
		{
			Logger.Log("Mod class `{0}` from DLL `{1}` failed to initialize.",
										modType.Name,
										Path.GetFileName(modType.Assembly.Location));
			Logger.Log(e.ToString());
		}

		private static void LogModLoaded(Type modType)
		{
			Logger.Log("Mod class `{0}` from DLL `{1}` was successfully initialized.",
										modType.Name,
										Path.GetFileName(modType.Assembly.Location));
		}

		private static void LogMissingMetadata(Type modClass)
		{
			Logger.Log($"Could not find mod metadata file for mod `{modClass.Name}`. " +
										"Assuming default metadata which may cause unintended behavior. " +
										"Please ship your mods with a metadata file to prevent this.");
		}

		private static void LogNullInstance(string path, Type modClass)
		{
			Logger.Log("Instantiated mod class `{0}` from DLL `{1}` was null for unknown reason.",
					   modClass.Name,
					   Path.GetFileName(path));
		}

		private void LogFailedDependencyResolution(ModEntry mod)
		{
			Logger.Log($"Failed to resolve dependencies for mod `{mod.ModMetadata.Name}`.");
			Logger.Log($"Dependencies for mod `{mod.ModMetadata.Name}`: `{string.Join(", ", mod.ModMetadata.Dependencies.Select(x => x.Name))}`.");
		}

		private void LogCircularDependency(ModEntry mod)
		{
			Logger.Log($"Could not load mod `{mod.ModMetadata.Name}` due to circular dependencies. " +
				$"Please check your metadata definitions for mods which might mutually depend on one another.");
		}
	}
}
