using System;
using System.Threading.Tasks;

namespace SocketNet
{
	public interface ISocketWrapper
	{
		event Action<RespRawData> NotifyReceivedData;
		event Action NotifyDisconnect;
		void Post(ReqInfo info);
		void Send(ReqInfo info);
		void BeginProcess();
		Task FinishProcess(bool force = false);
		void ReadProcess ();
        void WriteProcess ();
        // void ProcessReceiveEvents ();

		event Action NotifyConnected;
	}
}