using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaWebMap.ProcessManagerServer.Config
{
    /// <summary>
    /// A binary that is executed
    /// </summary>
    public class ConfigPackage
    {
        public string[] update_commands;
        public string exec_location;
        public string exec_args; //Should end with a space 
    }
}
