using System;
using System.Collections.Generic;
using TMediator;

namespace SocketNet {
	class NetHub {
		protected Mediator _mediator;
		public Mediator mediator {
			get {
				return _mediator;
			}
			set {
				foreach (var key in _ActionMap.Keys) {
					if (_mediator != null) {
						_mediator.removeSubscriber (_NetSubIds[key], new string[] { key });
						// _NetSubIds.Remove(key);
						_NetSubIds[key]=0;
					}
				}
				_mediator = value;
				foreach (var key in _ActionMap.Keys) {
					if (_mediator != null) {
						_NetSubIds[key] = _mediator.subscribe (key, (o) => {
							_ActionMap[key] (o);
						}).id;
					}
				}
			}
		}
		protected Dictionary<string, int> _NetSubIds = new Dictionary<string, int> ();
		protected Dictionary<string, Action<object>> _ActionMap;

		public Dictionary<string, Action<object>> ActionMap {
			set {
				_ActionMap = value;
			}
		}
	}

	public class SocketHub {
		NetHub _Nethub;
		public Mediator mediator {
			get {
				return _Nethub.mediator;
			}
			set {
				_Nethub.mediator = value;
			}
		}
		public event Action<string, object> NotifyNetEvent;
		public void OnNetEvent (string key, object o) {
			mediator.publish (key, o);
		}
		public void OnReceiveData (RespRawData respdata) {
			NotifyNetEvent ("socket-received-data", respdata);
		}
		public event Action<ReqInfo> NotifySendData;
		public event Action<ReqInfo> NotifyPostData;
		public SocketHub () {
			_Nethub = new NetHub ();
			_Nethub.ActionMap = new Dictionary<string, Action<object>> {
				["socket-send-data"] = (o) => NotifySendData ((ReqInfo) o),
				["socket-post-data"] = (o) => NotifyPostData ((ReqInfo) o),
			};
		}

	}

	public class ClientHub {

		NetHub _Nethub;
		public Mediator mediator {
			get {
				return _Nethub.mediator;
			}
			set {
				_Nethub.mediator = value;
			}
		}
		public event Action<string, object> NotifyNetEvent;
		public void OnNetEvent (string key, object o) {
			mediator.publish (key, o);
		}
		public void OnSendData (ReqInfo reqinfo) {
			NotifyNetEvent ("socket-send-data", reqinfo);
		}
		public void OnPostData (ReqInfo reqinfo) {
			NotifyNetEvent ("socket-post-data", reqinfo);
		}
		public event Action<RespRawData> NotifyReceiveData;
		public ClientHub () {
			_Nethub = new NetHub ();
			_Nethub.ActionMap = new Dictionary<string, Action<object>> {
				["socket-received-data"] = (o) => NotifyReceiveData ((RespRawData) o),
			};
		}

	}
}