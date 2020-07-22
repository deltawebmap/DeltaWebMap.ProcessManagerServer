using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace DeltaWebMap.ProcessManagerServer
{
    public class ManagerTools
    {
        public static int ExecuteShellCommand(string cmd, out string result)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Program.config.shell,
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode;
        }

        public static int ExecuteShellCommand(string cmd)
        {
            return ExecuteShellCommand(cmd, out string nullOutput);
        }
    }
}
