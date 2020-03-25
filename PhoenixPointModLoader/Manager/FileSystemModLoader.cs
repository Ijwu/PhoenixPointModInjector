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
			List<Type> modTypes = new List<Type>();
			var fileSearch = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);
			foreach (string foundFile in fileSearch)
			{
				try
				{
					Assembly loadedFile = Assembly.LoadFrom(foundFile);
					if (IsModTypePresent(loadedFile))
					{
						modTypes.AddRange(GetModTypes(loadedFile));
					}
				}
				catch (Exception e)
				{
					Logger.Log($"Could not load modfile `{foundFile}`.");
					Logger.Log(e.ToString());
					if (e.InnerException != null)
					{
						Logger.Log(e.InnerException.ToString());
					}
					continue;
				}
			}
			return modTypes;
		}

		public List<Type> LoadLegacyModsFromDirectory(string directory)
		{
			List<Type> modTypes = new List<Type>();
			var fileSearch = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);
			foreach (string foundFile in fileSearch)
			{
				try
				{
					Assembly loadedFile = Assembly.LoadFrom(foundFile);
					if (IsLegacyModPresent(loadedFile))
					{
						modTypes.AddRange(GetLegacyModTypes(loadedFile));
					}
				}
				catch (Exception e)
				{
					Logger.Log($"Could not load modfile `{foundFile}`.");
					Logger.Log(e.ToString());
					if (e.InnerException != null)
					{
						Logger.Log(e.InnerException.ToString());
					}
					continue;
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

		private static bool IsLegacyModPresent(Assembly asm)
		{
			return asm.GetTypes().Any(type => type.GetMethod("Init") != null);
		}

		private static IEnumerable<Type> GetLegacyModTypes(Assembly asm)
		{
			return asm.GetTypes().Where(type => type.GetMethod("Init") != null);
		}
	}
}