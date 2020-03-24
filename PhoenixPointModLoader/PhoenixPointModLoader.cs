using System;
using System.IO;
using System.Reflection;
using PhoenixPointModLoader.Config
using PhoenixPointModLoader.Infrastructure;
using PhoenixPointModLoader.Manager;

namespace PhoenixPointModLoader
{
	public static class PhoenixPointModLoader
	{
		private static ModManager ModManager;

		public static string ModsDirectory { get; private set; }

		public static void Initialize()
		{
			EnsureFolderSetup();
			Logger.InitializeLogging(Path.Combine(ModsDirectory, "PPModLoader.log"));
			SimpleInjector.Container container = CompositionRoot.GetContainer();
			ModManager = new ModManager(ModsDirectory, new JsonConfigProvider(), new FileSystemModLoader(), container);
			ModManager.Initialize();
		}

		private static void EnsureFolderSetup()
		{
			string manifestDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
				?? throw new InvalidOperationException("Could not determine operating directory. Is your folder structure correct? " +
				"Try verifying game files in the Epic Games Launcher, if you're using it.");

			ModsDirectory = Path.GetFullPath(Path.Combine(manifestDirectory, Path.Combine(@"..\..\Mods")));

			if (!Directory.Exists(ModsDirectory))
			{
				Directory.CreateDirectory(ModsDirectory);
			}
		}
	}
}

