using System;

namespace PPGModCompiler
{
	public class IncorrectPPGPathException : Exception
	{
		public IncorrectPPGPathException()
			: base("Incorrect PPG Path! Could not find either the PeoplePlayground_Data or the CompiledModAssemblies folder!")
		{
		}
	}
}
