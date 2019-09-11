using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rationals.Testing {

    public class TestAttribute : System.Attribute {} // for assembly classes
    public class FactAttribute : System.Attribute {} // for unittest methods
    public class SampleAttribute : System.Attribute {} // for sample methods
    public class RunAttribute : System.Attribute { } // for sample methods - force run

    public class Exception : System.Exception {
        public Exception(string message, params object[] args)
            : base(System.String.Format(message, args))
        { }
    }


    public static class Assert {
        public static void Equal<T>(T expected, T actual)
        {
            if (!expected.Equals(actual)) {
                throw new Exception("Equal failed:\n expected: {0}\n actual:   {1}", expected, actual);
            }
        }
    }

    public static class Utils
    {
        static bool GetTestClass(TypeInfo t, out object instance) {
            instance = null;
            if (!t.IsClass) return false;
            if (t.GetCustomAttribute<TestAttribute>() == null) return false;
            if (!t.IsAbstract) {
                try {
                    instance = Activator.CreateInstance(t);
                } catch (System.Exception) {
                    Console.Error.WriteLine("Can't create instance of {0}", t.FullName);
                    return false;
                }
            }
            return true;
        }

        static bool RunTestMethod(MethodInfo m, object instance) {
            Console.WriteLine("[{0}.{1}]", m.DeclaringType.FullName, m.Name);
            try {
                m.Invoke(m.IsStatic ? null : instance, null);
                Console.WriteLine("  Ok");
                return true;
            } catch (TargetInvocationException e) {
                System.Exception ex = e.InnerException ?? e;
                //var st = new System.Diagnostics.StackTrace(true); -- format stacktrace !!! 
                string message = ex.Message;
                if (!(ex is Exception)) {
                    message = ex.GetType().FullName + ": " + message;
                }
                Console.Error.WriteLine("  " + message);
                Console.Error.WriteLine(ex.StackTrace);
                return false;
            }
        }

        static MethodInfo[] GetDeclaredMethods<Attr>(TypeInfo t)
            where Attr : System.Attribute
        {
            return t.GetMethods(
                BindingFlags.NonPublic | BindingFlags.Public |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly
            )
            .Where(m => m.GetCustomAttribute<Attr>() != null)
            .ToArray();
        }

        struct TestMethod {
            public object instance;
            public MethodInfo method;
        }

        public static bool RunAssemblySamples(Assembly assembly)
        {
            // collect samples and run to choose
            var sampleMethods = new List<TestMethod>();
            var runMethods    = new List<TestMethod>();

            foreach (TypeInfo t in assembly.DefinedTypes)
            {
                if (!GetTestClass(t, out object instance)) continue;

                foreach (MethodInfo m in GetDeclaredMethods<SampleAttribute>(t)) {
                    sampleMethods.Add(new TestMethod { instance = instance, method = m });
                }
                foreach (MethodInfo m in GetDeclaredMethods<RunAttribute>(t)) {
                    runMethods.Add(new TestMethod { instance = instance, method = m });
                }
            }

            // run [Run] methods if any
            if (runMethods.Count > 0) {
                bool result = true;
                foreach (var m in runMethods) {
                    result &= RunTestMethod(m.method, m.instance);
                }
                return result;
            }

            // choose and run a sample method
            if (sampleMethods.Count > 0) {
                for (int i = 0; i < sampleMethods.Count; ++i) {
                    var m = sampleMethods[i];
                    Console.WriteLine("{0,3} {1}", i+1, m.method.Name);
                }

                bool result = true;

                Console.Write("> ");
                string input = Console.ReadLine();
                if (int.TryParse(input, out int choise) && 0 < choise && choise <= sampleMethods.Count) {
                    TestMethod m = sampleMethods[choise-1];
                    result &= RunTestMethod(m.method, m.instance);
                } else {
                    throw new Exception("Invalid input: {0}", input);
                }

                return result;
            }

            return true;
        }

        public static bool TestAssembly(Assembly assembly)
        {
            bool result = true;

            foreach (TypeInfo t in assembly.DefinedTypes)
            {
                if (!GetTestClass(t, out object instance)) continue;

                // run [Test] methods
                MethodInfo[] methods = GetDeclaredMethods<TestAttribute>(t);
                foreach (MethodInfo m in methods) {
                    result &= RunTestMethod(m, instance);
                }
            }
            return result;
        }

        public static int TestAssemblies(string[] assemblyNames)
        {
            bool result = true;

            foreach (string name in assemblyNames) {
                try {
                    Assembly a = Assembly.Load(name);
                    result &= TestAssembly(a);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex.GetType().FullName + " " + ex.Message);
                    return -1;
                }
            }

            return result ? 0 : 1;
        }

    }
}