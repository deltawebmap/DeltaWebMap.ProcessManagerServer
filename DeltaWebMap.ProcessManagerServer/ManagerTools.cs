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
            //Create args
            string args;
            if(Environment.OSVersion.Platform == PlatformID.Win32NT)
                args = "/C \"" + cmd.Replace("\"", "\\\"") + "\"";
            else
                args = "-c \"" + cmd.Replace("\"", "\\\"") + "\"";

            //Run
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Program.config.shell,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            //Start
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
