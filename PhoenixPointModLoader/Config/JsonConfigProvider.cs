using System;
using System.IO;
using Newtonsoft.Json;

namespace PhoenixPointModLoader.Config
{
	public class JsonConfigProvider : IFileConfigProvider
	{
		public T Read<T>(string relativeFilePath)
		{
			if (string.IsNullOrEmpty(relativeFilePath))
			{
				throw new InvalidOperationException("A relative file path to the `Mods` folder must be provided. " +
					"Try setting the `RelativeFilePath` property first.");
			}

			string absolutePath = Path.Combine(PhoenixPointModLoader.ModsDirectory, relativeFilePath);
			if (!File.Exists(absolutePath))
			{
				throw new FileNotFoundException($"The config file was not found at path `{absolutePath}`");
			}

			string configText = File.ReadAllText(absolutePath);
			T configObject = JsonConvert.DeserializeObject<T>(configText);

			return configObject;
		}

		public bool Write<T>(string relativeFilePath, T config)
		{
			if (string.IsNullOrEmpty(relativeFilePath))
			{
				throw new InvalidOperationException("A relative file path to the `Mods` folder must be provided. " +
					"Try setting the `RelativeFilePath` property first.");
			}

			string absolutePath = Path.Combine(PhoenixPointModLoader.ModsDirectory, relativeFilePath);
			string configText = JsonConvert.SerializeObject(config, Formatting.Indented);

			try
			{
				File.WriteAllText(absolutePath, configText);
			}
			catch (Exception e)
			{
				Logger.Log($"Exception occurred while writing to config file `{absolutePath}`.");
				Logger.Log(e.ToString());
				return false;
			}

			return true;
		}
	}
}