using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using PhoenixPointModLoader.Infrastructure;
using PhoenixPointModLoader.Mods;
using SimpleInjector;

namespace PhoenixPointModLoader
{
	public static class PhoenixPointModLoader
	{
		private static ModManager ModManager;

		public static string ModDirectory { get; private set; }

		public static void Initialize()
		{
			EnsureFolderSetup();
			Logger.InitializeLogging(Path.Combine(ModDirectory, "PPModLoader.log"));
			ModManager = new ModManager(ModDirectory);
			ModManager.Initialize();
		}

		private static void EnsureFolderSetup()
		{
			string manifestDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
				?? throw new InvalidOperationException("Could not determine operating directory. Is your folder structure correct? " +
				"Try verifying game files in the Epic Games Launcher, if you're using it.");

			ModDirectory = Path.GetFullPath(Path.Combine(manifestDirectory, Path.Combine(@"..\..\Mods")));

			if (!Directory.Exists(ModDirectory))
			{
				Directory.CreateDirectory(ModDirectory);
			}
		}
	}
}

