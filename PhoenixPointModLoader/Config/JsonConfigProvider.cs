using System;
using System.IO;
using Newtonsoft.Json;

namespace PhoenixPointModLoader.Config
{
	public class JsonConfigProvider : IConfigProvider
	{
		public T Read<T>(string relativeFilePath)
		{
			string absolutePath = Path.Combine(PhoenixPointModLoader.ModDirectory, Path.GetDirectoryName(relativeFilePath));
			if (!File.Exists(relativeFilePath))
			{
				throw new FileNotFoundException($"The config file was not found at path `{absolutePath}`");
			}

			string configText = File.ReadAllText(absolutePath);
			T configObject = JsonConvert.DeserializeObject<T>(configText);

			return configObject;
		}

		public bool Write<T>(T config, string relativeFilePath)
		{
			string absolutePath = Path.Combine(PhoenixPointModLoader.ModDirectory, Path.GetDirectoryName(relativeFilePath));
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