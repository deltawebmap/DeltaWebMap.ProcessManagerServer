using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.ProcessManagerServer.Config
{
    public class ConfigFile
    {
        public string delta_config_path;
        public ushort server_id;
        public int start_port_range;
        public ushort start_server_id_range;
        public string ip_address;
        public string shell; // /bin/bash on Linux
        public string apache_reload_command; //Only required if apache mode is enabled
        public ConfigServer[] servers;
        public Dictionary<string, ConfigPackage> packages;
    }
}
