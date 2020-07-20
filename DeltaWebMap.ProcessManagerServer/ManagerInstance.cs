using LibDeltaSystem.Db.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DeltaWebMap.ProcessManagerServer
{
    public class ManagerInstance
    {
        public ManagerServer server_type;
        public DbSystemServer settings;
        public Process process;
        public TaskCompletionSource<bool> processEndedPromise;

        public ManagerInstance(ManagerServer server_type, DbSystemServer settings)
        {
            this.server_type = server_type;
            this.settings = settings;
        }

        public void StartProcess()
        {
            //Make sure it's not already running
            if (process != null)
                throw new Exception("Process is already running!");

            //Create args
            string args = $"{Program.config.delta_config_path} {settings.server_id}";

            //Log
            Program.connection.Log("ManagerProcess-StartProcess", $"Starting server. Type={server_type.type_config.type.ToString()}, ServerID={settings.server_id}, Port={settings.port}, Exec={server_type.package.exec_location}, Args={args}", LibDeltaSystem.DeltaLogLevel.Low);

            //Create process
            process = Process.Start(new ProcessStartInfo
            {
                FileName = server_type.package.exec_location,
                Arguments = server_type.package.exec_args + args
            });
            process.EnableRaisingEvents = true; //why isn't this enabled by default?

            //Create promise
            processEndedPromise = new TaskCompletionSource<bool>();

            //Subscribe
            process.Exited += (object sender, EventArgs e) =>
            {
                processEndedPromise.TrySetResult(true);
            };
            if(process.HasExited)
                processEndedPromise.TrySetResult(true);
        }

        public Task<bool> StopProcess()
        {
            //Send command
            //process.CloseMainWindow();
            process.Kill();

            return processEndedPromise.Task;
        }

        public bool IsProcessRunning()
        {
            if (process == null)
                return false;
            return !process.HasExited;
        }
    }
}
