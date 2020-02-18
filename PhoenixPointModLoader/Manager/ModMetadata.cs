using System;
using System.Collections.Generic;

namespace PhoenixPointModLoader.Manager
{
	public class ModMetadata
	{
		public ModMetadata(string name) : this(name, new Version(0, 0, 0, 0), string.Empty) { }

		public ModMetadata(string name, Version version) : this(name, version, string.Empty) { }

		public ModMetadata(string name, Version version, string description)
		{
			Name = name;
			Version = version;
			Description = description;
			Dependencies = new List<ModMetadata>();
		}

		public string Name { get; }
		public string Description { get; }
		public Version Version { get; }

		public IList<ModMetadata> Dependencies { get; }

		public bool TryResolveDependencies(IEnumerable<ModMetadata> loadedMods)
		{
			List<ModMetadata> resolvedMods = new List<ModMetadata>();
			List<ModMetadata> unresolvedMods = new List<ModMetadata>() { this };

			bool ResolveDependenciesForMod(ModMetadata mod)
			{
				foreach (ModMetadata dependency in Dependencies)
				{
					if (!resolvedMods.Contains(dependency))
					{
						if (unresolvedMods.Contains(dependency))
						{
							throw new ModCircularDependencyException(this, dependency);
						}
						ResolveDependenciesForMod(dependency);
					}
				}
				resolvedMods.Add(mod);
				unresolvedMods.Remove(mod);
				return true;
			}

			return ResolveDependenciesForMod(this);
		}
	}
}
