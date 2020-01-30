using System;
using System.IO;
using Newtonsoft.Json;

namespace PhoenixPointModLoader.Config
{
	public class JsonConfigProvider : IConfigProvider
	{
		public string RelativeFilePath { get; }

		public JsonConfigProvider(string relativeFilePath)
		{
			RelativeFilePath = relativeFilePath;
		}

		public T Read<T>()
		{
			string absolutePath = Path.Combine(PhoenixPointModLoader.ModDirectory, Path.GetDirectoryName(RelativeFilePath));
			if (!File.Exists(RelativeFilePath))
			{
				throw new FileNotFoundException($"The config file was not found at path `{absolutePath}`");
			}

			string configText = File.ReadAllText(absolutePath);
			T configObject = JsonConvert.DeserializeObject<T>(configText);

			return configObject;
		}

		public bool Write<T>(T config)
		{
			string absolutePath = Path.Combine(PhoenixPointModLoader.ModDirectory, Path.GetDirectoryName(RelativeFilePath));
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