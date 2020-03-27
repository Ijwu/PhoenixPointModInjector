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
		private static Container Container;
		public static Container GetContainer()
		{
			if (Container is null)
			{
				Container = new Container();
				Container.Register<IFileConfigProvider, JsonConfigProvider>();
				Container.Verify();
				return Container;
			}
			else
			{
				return Container;
			}
		}
	}
}
