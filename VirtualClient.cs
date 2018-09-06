using System;
using System.Runtime.InteropServices;
using System.Threading;
using TMediator;
using ArgList = System.Object;

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

	public delegate void EventCallback(ArgList arglist);
	public delegate void DataCallback(byte[] data,int len);

	public class VirtualClient {

		protected Mediator _mediator;
		public Mediator mediator{
			get{
				return _mediator;
			}
			set{
				if(_mediator!=null){
					_mediator.removeSubscriber(subscribeid,new string[]{"socket-received-data"});
				}
				_mediator=value;
				subscribeid=_mediator.subscribe("socket-received-data",this._onReceivedData).id;
			}
		}

		public VirtualClient () { }

		int subscribeid;
		public void init(Mediator mediator){
		}

		protected void _onReceivedData(ArgList e){
			var reqinfo=(RespRawData)e;
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
			info.sessionid=GenSessionId();
			
			var sessionid=info.sessionid;
			var reqid=info.reqid;
			var rawdata=new SessionRawData{
				sessionid=sessionid,
				reqid=info.reqid,
				data=info.data,
			};
			
			var id=0;
			id=_mediator.once("client-received-data",(e)=>{
				var reqinfo=(RespData)e;
				var bytedata=reqinfo.data;
				var len=reqinfo.size;
				fn(bytedata,len);
			},new Options{
				predicate=(e)=>{
					var reqinfo=(RespData)e;
					return reqinfo.reqid==reqid && reqinfo.sessionid==sessionid;
				}
			}).id;
			
			{
				var len=Marshal.SizeOf(typeof(SessionRawDataHead))+((byte[])rawdata.data).Length;
				var bytedata=DataTypeUtil.StructToBytes(rawdata,len);
				_mediator.publish("socket-send-data",new ReqInfo{
					method=info.method,
					data=bytedata,
					len=len,
				});
			}

			return true;
		}

		public int OnResp(ReqId reqid,DataCallback fn){
			return mediator.subscribe("client-received-data",(e)=>{
				var reqinfo=(RespData)e;
				var bytedata=reqinfo.data;
				var len=reqinfo.size;
				fn(bytedata,len);

			},new Options{
				predicate=(e)=>{
					var reqinfo=(RespData)e;
					return reqinfo.reqid==reqid;
				}
			}).id;
		}

		public void OnSessionRespOnce(ReqId reqid,DataCallback fn){
			var id=0;
			id=OnResp(reqid,(databytes,len)=>{
				mediator.removeSubscriber(id,new string[]{"client-received-data"});

				fn(databytes,len);
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