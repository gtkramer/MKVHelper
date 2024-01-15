using System;
using System.Diagnostics;
using System.IO;

namespace MKVHelper.Utilities {
    public class ProcessUtilities {
        public static string GetProcessOutput(string command, string arguments) {
            Process process = RunProcess(command, arguments);
            using StreamReader reader = process.StandardOutput;
            return reader.ReadToEnd();
        }

        public static Process RunProcess(string command, string arguments) {
            Console.WriteLine("Running command: " + command + " " + arguments);
            using Process process = new();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit();
            return process;
        }
    }
}
