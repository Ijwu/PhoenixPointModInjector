using Base.Utils.GameConsole;

namespace PhoenixPointModLoader.Mods
{
	public class EnableConsoleMod : IPhoenixPointMod
	{
		public ModLoadPriority Priority => ModLoadPriority.Normal;

		public void Initialize()
		{
			GameConsoleWindow.DisableConsoleAccess = false;
		}
	}
}
