using System;

namespace PPGModCompiler
{
	public class ShadyException : Exception
	{
		public ShadyException(string message)
			: base(message)
		{
		}
	}
}
