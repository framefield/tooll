
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;


namespace Framefield.Shared
{
    public static class LibPathManager
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)] 
        private static extern Boolean SetDllDirectory(string directory);

        public static string PlatformSpecificDir
        {
            get { return (IntPtr.Size == 8) ? "x64" : "x86"; }
        }

        public static void SetDllSearchPath()
        {
            var dllPath = AppDomain.CurrentDomain.BaseDirectory + @"Libs\" + PlatformSpecificDir;
            SetDllDirectory(dllPath);
        }

        public static Assembly CustomResolve(object sender, ResolveEventArgs args)
        {
            var splitIndex = args.Name.IndexOf(',');
            var assemblyFileName = String.Empty;
            if (splitIndex > 0)
                assemblyFileName = args.Name.Substring(0, splitIndex) + ".dll";
            else
                assemblyFileName = args.Name + ".dll";
            
            // first look in general (x86 and x64) lib path
            var assemblyPath = AppDomain.CurrentDomain.BaseDirectory + @"Libs\" + assemblyFileName; // path must be absolute to load assembly
            if (File.Exists(assemblyPath))
            {
                var asm = Assembly.LoadFile(assemblyPath);
                return asm;
            }

            // not found there, so look in platform specific path
            assemblyPath = AppDomain.CurrentDomain.BaseDirectory + @"Libs\" + PlatformSpecificDir + @"\" + assemblyFileName;
            if (File.Exists(assemblyPath))
            {
                var asm = Assembly.LoadFile(assemblyPath);
                return asm;
            }
            Console.WriteLine("LibPathManager - Could not load assembly: {0}", assemblyFileName);

            return null;
        }
    }
}
