using System;
using System.Runtime.Serialization;

namespace PhoenixPointModLoader.Manager
{
	[Serializable]
	internal class ModCircularDependencyException : Exception
	{
		private ModMetadata modMetadata;
		private ModMetadata dependency;

		public ModCircularDependencyException()
		{
		}

		public ModCircularDependencyException(string message) : base(message)
		{
		}

		public ModCircularDependencyException(ModMetadata modMetadata, ModMetadata dependency)
		{
			this.modMetadata = modMetadata;
			this.dependency = dependency;
		}

		public ModCircularDependencyException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected ModCircularDependencyException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}