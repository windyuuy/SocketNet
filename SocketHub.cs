using System;
using System.Collections.Generic;
using TMediator;

namespace SocketNet {
	class NetHub {
		protected EventHub _eventhub;
		public EventHub eventhub {
			get {
				return _eventhub;
			}
			set {
				foreach (var key in _ActionMap.Keys) {
					if (_eventhub != null) {
						_eventhub.notify -= _NetSubIds[key].input;
						_NetSubIds[key] = null;
					}
				}
				_eventhub = value;
				foreach (var key in _ActionMap.Keys) {
					if (_eventhub != null) {
						var eventfilter = new EventFilter (key);
						var fn = _ActionMap[key];
						eventfilter.notify += (k, o) => fn (o);
						eventhub.notify += eventfilter.input;
						_NetSubIds[key] = eventfilter;
					}
				}
			}
		}
		protected Dictionary<string, EventFilter> _NetSubIds = new Dictionary<string, EventFilter> ();
		protected Dictionary<string, Action<object>> _ActionMap;

		public Dictionary<string, Action<object>> ActionMap {
			set {
				_ActionMap = value;
			}
		}
	}

	public class SocketHub {
		NetHub _Nethub;
		public EventHub eventhub {
			get {
				return _Nethub.eventhub;
			}
			set {
				_Nethub.eventhub = value;
			}
		}
		public event Action<string, object> NotifyNetEvent;
		public void OnNetEvent (string key, object o) => eventhub.input (key, o);
		public void OnReceiveData (RespRawData respdata) => NotifyNetEvent ("socket-received-data", respdata);
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
		public EventHub eventhub {
			get {
				return _Nethub.eventhub;
			}
			set {
				_Nethub.eventhub = value;
			}
		}
		public event Action<string, object> NotifyNetEvent;
		public void OnNetEvent (string key, object o) => eventhub.input (key, o);
		public void OnSendData (ReqInfo reqinfo) => NotifyNetEvent ("socket-send-data", reqinfo);
		public void OnPostData (ReqInfo reqinfo) => NotifyNetEvent ("socket-post-data", reqinfo);
		public event Action<RespRawData> NotifyReceiveData;
		public ClientHub () {
			_Nethub = new NetHub ();
			_Nethub.ActionMap = new Dictionary<string, Action<object>> {
				["socket-received-data"] = (o) => NotifyReceiveData ((RespRawData) o),
			};
		}

	}
}