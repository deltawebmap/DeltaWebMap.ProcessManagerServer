using DeltaWebMap.ProcessManagerServer.Config;
using LibDeltaSystem;
using LibDeltaSystem.CoreHub.CoreNetwork;
using LibDeltaSystem.Db.System;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DeltaWebMap.ProcessManagerServer
{
    class Program
    {
        public static ConfigFile config;
        public static DeltaConnection connection;
        public static Dictionary<CoreNetworkServerType, ManagerServer> server_types;
        public static List<DbSystemServer> server_instances;

        static void Main(string[] args)
        {
            //Decode config file
            config = JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(args[0]));

            //Create Delta connection
            connection = new DeltaConnection(config.delta_config_path, config.server_id, 0, 1, new ManagerCoreNet());
            connection.Connect().GetAwaiter().GetResult();

            //Fetch server list
            server_instances = connection.system_delta_servers.Find(Builders<DbSystemServer>.Filter.Eq("enviornment", connection.config.env)).ToList();

            //Create all server types
            server_types = new Dictionary<CoreNetworkServerType, ManagerServer>();
            foreach (var s in config.servers)
                server_types.Add(s.type, new ManagerServer(s));

            //Start all
            foreach(var t in server_types)
            {
                t.Value.StartAll();
            }

            Console.WriteLine("Ready.");
            Task.Delay(-1).GetAwaiter().GetResult();
        }

        public static int GetNextPort()
        {
            int port = config.start_port_range;
            foreach(var s in server_instances)
            {
                port = Math.Max(port, s.port);
                foreach(var p in s.ports)
                    port = Math.Max(port, p);
            }
            return port + 1;
        }

        public static ushort GetNextServerId()
        {
            ushort id = config.start_server_id_range;
            foreach (var s in server_instances)
                id = (ushort)Math.Max(id, s.server_id);
            return (ushort)(id + 1);
        }
    }
}
