using PhoenixPointModLoader.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoenixPointModLoader.Tests.Helpers
{
	public class ModMetadataBuilder
	{
		public static ModMetadataBuilder Instance = new ModMetadataBuilder();

		private ModMetadata _modMetadata;
		private ModMetadataBuilder()
		{
			
		}

		public ModMetadataBuilder WithName(string name)
		{
			_modMetadata = new ModMetadata(name, _modMetadata?.Version, _modMetadata?.Description);
			return this;
		}

		public ModMetadataBuilder WithVersion(Version version)
		{
			_modMetadata = new ModMetadata(_modMetadata?.Name, version, _modMetadata?.Description);
			return this;
		}

		public ModMetadataBuilder WithVersion(string version)
		{
			Version parsed = new Version(version);
			_modMetadata = new ModMetadata(_modMetadata?.Name, parsed, _modMetadata?.Description);
			return this;
		}

		public ModMetadataBuilder WithDependency(ModMetadata dependency)
		{
			_modMetadata?.Dependencies?.Add(dependency);
			return this;
		}

		public ModMetadata Build()
		{
			return _modMetadata;
		}
	}
}
