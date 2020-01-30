namespace PhoenixPointModLoader.Config
{
	public interface IConfigProvider
	{
		T Read<T>(string relativeFilePath);

		bool Write<T>(T config, string relativeFilePath);
	}
}