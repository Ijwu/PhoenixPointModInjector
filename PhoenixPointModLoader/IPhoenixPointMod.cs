using PhoenixPointModLoader.Manager;

namespace PhoenixPointModLoader
{
	public interface IPhoenixPointMod
	{
		ModLoadPriority Priority { get; }

		void Initialize();
	}
}