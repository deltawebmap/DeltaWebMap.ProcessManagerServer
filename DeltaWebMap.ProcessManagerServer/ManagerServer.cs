using DeltaWebMap.ProcessManagerServer.Config;
using LibDeltaSystem.Db.System;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaWebMap.ProcessManagerServer
{
    /// <summary>
    /// A type of server we manage, NOT A PROCESS
    /// </summary>
    public class ManagerServer
    {
        public ManagerServer(ConfigServer type_config)
        {
            this.type_config = type_config;
            this.package = Program.config.packages[this.type_config.package_name];

            //Get servers of this type and spawn their processes (but don't actually start them yet)
            foreach(var i in Program.server_instances)
            {
                if (i.server_type == type_config.type.ToString() && i.manager_id == Program.config.server_id)
                    instances.Add(new ManagerInstance(this, i));
            }
        }

        public ConfigServer type_config;
        public ConfigPackage package;
        public List<ManagerInstance> instances = new List<ManagerInstance>();

        public const string APACHE_CONF_MARKER_BEGIN = "#DELTA_AUTOMATE_BEGIN";
        public const string APACHE_CONF_MARKER_END = "#DELTA_AUTOMATE_END";

        public void StartAll()
        {
            foreach (var p in instances)
                p.StartProcess();
        }

        public int EndAll()
        {
            List<Task> tasks = new List<Task>();
            foreach (var p in instances)
                tasks.Add(p.StopProcess());
            Task.WaitAll(tasks.ToArray());
            return tasks.Count;
        }

        /// <summary>
        /// Used for automating adding HTTP load balancers to Apache2. We open the Apache2 file, modify it, close it. YOU WILL NEED TO RELOAD APACHE2 ELSEWHERE
        /// </summary>
        public void UpdateApacheFile(bool reload = false)
        {
            //Ensure that Apache2 mode is enabled
            if (!type_config.apache_mode_enabled)
                return;

            //Read file
            string conf = File.ReadAllText(type_config.apache_file);

            //Locate the beginning of our template region
            int begin = conf.IndexOf(APACHE_CONF_MARKER_BEGIN);
            if (begin == -1)
                throw new Exception("Apache2 file is not properly formatted. Missing beginning tag.");
            begin += APACHE_CONF_MARKER_BEGIN.Length;

            //Locate the end of our template region
            int end = conf.IndexOf(APACHE_CONF_MARKER_END);
            if (end == -1)
                throw new Exception("Apache2 file is not properly formatted. Missing ending tag.");

            //Create the new data to use
            string data = "";
            foreach(var i in instances)
            {
                data += type_config.apache_template.Replace("%HOST%", i.settings.address + ":" + i.settings.ports[0]) + "\n";
            }

            //Modify the file
            File.WriteAllText(type_config.apache_file, conf.Substring(0, begin) + "\n" + data + conf.Substring(end, conf.Length - end));

            //Ensure we have the command to reload
            if (reload && Program.config.apache_reload_command == null)
                throw new Exception("Can't reload Apache2, as there is no command set for it.");

            //Reload
            if (reload)
                ManagerTools.ExecuteShellCommand(Program.config.apache_reload_command);
        }

        public ManagerInstance CreateInstance(JObject config)
        {
            //Generate ports to use
            int netPort = Program.GetNextPort();
            List<int> customPorts = new List<int>();
            for (int i = 0; i < type_config.required_ports; i++)
                customPorts.Add(netPort + 1 + i);

            //Generate an instance id
            ushort instanceId = Program.GetNextServerId();

            //Generate token to use
            uint token = (uint)LibDeltaSystem.Tools.SecureStringTool.GenerateSecureInteger();

            //Create server entry
            var settings = new DbSystemServer
            {
                server_id = instanceId,
                server_token = token.ToString(),
                server_type = type_config.type.ToString(),
                address = Program.config.ip_address,
                port = netPort,
                enviornment = Program.connection.config.env,
                manager_id = Program.config.server_id,
                ports = customPorts,
                config = config,
                _id = ObjectId.GenerateNewId()
            };

            //Add
            Program.connection.system_delta_servers.InsertOne(settings);
            Program.server_instances.Add(settings);

            //Notify all of this change and allow it to propigate
            Program.connection.network.NotifyAllServerListModified();
            Thread.Sleep(400);

            //Create instances
            var instance = new ManagerInstance(this, settings);
            instances.Add(instance);

            //Modify the Apache2 config file
            UpdateApacheFile();

            return instance;
        }
    }
}
