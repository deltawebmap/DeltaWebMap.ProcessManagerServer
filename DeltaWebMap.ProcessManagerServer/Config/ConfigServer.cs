using LibDeltaSystem.CoreHub.CoreNetwork;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.ProcessManagerServer.Config
{
    public class ConfigServer
    {
        public int required_ports; //Required ports, NOT including the corenet port
        public CoreNetworkServerType type;
        public string package_name;

        //Apache2 mode
        public bool apache_mode_enabled;
        public string apache_file; //File to open. Contents between #DELTA_AUTOMATE_BEGIN and #DELTA_AUTOMATE_END will be deleted
        public string apache_template; //Template string to use. Replaces %HOST% with the address and port. For example, "10.0.1.13:43182"
    }
}
