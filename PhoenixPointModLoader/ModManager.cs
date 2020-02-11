using PhoenixPointModLoader.Infrastructure;
using PhoenixPointModLoader.Mods;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PhoenixPointModLoader
{
	public class ModManager
	{
		private Container Container;

		public string ModDirectory { get; }

		public ModManager(string modDirectory)
		{
			ModDirectory = modDirectory;
		}

		public void Initialize()
		{
			Container = CompositionRoot.GetContainer();

			IList<IPhoenixPointMod> allMods = RetrieveAllMods();
			InitializeMods(allMods);
		}

		private IList<IPhoenixPointMod> RetrieveAllMods()
		{
			List<string> dllPaths = Directory.GetFiles(ModDirectory, "*.dll", SearchOption.AllDirectories).ToList();
			List<IPhoenixPointMod> allMods = new List<IPhoenixPointMod>();
			IncludeDefaultMods(allMods);
			foreach (var dllPath in dllPaths)
			{
				allMods.AddRange(LoadDll(dllPath));
			}
			return allMods;
		}

		private void IncludeDefaultMods(IList<IPhoenixPointMod> allMods)
		{
			allMods.Add(Container.GetInstance<EnableConsoleMod>());
			allMods.Add(Container.GetInstance<LoadConsoleCommandsFromAllAssembliesMod>());
		}

		private void InitializeMods(IList<IPhoenixPointMod> allMods)
		{
			var prioritizedModList = allMods.ToLookup(x => x.Priority);
			ModLoadPriority[] loadOrder = new[] { ModLoadPriority.High, ModLoadPriority.Normal, ModLoadPriority.Low };
			foreach (ModLoadPriority priority in loadOrder)
			{
				Logger.Log("Attempting to initialize `{0}` priority mods.", priority.ToString());
				foreach (var mod in prioritizedModList[priority])
				{
					try
					{
						mod.Initialize();
						Logger.Log("Mod class `{0}` from DLL `{1}` was successfully initialized.",
							mod.GetType().Name,
							Path.GetFileName(mod.GetType().Assembly.Location));
					}
					catch (Exception e)
					{
						Logger.Log("Mod class `{0}` from DLL `{1}` failed to initialize.",
							mod.GetType().Name,
							Path.GetFileName(mod.GetType().Assembly.Location));
						Logger.Log(e.ToString());
					}
				}
			}
		}

		private IList<IPhoenixPointMod> LoadDll(string path)
		{
			string originalDirectory = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(path);
			Assembly mod = Assembly.LoadFile(path);
			Type[] allClasses = mod.GetTypes();
			List<Type> modClasses = allClasses.Where(x => x.GetInterface("IPhoenixPointMod") != null).ToList();

			if (!modClasses.Any())
			{
				Logger.Log("No mod classes found in DLL: {0}", Path.GetFileName(path));
				return new List<IPhoenixPointMod>();
			}

			var modInstances = new List<IPhoenixPointMod>();
			foreach (Type modClass in modClasses)
			{
				IPhoenixPointMod modInstance = null;
				try
				{
					modInstance = Container.GetInstance(modClass) as IPhoenixPointMod;
				}
				catch (Exception e)
				{
					Logger.Log("Error has occurred when instantiating mod class`{0}` in DLL `{1}`.", modClass.Name, Path.GetFileName(path));
					Logger.Log(e.ToString());
					continue;
				}

				if (modInstance == null)
				{
					Logger.Log("Instantiated mod class `{0}` from DLL `{1}` was null for unknown reason. " +
						"Please ensure you have a default constructor defined for your type.", modClass.Name, Path.GetFileName(path));
				}

				modInstances.Add(modInstance);
			}
			Environment.CurrentDirectory = originalDirectory;
			return modInstances;
		}
	}
}
