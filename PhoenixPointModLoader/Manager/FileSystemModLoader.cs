using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PhoenixPointModLoader.Manager
{
	public class FileSystemModLoader
	{
		public IList<Type> LoadModTypesFromDirectory(string directory)
		{
			List<Type> modTypes = new List<Type>();
			var fileSearch = Directory.EnumerateFiles(directory, ".dll", SearchOption.AllDirectories);
			foreach (string foundFile in fileSearch)
			{
				Assembly loadedFile = Assembly.Load(foundFile);
				if (IsModTypePresent(loadedFile))
				{
					modTypes.AddRange(GetModTypes(loadedFile));
				}
			}
			return modTypes;
		}

		private static IEnumerable<Type> GetModTypes(Assembly asm)
		{
			return asm.GetTypes().Where(type => typeof(IPhoenixPointMod).IsAssignableFrom(type));
		}

		private static bool IsModTypePresent(Assembly asm)
		{
			return asm.GetTypes().Any(type => typeof(IPhoenixPointMod).IsAssignableFrom(type));
		}
	}
}