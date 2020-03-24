using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoenixPointModLoader.Config
{
	public interface IFileConfigProvider
	{
		T Read<T>(string relativeFilePath);

		bool Write<T>(string relativeFilePath, T config);
	}
}
