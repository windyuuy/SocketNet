
using System;
using System.Threading.Tasks;

namespace SocketNet{
	
	class ClientProxy {
		VirtualClient client;
		public ClientProxy (VirtualClient client) {
			this.client = client;
		}

		public bool Send (SessionInfo info, DataCallback fn) {
			return client.Send (info, fn);
		}

		public bool Post (SessionInfo info, DataCallback fn) {
			return client.Post (info, fn);
		}

		public Task<RespRawData> SendAsync (SessionInfo info) {
			return AsyncTask.Run<SessionInfo,RespRawData> (Send, info);
		}
		public Task<RespRawData> PostAsync (SessionInfo info) {
			return AsyncTask.Run<SessionInfo,RespRawData> (Post, info);
		}
	}

}