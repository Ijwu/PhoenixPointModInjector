using PhoenixPointModLoader.Config;
using PhoenixPointModLoader.Exceptions;
using PhoenixPointModLoader.Mods;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhoenixPointModLoader.Manager
{
	public class ModManager
	{
		private Container _container;
		private IFileConfigProvider _metadataProvider;
		private FileSystemModLoader _modLoader;

		public string ModsDirectory { get; }
		public List<ModEntry> Mods { get; private set; }

		public ModManager(string modsDirectory, IFileConfigProvider metadataProvider, FileSystemModLoader modLoader, Container container)
		{
			ModsDirectory = modsDirectory;
			_metadataProvider = metadataProvider;
			_modLoader = modLoader;
			_container = container;
		} 

		public void Initialize()
		{
			List<Type> mods = _modLoader.LoadModTypesFromDirectory(ModsDirectory);
			List<ModEntry> modsWithMetadata = LoadMetadataForModTypes(mods);
			List<ModEntry> modsWithResolvedDependencies = ResolveDependencies(modsWithMetadata);
			Mods = InstantiateMods(modsWithResolvedDependencies);
			AddDefaultModEntries(Mods);
			LoadLegacyMods(Mods);
			InitializeMods(Mods);
		}

		private void AddDefaultModEntries(List<ModEntry> mods)
		{
			mods.Add(new ModEntry(new EnableConsoleMod(_metadataProvider), null, new ModMetadata("Enable Console", new Version(1,0), null)));
			mods.Add(new ModEntry(new LoadConsoleCommandsFromAllAssembliesMod(), null, new ModMetadata("Load Mod Commands", new Version(1, 0), null)));
		}

		private List<ModEntry> LoadMetadataForModTypes(List<Type> mods)
		{
			var result = new List<ModEntry>();
			foreach (Type mod in mods)
			{
				try
				{
					string metadataPath = Path.Combine(
						Path.GetDirectoryName(mod.Assembly.Location), 
						$"{Path.GetFileNameWithoutExtension(mod.Assembly.Location)}.json");
					ModMetadata metadata = _metadataProvider.Read<ModMetadata>(metadataPath);
					if (metadata is null)
					{
						metadata = new ModMetadata(mod.FullName, new Version(0, 0));
					}
					result.Add(new ModEntry(mod, metadata));
				}
				catch (FileNotFoundException)
				{
					ModMetadata metadata = new ModMetadata(mod.FullName, new Version(0, 0));
					result.Add(new ModEntry(mod, metadata));
				}
			}
			return result;
		}

		private List<ModEntry> ResolveDependencies(List<ModEntry> modsWithMetadata)
		{
			List<ModEntry> successfullyResolved = new List<ModEntry>();
			var dependencyResolutionList = modsWithMetadata.OrderBy(entry => entry.ModMetadata?.Dependencies?.Count);
			foreach (ModEntry entry in dependencyResolutionList)
			{
				if (entry.ModMetadata.Dependencies is null || entry.ModMetadata.Dependencies.Count == 0)
				{
					successfullyResolved.Add(entry);
					continue;
				}
				List<ModMetadata> currentlyResolvedMetadata = successfullyResolved.Select(x => x.ModMetadata).ToList();
				currentlyResolvedMetadata.Add(entry.ModMetadata);
				bool resolved = entry.ModMetadata.TryResolveDependencies(currentlyResolvedMetadata);
				if (resolved)
				{
					successfullyResolved.Add(entry);
				}
				else
				{
					var dependenciesString = string.Join(", ", entry.ModMetadata.Dependencies.Select(x => $"{x.Name} (v{x.Version})"));
				}
			}
			return successfullyResolved;
		}

		private List<ModEntry> InstantiateMods(List<ModEntry> modEntries)
		{
			var instantiatedEntries = new List<ModEntry>();
			foreach (ModEntry entry in modEntries)
			{
				try
				{
					IPhoenixPointMod modInstance = _container.GetInstance(entry.ModType) as IPhoenixPointMod;
					if (modInstance == null)
					{
						throw new ModLoadFailureException($"Mod `{entry.ModType.FullName}` failed to initialize for unknown reason.");
					}

					instantiatedEntries.Add(new ModEntry(modInstance, entry.ModType, entry.ModMetadata));		
				}
				catch (Exception e)
				{
					Logger.Log($"Mod `{entry.ModMetadata.Name} (v{entry.ModMetadata.Version})` failed to initialize.");
					Logger.Log($"{e}");
					continue;
				}
			}
			return instantiatedEntries;
		}

		private IEnumerable<ModEntry> InstantiateLegacyMods(List<ModEntry> modEntries)
		{
			var instantiatedEntries = new List<ModEntry>();
			foreach (ModEntry entry in modEntries)
			{
				try
				{
					IPhoenixPointMod modInstance = new LegacyPhoenixPointMod(entry.ModType);
					if (modInstance == null)
					{
						throw new ModLoadFailureException($"Legacy Mod `{entry.ModType.FullName}` failed to initialize for unknown reason.");
					}

					instantiatedEntries.Add(new ModEntry(modInstance, entry.ModType, entry.ModMetadata));
				}
				catch (Exception e)
				{
					Logger.Log($"Legacy Mod `{entry.ModMetadata.Name} (v{entry.ModMetadata.Version})` failed to initialize.");
					Logger.Log($"{e}");
					continue;
				}
			}
			return instantiatedEntries;
		}

		private void LoadLegacyMods(List<ModEntry> modList)
		{
			List<Type> legacyModTypes = _modLoader.LoadLegacyModsFromDirectory(ModsDirectory);
			List<ModEntry> modsWithMetadata = LoadMetadataForModTypes(legacyModTypes);
			modList.AddRange(InstantiateLegacyMods(modsWithMetadata));
		}

		private void InitializeMods(List<ModEntry> mods)
		{
			ModLoadPriority[] priorityOrder = new[] { ModLoadPriority.High, ModLoadPriority.Normal, ModLoadPriority.Low };
			foreach (ModLoadPriority priority in priorityOrder)
			{
				Logger.Log($"Loading `{priority}` priority mods.");
				IEnumerable<ModEntry> prioritizedMods = mods.Where(mod => mod.ModInstance.Priority == priority);
				foreach (ModEntry mod in prioritizedMods)
				{
					mod.ModInstance.Initialize();
					Logger.Log($"Successfully initialized mod `{mod.ModMetadata.Name} (v{mod.ModMetadata.Version})`.");
				}
			}
		}
	}
}