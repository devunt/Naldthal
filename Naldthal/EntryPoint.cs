using System;
using System.Runtime.Remoting;
using EasyHook;

namespace Naldthal
{
    public class EntryPoint : IEntryPoint
    {
        private readonly BridgeInterface _bridge;

        public EntryPoint(RemoteHooking.IContext context, string channelName, string dataJsonPath)
        {
            _bridge = RemoteHooking.IpcConnectClient<BridgeInterface>(channelName);
            _bridge.Ping();

            Hook.Initialize(_bridge, dataJsonPath);
        }

        public void Run(RemoteHooking.IContext context, string channelName, string dataJsonPath)
        {
            try
            {
                Util.Log("Payload loaded.");

                Hook.Install();
                RemoteHooking.WakeUpProcess();

                Hook.Join();
            }
            catch (RemotingException)
            {
            }
            catch (Exception ex)
            {
                _bridge.WriteLine(ex);
                _bridge.WriteLine(ex.StackTrace);
            }
            finally
            {
                Hook.Release();

                Util.Log("Payload unloaded.");
            }
        }
    }
}
