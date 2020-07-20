using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace DeltaWebMap.ProcessManagerServer
{
    public class ManagerTools
    {
        public static int ExecuteShellCommand(string cmd)
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
            process.WaitForExit();

            return process.ExitCode;
        }
    }
}
