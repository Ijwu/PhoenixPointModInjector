namespace PhoenixPointModLoader.Manager
{
	internal class ModEntry
	{
		public IPhoenixPointMod ModInstance { get; }
		public ModMetadata ModMetadata { get; }

		public ModEntry(IPhoenixPointMod enableConsoleMod, ModMetadata modMetadata)
		{
			ModInstance = enableConsoleMod;
			ModMetadata = modMetadata;
		}
	}
}