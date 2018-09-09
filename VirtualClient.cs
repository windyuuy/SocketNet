using System;
using System.Runtime.InteropServices;
using TMediator;

namespace SocketNet {

	using SessionId = System.UInt32;
	using ReqId = System.Int32;
	using SessionData = Object;

	public struct SessionInfo {
		public SessionId sessionid;
		public ReqId reqid;
		public ReqMethod method;
		public SessionData data;
	}

	struct SessionRawDataHead {
		public SessionId sessionid;
		public ReqId reqid;
	}

	struct SessionRawData {
		public SessionId sessionid;
		public ReqId reqid;
		// public int size;
		public SessionData data;
	}

	struct RespData {
		public SessionId sessionid;
		public ReqId reqid;
		public int size;
		public byte[] data;
	}

	public delegate void DataCallback (RespRawData o);

	public class VirtualClient {

		public event Action<ReqInfo> SendData;
		public event Action<ReqInfo> PostData;
		// public event Action<string,object> NotifyNetEvent;
		public void _SendData (ReqInfo reqinfo, string fmethod) {
			switch (fmethod) {
				case "send":
					// NotifyNetEvent("socket-send-data",reqinfo);
					SendData (reqinfo);
					break;
				case "post":
					// NotifyNetEvent("socket-post-data",reqinfo);
					PostData (reqinfo);
					break;
				default:
					throw new Exception (string.Format ("no such method {0}", fmethod));
			}
		}

		// public Mediator mediator;
		public EventHub eventhub;

		public void OnReceivedData (RespRawData reqinfo) {
			var bytedata = reqinfo.data;
			var len = reqinfo.size;

			// var rawdata = (SessionRawData) DataTypeUtil.BytesToStruct (bytedata, typeof (SessionRawData), len);
			// var data = (byte[]) rawdata.data;
			// var datalen = data.Length;

			int headsize = Marshal.SizeOf (typeof (SessionRawDataHead));
			var datalen = len - headsize;
			var rawdata = (SessionRawDataHead) DataTypeUtil.BytesToStruct (bytedata, typeof (SessionRawDataHead), headsize);
			var data = new byte[datalen];
			Buffer.BlockCopy (bytedata, headsize, data, 0, datalen);

			var respdata = new RespData {
				sessionid = rawdata.sessionid,
					reqid = rawdata.reqid,
					size = datalen,
					data = data,
			};

			eventhub.input ("client-received-data", respdata);
		}

		protected SessionId curid = 0;
		protected SessionId GenSessionId () {
			return curid++;
		}

		public bool Send (SessionInfo info, DataCallback fn) => _Send (info, fn, "send");
		public bool Post (SessionInfo info, DataCallback fn) => _Send (info, fn, "post");
		public bool _Send (SessionInfo info, DataCallback fn, string fmethod) {
			info.sessionid = GenSessionId ();

			var sessionid = info.sessionid;
			var reqid = info.reqid;

			{
				var eventfilter = new EventFilter ("client-received-data");
				eventfilter.filter = (key, e) => {
					var reqinfo = (RespData) e;
					return reqinfo.reqid == reqid && reqinfo.sessionid == sessionid;
				};
				eventfilter.once += (key, o) => {
					var reqinfo = (RespData) o;
					var bytedata = reqinfo.data;
					var size = reqinfo.size;
					fn (new RespRawData { data = bytedata, size = size });
				};
				eventhub.notify += eventfilter.input;

			}
			// var rawdata = new SessionRawData {
			// 	sessionid = sessionid,
			// 		reqid = info.reqid,
			// 		data = info.data,
			// };
			var rawdata = new SessionRawDataHead {
				sessionid = sessionid,
					reqid = info.reqid,
			};

			{
				// var len = Marshal.SizeOf (typeof (SessionRawDataHead)) + ((byte[]) rawdata.data).Length;
				// var bytedata = DataTypeUtil.StructToBytes (rawdata, len);

				var rawbytedata = (byte[]) info.data;
				int headlen = Marshal.SizeOf (typeof (SessionRawDataHead));
				var len = headlen + rawbytedata.Length;
				var bytedata = new byte[len];
				Buffer.BlockCopy (DataTypeUtil.StructToBytes (rawdata, headlen), 0, bytedata, 0, headlen);
				Buffer.BlockCopy (rawbytedata, 0, bytedata, headlen, rawbytedata.Length);
				if (len != bytedata.Length) {
					throw new Exception ("");
				}

				var reqinfo = new ReqInfo {
					method = info.method,
						data = bytedata,
						len = len,
				};
				this._SendData (reqinfo, fmethod);
			}

			return true;
		}

		public EventFilter OnResp (ReqId reqid, DataCallback fn) {
			{
				var eventfilter = new EventFilter ("client-received-data");
				eventfilter.filter = (key, e) => {
					var reqinfo = (RespData) e;
					return reqinfo.reqid == reqid;
				};
				eventfilter.notify += (key, o) => {
					var reqinfo = (RespData) o;
					var bytedata = reqinfo.data;
					var size = reqinfo.size;
					fn (new RespRawData { data = bytedata, size = size });
				};
				eventhub.notify += eventfilter.input;
				return eventfilter;
			}
		}

		public EventFilter OnSessionRespOnce (ReqId reqid, DataCallback fn) {
			{
				var eventfilter = new EventFilter ("client-received-data");
				eventfilter.filter = (key, e) => {
					var reqinfo = (RespData) e;
					return reqinfo.reqid == reqid;
				};
				eventfilter.once += (key, o) => {
					var reqinfo = (RespData) o;
					var bytedata = reqinfo.data;
					var size = reqinfo.size;
					fn (new RespRawData { data = bytedata, size = size });
				};
				eventhub.notify += eventfilter.input;
				return eventfilter;
			}
		}

		int _timeout = 5000;
		public int Timeout {
			set {
				_timeout = value;
			}
			get {
				return _timeout;
			}
		}

	}
}