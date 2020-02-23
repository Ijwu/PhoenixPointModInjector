using System;
using System.Collections.Generic;
using System.Linq;

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

		public bool TryResolveDependencies(IList<ModMetadata> modList)
		{
			List<ModMetadata> resolvedMods = new List<ModMetadata>();
			List<ModMetadata> unresolvedMods = new List<ModMetadata>();

			bool ResolveDependenciesForMod(ModMetadata mod)
			{
				bool result = false;

				if (!modList.Contains(mod))
				{
					return false;
				}

				if (mod.Dependencies.Count == 0)
				{
					return true;
				}

				unresolvedMods.Add(mod);
				foreach (ModMetadata dependency in mod.Dependencies)
				{
					if (!resolvedMods.Any(resolved => resolved.Name == dependency.Name && resolved.Version == dependency.Version))
					{
						if (unresolvedMods.Any(unresolved => unresolved.Name == dependency.Name && unresolved.Version == dependency.Version))
						{
							throw new ModCircularDependencyException(this, dependency);
						}
						result = ResolveDependenciesForMod(dependency);
					}
					else
					{
						result = true;
					}
				}
				resolvedMods.Add(mod);
				unresolvedMods.Remove(mod);
				return result;
			}

			return ResolveDependenciesForMod(this);
		}
	}
}
