using LibDeltaSystem.CoreHub;
using LibDeltaSystem.CoreHub.CoreNetwork;
using LibDeltaSystem.CoreHub.Extras.OperationProgressStatus;
using LibDeltaSystem.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DeltaWebMap.ProcessManagerServer
{
    public class ManagerCoreNet : BaseClientCoreNetwork
    {
        public ManagerCoreNet()
        {
            SubscribeMessageOpcode(CoreNetworkOpcode.PROCESSMAN_DEPLOY, OnDeployCommand);
            SubscribeMessageOpcode(CoreNetworkOpcode.PROCESSMAN_UPDATE_ALL, OnUpdateAllCommand);
            SubscribeMessageOpcode(CoreNetworkOpcode.PROCESSMAN_DELETE_MANAGED, OnDestroyCommand);
        }

        public byte[] OnDeployCommand(CoreNetworkServer server, CoreNetworkOpcode opcode, byte[] payload)
        {
            //Get the parameters
            CoreNetworkServerType serverType = (CoreNetworkServerType)BinaryTool.ReadInt16(payload, 0);
            string configString = Encoding.UTF8.GetString(payload, 4, payload.Length - 4);
            byte count = payload[2];

            //Validate that we support this
            if (!Program.server_types.ContainsKey(serverType))
                return CreateFailResponse("This server manager is not configured to support this type of server.");

            //Get the config data
            JObject serverConfig;
            try
            {
                serverConfig = JsonConvert.DeserializeObject<JObject>(configString);
            } catch
            {
                return CreateFailResponse("Config JSON is invalid.");
            }

            //Create servers
            List<ManagerInstance> instances = new List<ManagerInstance>();
            for(int i = 0; i<count; i++)
            {
                ManagerInstance instance;
                try
                {
                    instance = Program.server_types[serverType].CreateInstance(i == count-1, serverConfig);
                }
                catch (Exception ex)
                {
                    return CreateFailResponse($"Failed to create instance: {ex.Message} {ex.StackTrace}");
                }

                //Start instance
                instance.StartProcess();
                instances.Add(instance);
            }

            //Validate instances is running
            Thread.Sleep(500);
            foreach(var i in instances)
            {
                if (!i.IsProcessRunning())
                    return CreateFailResponse($"A spawned instance ({i.settings.server_id}) ended early. More may have failed.");
            }

            //Create response payload
            byte[] response = new byte[3];
            response[0] = 0x00;
            BinaryTool.WriteUInt16(response, 1, (ushort)instances[0].settings.server_id);
            return response;
        }

        public byte[] OnUpdateAllCommand(CoreNetworkServer server, CoreNetworkOpcode opcode, byte[] payload)
        {
            //Read token
            uint token = BinaryTool.ReadUInt32(payload, 0);
            var eventSender = new OperationProgressClient(this, token);
            
            //Begin thread
            var t = new Thread(() =>
            {
                PackageUpdater.BeginUpdate(eventSender);
            });
            t.IsBackground = true;
            t.Start();
            return new byte[0];
        }

        public byte[] OnDestroyCommand(CoreNetworkServer server, CoreNetworkOpcode opcode, byte[] payload)
        {
            //Get requested ID
            ushort id = BinaryTool.ReadUInt16(payload, 0);

            //Find requested server ID
            ManagerInstance instance = null;
            foreach (var s in Program.server_types)
            {
                foreach(var p in s.Value.instances)
                {
                    if (p.settings.server_id == id)
                        instance = p;
                }
            }

            //Check if failed
            if (instance == null)
                return CreateFailResponse("Could not find requested instance.");

            //End
            instance.RemoveInstance().GetAwaiter().GetResult();

            //Return OK status
            return new byte[] { 0x01 };
        }

        private byte[] CreateFailResponse(string msg)
        {
            byte[] buffer = new byte[Encoding.UTF8.GetByteCount(msg) + 1];
            buffer[0] = 0x01;
            Encoding.UTF8.GetBytes(msg, 0, msg.Length, buffer, 1);
            return buffer;
        }
    }
}
