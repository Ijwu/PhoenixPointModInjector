using System;

namespace PhoenixPointModLoader.Exceptions
{
	public class ModLoadFailureException : Exception
	{
		public ModLoadFailureException()
		{
		}

		public ModLoadFailureException(string message) : base(message)
		{
		}
	}
}