// socket manager
using System.Collections.Generic;

namespace SocketNet {
	class SocketMG {

		List<ISocketWrapper> sockets = new List<ISocketWrapper> ();
		public void AddSocket (ISocketWrapper socket) {
			sockets.Add (socket);

			socket.NotifyReceivedData += shub.OnReceiveData;
			shub.NotifyPostData += socket.Post;
			shub.NotifySendData += socket.Send;

		}

		public void RemoveSocket (ISocketWrapper socket) {
			socket.NotifyReceivedData -= shub.OnReceiveData;
			shub.NotifyPostData -= socket.Post;
			shub.NotifySendData -= socket.Send;

			sockets.Remove (socket);
		}

		public void ClearSockets(){
			while(sockets.Count>0){
				RemoveSocket(sockets[0]);
			}
		}

		EventHub eventhub=new EventHub();
		SocketHub shub = new SocketHub ();
		ClientHub chub = new ClientHub ();
		VirtualClient client = new VirtualClient ();
		public SocketMG () {
			shub.eventhub = eventhub;
			chub.eventhub = eventhub;
			chub.NotifyNetEvent += shub.OnNetEvent;
			shub.NotifyNetEvent += chub.OnNetEvent;

			client.eventhub = eventhub;
			client.SendData += chub.OnSendData;
			client.PostData += chub.OnPostData;
			chub.NotifyReceiveData += client.OnReceivedData;
		}

		public ClientProxy GenClient () {
			return new ClientProxy (client);
		}

	}
}