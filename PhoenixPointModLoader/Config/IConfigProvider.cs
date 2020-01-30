namespace PhoenixPointModLoader.Config
{
	public interface IConfigProvider
	{
		T Read<T>();

		bool Write<T>(T config);
	}
}