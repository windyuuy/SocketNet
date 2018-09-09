using System;
using System.Net;
using System.Net.Sockets;

namespace SocketNet {
	class AliveSocket {
		Socket _socket;
		private AddressFamily interNetwork;
		private SocketType stream;
		private ProtocolType tcp;

		public bool Blocking { get { return _socket.Blocking; } internal set { _socket.Blocking = value; } }
		public int SendTimeout { get { return _socket.SendTimeout; } internal set { _socket.SendTimeout = value; } }
		public int ReceiveTimeout { get { return _socket.ReceiveTimeout; } internal set { _socket.ReceiveTimeout = value; } }
		public int Available { get { return _socket.Available; } }
		public bool Connected { get { return _socket.Connected; } }

		public AliveSocket (AddressFamily interNetwork, SocketType stream, ProtocolType tcp) {
			_socket = new Socket (interNetwork, stream, tcp);
		}
		~AliveSocket(){
			Console.WriteLine("sdkjlwjelefwlj");
		}

		internal int Receive (byte[] buffer, int size, SocketFlags socketFlags) {
			return _socket.Receive (buffer, size, socketFlags);
		}

		internal bool Poll (int microSeconds, SelectMode mode) {
			return _socket.Poll (microSeconds, mode);
		}

		internal bool DisconnectAsync (SocketAsyncEventArgs e) {
			return _socket.DisconnectAsync (e);
		}

		internal void EndConnect (IAsyncResult ar) {
			_socket.EndConnect (ar);
		}

		internal int Send (byte[] buffer, int size, SocketFlags socketFlags) {
			return _socket.Send (buffer, size, socketFlags);
		}

		internal IAsyncResult BeginConnect (EndPoint remoteEP, AsyncCallback callback, object state) {
			return _socket.BeginConnect (remoteEP, callback, state);
		}
	}
}