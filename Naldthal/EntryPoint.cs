using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using EasyHook;

namespace Naldthal
{
    public class EntryPoint : IEntryPoint
    {
        private readonly Bridge _bridge;

        public EntryPoint(RemoteHooking.IContext context, string channelName, string dataJsonPath)
        {
            _bridge = RemoteHooking.IpcConnectClient<Bridge>(channelName);
            _bridge.Ping();

            Hook.Initialize(_bridge, dataJsonPath);
        }

        public void Run(RemoteHooking.IContext context, string channelName, string dataJsonPath)
        {
            try
            {
                Debug.WriteLine("[NT] Payload loaded.");

                Hook.Install();
                RemoteHooking.WakeUpProcess();

                Hook.Join();
            }
            catch (RemotingException)
            {
                Debug.WriteLine("[NT] Bridge is broken.");
            }
            catch (Exception ex)
            {
                _bridge.WriteLine(ex);
            }
            finally
            {
                Hook.Release();

                Debug.WriteLine("[NT] Payload unloaded.");
            }
        }
    }
}