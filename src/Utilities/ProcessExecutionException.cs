using System;

namespace MKVHelper.Utilities {
    public class ProcessExecutionException(int exitCode, string output, string error) : Exception($"Process exited with error code {exitCode}\nStandard Output: {output}\nStandard Error: {error}") {
        public int ExitCode { get; } = exitCode;
        public string StandardOutput { get; } = output;
        public string StandardError { get; } = error;
    }
}
