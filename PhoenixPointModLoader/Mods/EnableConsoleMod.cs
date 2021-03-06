﻿using Base.Utils.GameConsole;
using PhoenixPointModLoader.Config;
using PhoenixPointModLoader.Manager;
using System.IO;

namespace PhoenixPointModLoader.Mods
{
	public class EnableConsoleMod : IPhoenixPointMod
	{
		private EnableConsoleConfig consoleAccessConfig;
		private IFileConfigProvider configProvider;

		public ModLoadPriority Priority => ModLoadPriority.Normal;

		public EnableConsoleMod(IFileConfigProvider config)
		{
			configProvider = config;
		}

		public void Initialize()
		{
			try
			{
				consoleAccessConfig = configProvider.Read<EnableConsoleConfig>("EnableConsoleAccess.json");
			}
			catch (FileNotFoundException)
			{
				consoleAccessConfig = new EnableConsoleConfig() { EnableConsoleAccess = true };
				configProvider.Write("EnableConsoleAccess.json", consoleAccessConfig);
			}
			finally
			{
				SetConsoleAccess(consoleAccessConfig.EnableConsoleAccess);
			}
		}

		private void SetConsoleAccess(bool enabled)
		{
			GameConsoleWindow.DisableConsoleAccess = !enabled;
		}

		private class EnableConsoleConfig
		{
			public bool EnableConsoleAccess { get; set; } = true;
		}
	}
}
