// socket manager
using System.Collections.Generic;
using TMediator;

namespace SocketNet {
	class SocketMG {

		List<SocketWrapper> sockets = new List<SocketWrapper> ();
		public void AddSocket (SocketWrapper socket) {
			sockets.Add (socket);

			socket.NotifyReceivedData += shub.OnReceiveData;
			shub.NotifyPostData += socket.Post;
			shub.NotifySendData += socket.Send;

		}

		public void removeSocket (SocketWrapper socket) {
			socket.NotifyReceivedData += shub.OnReceiveData;
			shub.NotifyPostData += socket.Post;
			shub.NotifySendData += socket.Send;

			sockets.Remove (socket);
		}

		Mediator mediator = new Mediator ();
		SocketHub shub = new SocketHub ();
		ClientHub chub = new ClientHub ();
		VirtualClient client = new VirtualClient ();
		public SocketMG () {
			shub.mediator = mediator;
			chub.mediator = mediator;
			chub.NotifyNetEvent += shub.OnNetEvent;
			shub.NotifyNetEvent += chub.OnNetEvent;

			client.mediator = mediator;
			client.SendData += chub.OnSendData;
			client.PostData += chub.OnPostData;
			chub.NotifyReceiveData += client.OnReceivedData;
		}

		public ClientProxy GenClient () {
			return new ClientProxy (client);
		}

	}
}