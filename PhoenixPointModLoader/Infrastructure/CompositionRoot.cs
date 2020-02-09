using PhoenixPointModLoader.Config;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoenixPointModLoader.Infrastructure
{
	public static class CompositionRoot
	{
		public static void ConfigureContainer(Container container)
		{
			container.Register<IFileConfigProvider, JsonConfigProvider>();
			container.Verify();
		}
	}
}
