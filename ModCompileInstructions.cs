using System;
namespace PPGModCompiler
{
    public struct ModCompileInstructions
    {
        public int ID;

        public string OutputFileName;

        public string[] Paths;

        public string[] AssemblyReferenceLocations;

        public string MainClass;

        public bool RejectShadyCode;

        public string InsertSourceB64;
    }
}

