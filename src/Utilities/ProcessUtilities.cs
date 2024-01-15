using System;
using System.Diagnostics;

namespace MKVHelper.Utilities {
    public class ProcessUtilities {
        public static (string StandardOutput, string StandardError) RunProcess(string command, string arguments) {
            Console.WriteLine("Running command: " + command + " " + arguments);
            using Process process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();

            // To avoid deadlocks, always read the standard output/error streams first and then wait.
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0) {
                throw new ProcessExecutionException(process.ExitCode, output, error);
            }

            return (output, error);
        }
    }
}
