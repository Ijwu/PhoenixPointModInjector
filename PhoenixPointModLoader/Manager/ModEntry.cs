using System;

namespace PhoenixPointModLoader.Manager
{
	public class ModEntry
	{
		public Type ModType { get; }
		public IPhoenixPointMod ModInstance { get; }
		public ModMetadata ModMetadata { get; }

		public ModEntry(Type modType, ModMetadata modMetadata) : this(null, modType, modMetadata) { }

		public ModEntry(IPhoenixPointMod modInstance, Type modType, ModMetadata modMetadata)
		{
			ModInstance = modInstance;
			ModType = modType;
			ModMetadata = modMetadata;
		}
	}
}