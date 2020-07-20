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
        }

        public byte[] OnDeployCommand(CoreNetworkServer server, CoreNetworkOpcode opcode, byte[] payload)
        {
            //Get the type
            CoreNetworkServerType serverType = (CoreNetworkServerType)BinaryTool.ReadInt16(payload, 0);

            //Validate that we support this
            if(!Program.server_types.ContainsKey(serverType))
                return CreateFailResponse("This server manager is not configured to support this type of server.");

            //Get the config data
            string configString = Encoding.UTF8.GetString(payload, 4, BinaryTool.ReadUInt16(payload, 2));
            JObject serverConfig;
            try
            {
                serverConfig = JsonConvert.DeserializeObject<JObject>(configString);
            } catch
            {
                return CreateFailResponse("Config JSON is invalid.");
            }

            //Create server
            var instance = Program.server_types[serverType].CreateInstance(serverConfig);
            instance.StartProcess();

            //Create response payload
            byte[] response = new byte[3];
            response[0] = 0x00;
            BinaryTool.WriteUInt16(response, 1, (ushort)instance.settings.server_id);
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

        private byte[] CreateFailResponse(string msg)
        {
            byte[] buffer = new byte[Encoding.UTF8.GetByteCount(msg) + 1];
            buffer[0] = 0x01;
            Encoding.UTF8.GetBytes(msg, 0, msg.Length, buffer, 1);
            return buffer;
        }
    }
}
