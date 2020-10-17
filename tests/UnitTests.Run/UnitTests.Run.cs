using System;
using System.IO;
using System.Reflection;


namespace Rationals.Testing
{
    class Program
    {
        static int Main()
        {
            // TestedAssemblies.txt written to output directory by MSBuild "WriteAssemblyIndex" target
            string[] assemblyNames = File.ReadAllLines("TestedAssemblies.txt");
            return Utils.TestAssemblies(assemblyNames);
        }
    }
}