using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PhoenixPointModLoader.Manager
{
	public class FileSystemModLoader
	{
		public List<Type> LoadModTypesFromDirectory(string directory)
		{
			return FilterTypesInDirectory(directory, IsModType).ToList();
		}

		public List<Type> LoadLegacyModsFromDirectory(string directory)
		{
			return FilterTypesInDirectory(directory, IsLegacyModType).ToList();
		}

		private static IEnumerable<Type> FilterTypesInDirectory(string directory, Func<Type, bool> predicate)
		{
			List<Type> modTypes = new List<Type>();
			var fileSearch = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);
			foreach (string foundFile in fileSearch)
			{
				try
				{
					Assembly loadedFile = Assembly.LoadFrom(foundFile);
					modTypes = modTypes.Concat(FilterTypes(loadedFile, predicate)).ToList();
				}
				catch (Exception e)
				{
					Logger.Log($"Could not load mod file `{foundFile}`.");
					Logger.Log(e.ToString());
					continue;
				}
			}
			return modTypes;
		}

		private static IEnumerable<Type> FilterTypes(Assembly asm, Func<Type, bool> predicate)
		{
			return asm.GetTypes().Where(predicate);
		}

		private static bool IsModType(Type type)
		{
			return typeof(IPhoenixPointMod).IsAssignableFrom(type);
		}

		private static bool IsLegacyModType(Type type)
		{
			return type.GetMethod("Init") != null;
		}
	}
}