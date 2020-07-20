using LibDeltaSystem.CoreHub.Extras.OperationProgressStatus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DeltaWebMap.ProcessManagerServer
{
    public static class PackageUpdater
    {
        public static void BeginUpdate(OperationProgressClient progressSender)
        {
            //Close all services
            _CloseProcesses(progressSender);

            //Update all
            _UpdatePackages(progressSender);

            //Restart all
            _RestartProcesses(progressSender);
        }

        private static void _CloseProcesses(OperationProgressClient progressSender)
        {
            progressSender.SendStatus(0x00, "Shutting down all processes...");
            int procs = 0;
            foreach (var s in Program.server_types)
                procs += s.Value.EndAll();
            progressSender.SendStatus(0x00, $"{procs} processes shut down.");
        }

        private static void _UpdatePackages(OperationProgressClient progressSender)
        {
            progressSender.SendStatus(0x00, $"Updating {Program.config.packages.Count} packages...");
            foreach(var p in Program.config.packages)
            {
                progressSender.SendStatus(0x00, $"Updating package {p.Key}...");
                int code = ManagerTools.ExecuteShellCommand(p.Value.update_command);
                progressSender.SendStatus(0x00, $"Update of package {p.Key} finished with exit code {code}.");
            }
            progressSender.SendStatus(0x00, $"Finished updating {Program.config.packages.Count} packages.");
        }

        private static void _RestartProcesses(OperationProgressClient progressSender)
        {
            progressSender.SendStatus(0x00, $"Restarting processes...");
            foreach (var s in Program.server_types)
                s.Value.StartAll();
            progressSender.SendStatus(0x00, $"Processes started. Waiting for validation...");

            //Wait to see if any end
            Thread.Sleep(5000);

            //Count up processess running
            int running = 0;
            int ended = 0;
            foreach(var s in Program.server_types)
            {
                foreach(var p in s.Value.instances)
                {
                    if (p.IsProcessRunning())
                        running++;
                    else
                        ended++;
                }
            }

            //Finish
            progressSender.SendStatus((ushort)(ended == 0 ? 0x01 : 0x02), $"Processes started. {running} running, {ended} died.");
        }
    }
}
