using Newtonsoft.Json;
using PhoenixPointModLoader.Config;
using PhoenixPointModLoader.Exceptions;
using PhoenixPointModLoader.Infrastructure;
using PhoenixPointModLoader.Mods;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PhoenixPointModLoader.Manager
{
	public class ModManager
	{
		private Container _container;
		private IFileConfigProvider _metadataProvider;
		private FileSystemModLoader _modLoader;

		public string ModsDirectory { get; }
		public IList<ModEntry> Mods { get; private set; }

		public ModManager(string modsDirectory, IFileConfigProvider metadataProvider, FileSystemModLoader modLoader, Container container)
		{
			ModsDirectory = modsDirectory;
			_metadataProvider = metadataProvider;
			_modLoader = modLoader;
			_container = container;
		} 

		public void Initialize()
		{
			IList<Type> mods = _modLoader.LoadModTypesFromDirectory(ModsDirectory);
			IList<ModEntry> modsWithMetadata = LoadMetadataForModTypes(mods);
			IList<ModEntry> modsWithResolvedDependencies = ResolveDependencies(modsWithMetadata);
			Mods = InstantiateMods(modsWithResolvedDependencies);
			AddDefaultModEntries(Mods);
			foreach (ModEntry mod in Mods)
			{
				mod.ModInstance.Initialize();
				Logger.Log($"Successfully initialized mod `{mod.ModMetadata.Name} (v{mod.ModMetadata.Version})`.");
			}
		}

		private void AddDefaultModEntries(IList<ModEntry> mods)
		{
			mods.Add(new ModEntry(new EnableConsoleMod(_metadataProvider), null, new ModMetadata("Enable Console", new Version(1,0), null)));
			mods.Add(new ModEntry(new LoadConsoleCommandsFromAllAssembliesMod(), null, new ModMetadata("Load Mod Commands", new Version(1, 0), null)));
		}

		private IList<ModEntry> LoadMetadataForModTypes(IList<Type> mods)
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

		private IList<ModEntry> ResolveDependencies(IList<ModEntry> modsWithMetadata)
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
				IList<ModMetadata> currentlyResolvedMetadata = successfullyResolved.Select(x => x.ModMetadata).ToList();
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

		private IList<ModEntry> InstantiateMods(IList<ModEntry> modEntries)
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
	}
}