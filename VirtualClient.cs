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

	struct SessionRawDataHead{
		public SessionId sessionid;
		public ReqId reqid;
	}

	struct SessionRawData{
		public SessionId sessionid;
		public ReqId reqid;
		// public int size;
		public SessionData data;
	}

	struct RespData{
		public SessionId sessionid;
		public ReqId reqid;
		public int size;
		public byte[] data;
	}

	public delegate void DataCallback(RespRawData o);

	public class VirtualClient {

		public event Action<ReqInfo> SendData;
		public event Action<ReqInfo> PostData;
		// public event Action<string,object> NotifyNetEvent;
		public void _SendData(ReqInfo reqinfo,string fmethod){
			switch(fmethod){
				case "send":
					// NotifyNetEvent("socket-send-data",reqinfo);
					SendData(reqinfo);
					break;
				case "post":
					// NotifyNetEvent("socket-post-data",reqinfo);
					PostData(reqinfo);
					break;
				default:
					throw new Exception(string.Format("no such method {0}",fmethod));
			}
		}

		public Mediator mediator;

		public void OnReceivedData(RespRawData reqinfo){
			var bytedata=reqinfo.data;
			var len=reqinfo.size;
			var rawdata=(SessionRawData)DataTypeUtil.BytesToStruct(bytedata,typeof(SessionRawData),len);
			var data=(byte[])rawdata.data;
			var datalen=data.Length;
			// var headlen=Marshal.SizeOf(typeof(SessionRawDataHead));
			// var rawdata=(SessionRawDataHead)DataTypeUtil.BytesToStruct(bytedata,typeof(SessionRawDataHead),headlen);
			// var data=new byte[len-headlen];
			// Buffer.BlockCopy(bytedata,headlen,data,0,len-headlen);

			mediator.publish("client-received-data",new RespData{
				sessionid=rawdata.sessionid,
				reqid=rawdata.reqid,
				size=datalen,
				data=data,
			});
		}

		protected SessionId curid=0;
		protected SessionId GenSessionId(){
			return curid++;
		}

		public bool Send(SessionInfo info,DataCallback fn){
			return _Send(info,fn,"send");
		}
		public bool Post(SessionInfo info,DataCallback fn){
			return _Send(info,fn,"post");
		}
		public bool _Send(SessionInfo info,DataCallback fn,string fmethod){
			info.sessionid=GenSessionId();
			
			var sessionid=info.sessionid;
			var reqid=info.reqid;
			var rawdata=new SessionRawData{
				sessionid=sessionid,
				reqid=info.reqid,
				data=info.data,
			};
			
			var id=0;
			id=mediator.once("client-received-data",(o)=>{
				var reqinfo=(RespData)o;
				var bytedata=reqinfo.data;
				var size=reqinfo.size;
				fn(new RespRawData{data=bytedata,size=size});
			},new Options{
				predicate=(e)=>{
					var reqinfo=(RespData)e;
					return reqinfo.reqid==reqid && reqinfo.sessionid==sessionid;
				}
			}).id;
			
			{
				var len=Marshal.SizeOf(typeof(SessionRawDataHead))+((byte[])rawdata.data).Length;
				var bytedata=DataTypeUtil.StructToBytes(rawdata,len);
				var reqinfo=new ReqInfo{
					method=info.method,
					data=bytedata,
					len=len,
				};
				this._SendData(reqinfo,fmethod);
			}

			return true;
		}

		public int OnResp(ReqId reqid,DataCallback fn){
			return mediator.subscribe("client-received-data",(e)=>{
				var reqinfo=(RespData)e;
				var bytedata=reqinfo.data;
				var size=reqinfo.size;
				fn(new RespRawData{data=bytedata,size=size});

			},new Options{
				predicate=(e)=>{
					var reqinfo=(RespData)e;
					return reqinfo.reqid==reqid;
				}
			}).id;
		}

		public void OnSessionRespOnce(ReqId reqid,DataCallback fn){
			var id=0;
			id=OnResp(reqid,(o)=>{
				mediator.removeSubscriber(id,new string[]{"client-received-data"});

				fn(o);
			});
		}

		int _timeout=5000;
		public int Timeout{
			set{
				_timeout=value;
			}
			get{
				return _timeout;
			}
		}

	}
}