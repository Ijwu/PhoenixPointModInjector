using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoenixPointModLoader.Dependencies
{
	public class ModMetadata
	{
		public string Name { get; set; }
		public Version Version { get; set; }
		public IList<ModMetadata> Dependencies { get; set; }
	}
}
