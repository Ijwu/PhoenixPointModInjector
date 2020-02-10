using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoenixPointModLoader.Config
{
	public interface IFileConfigProvider
	{
		string RelativeFilePath { get; set; }

		T Read<T>();

		bool Write<T>(T config);
	}
}
