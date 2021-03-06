﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MyLogger;

namespace SocketNet {
	#region define structs
	public enum ReqMethod {
		// byte
		Byte = 0,
		// packet
		Packet,
		// file
		File,
		// udp data
		UdpData,
		// custom
		Custom,
	}
	public struct ReqInfo {
		public ReqMethod method;
		public int len;
		public byte[] data;
	}

	struct RespHeadInfo {
		public const int headmark = 12334;
		public int mark;
		public int len;
	}

	struct RespEndInfo {
		public const int endmark = 12434;
		public int mark;
	}

	public struct RespRawData {
		public int size;
		public byte[] data;
	}
	#endregion

	public class SocketWrapper:ISocketWrapper {
		#region construct and desctruct
		protected Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        string _ip = null;
		int _port = -1;
		uint _ConnnectTimeMax = 1;

        protected AutoResetEvent _ConnEventR =new AutoResetEvent (false);
        protected AutoResetEvent _ConnEventW =new AutoResetEvent (false);
        // AutoResetEvent _QueueEvent;
        protected SafeDeque<ReqInfo> _PostDeque=new SafeDeque<ReqInfo> ();
        protected SafeDeque<RespRawData> _ReceivedEventQueue=new SafeDeque<RespRawData> ();

		protected Logger log = new Logger ("SocketWrapper");
		public SocketWrapper() {
				_CurWaitReadSize = _RespInfoSize;
				_WriteTread=new Thread (this.WriteProcess);
				_ReadThread=new Thread (this.ReadProcess);
				_EventThread=new Thread (this.ProcessReceiveEvents);
				// _QueueEvent = new AutoResetEvent (false);
			}

			~SocketWrapper () {
				this.FinishProcess (true);
			}
		#endregion

		#region process
		Thread _WriteTread=null;
		Thread _ReadThread=null;
		Thread _EventThread=null;
		public virtual void BeginProcess () {
			_WriteTread.Start ();
			_ReadThread.Start ();
			_EventThread.Start ();
		}

		public virtual Task FinishProcess (bool force = false) {
			_AbortReadStream = true;
			_AbortWriteStream = true;
			_AbortEventProcess = true;

			if (!this._ReadingStream) { }

			if (!this._WritingStream) {
				// if (_PostDeque.Count <= 0) {
				// 	_QueueEvent.Set ();
				// }
				_PostDeque.SetEvent ();
			}

			if (!this._ProcessingEvent) {
				_ReceivedEventQueue.SetEvent ();
			}
			var wait = Task.Run (() => {
				_ProcessCount.WaitOne ();
				_ProcessCount.WaitOne ();
				_ProcessCount.WaitOne ();
				_ProcessCount.Release ();
				_ProcessCount.Release ();
				_ProcessCount.Release ();
				_PostDeque.WaitOneEvent ();
				_ReceivedEventQueue.WaitOneEvent ();
			});

            return Task.Run(async () =>
            {
                await wait;
                _ReadThread.Join();
                _WriteTread.Join();
                _EventThread.Join();
            });
		}
		#endregion

		#region read and write process
		protected AutoResetEvent _OPSocket = new AutoResetEvent (true);
		protected int _Receive (byte[] buffer, int size, SocketFlags socketFlags) {
			// _OPSocket.WaitOne ();
			// try {
			// 	int len = _socket.Receive (buffer, size, socketFlags);
			// 	return len;
			// } catch (Exception e) {
			// 	throw e;
			// } finally {
			// 	_OPSocket.Set ();
			// }
			int len = _socket.Receive (buffer, size, socketFlags);
			return len;

		}
		protected int _Send (byte[] buffer, int size, SocketFlags socketFlags) {
			// _OPSocket.WaitOne ();
			// try {
			// 	int len = _socket.Send (buffer, size, socketFlags);
			// 	return len;
			// } catch (Exception e) {
			// 	throw e;
			// } finally {
			// 	_OPSocket.Set ();
			// }
			int len = _socket.Send (buffer, size, socketFlags);
			return len;

		}
		protected bool _Poll (int microSeconds, SelectMode mode) {
			//_OPSocket.WaitOne ();
			bool ok = _socket.Poll (microSeconds, mode);
			//_OPSocket.Set ();
			return ok;
		}

		protected int _ReceiveTimeout = 2000;
		protected int _DataLackWaitingTime = 10;
		protected int _PollTimeout = 1000 * 1000;
		protected void _SetBlocking (bool b) {
			// _OPSocket.WaitOne ();
			_socket.Blocking = b;
			// _OPSocket.Set();

			// _socket.Blocking = true;
			// if (b) {
			// 	_socket.ReceiveTimeout = _ReceiveTimeout;
			// } else {
			// 	_socket.ReceiveTimeout = 1;
			// }
		}

		protected Semaphore _ProcessCount=new Semaphore (3, 3);
        protected Semaphore _DetectConnection =new Semaphore (1, 1);
		public virtual bool IsConnected () {
			if (!_socket.Connected) {
				return false;
			}

			_DetectConnection.WaitOne ();
			bool b = true;
			if (_Poll (1, SelectMode.SelectRead)) {
				try {
					_SetBlocking (false);
					byte[] tmp = new byte[_socket.ReceiveBufferSize];
					int nRead = _Receive (tmp, _socket.ReceiveBufferSize-4, SocketFlags.Peek);
					if (nRead == 0) {
						b = false;
					}
				} catch (SocketException e) {
					if ((!e.NativeErrorCode.Equals (10035))
						// && (!e.NativeErrorCode.Equals (10060))
					) {
						b = false;
					}
				} finally {
					_SetBlocking (true);
				}
			}

			if (b) {
				try {
					_SetBlocking (false);
					byte[] tmp = new byte[_socket.ReceiveBufferSize];
					int nRead = _Receive (tmp, _socket.ReceiveBufferSize-4, SocketFlags.Peek);
					_Send (tmp, 0, SocketFlags.None);
				} catch (SocketException e) {
					if ((!e.NativeErrorCode.Equals (10035))
						// && (!e.NativeErrorCode.Equals (10060))
					) {
						b = false;
					}
				} finally {
					_SetBlocking (true);
				}
			}
			_DetectConnection.Release ();
			return b;
		}

		protected bool _WritingStream = false;
		protected bool _AbortWriteStream = false;
		public virtual void WriteProcess () {
			_ProcessCount.WaitOne ();
			_socket.SendTimeout = _ReceiveTimeout;
			while (true) {
				if (_AbortWriteStream) {
					break;
				}

				// if (_PostDeque.Count <= 0) {
				// 	// log.Debug ("waitq");
				// 	_QueueEvent.WaitOne ();
				// 	// log.Debug ("exit waitq");
				// 	if (_PostDeque.Count <= 0) {
				// 		continue;
				// 	}
				// }
				if (!this.IsConnected ()) {
					// log.Debug ("waitcn");
					_ConnEventW.WaitOne ();
					// log.Debug ("exit waitcn");
					if (!this.IsConnected ()) {
						continue;
					}
				}

				if (!_Poll (_PollTimeout, SelectMode.SelectWrite)) {
					continue;
				}

				try {
					var info = _PostDeque[0];
					var respinfo = new RespHeadInfo ();
					respinfo.len = info.len;
					respinfo.mark = RespHeadInfo.headmark;
					var respend = new RespEndInfo ();
					respend.mark = RespEndInfo.endmark;
					var respheadsize = Marshal.SizeOf (respinfo);
					var respendsize = Marshal.SizeOf (respend);
					var headBytes = DataTypeUtil.StructToBytes (respinfo, respheadsize);
					var endBytes = DataTypeUtil.StructToBytes (respend, respendsize);
					var respinfosize = respheadsize + respinfo.len + respendsize;
					var respBytes = new byte[respinfosize + 4];
					Buffer.BlockCopy (headBytes, 0, respBytes, 0, respheadsize);
					Buffer.BlockCopy (info.data, 0, respBytes, respheadsize, info.len);
					Buffer.BlockCopy (endBytes, 0, respBytes, respheadsize + info.len, respendsize);
					_WritingStream = true;
                    // Console.WriteLine($"send data size: {respinfosize}");
					int nResult = _Send (respBytes, respinfosize, SocketFlags.None);
					//totalsentsize += nResult;
					//log.Debug ("Sent: {0} {1}", totalsentsize, nResult);
					_WritingStream = false;
					_PostDeque.RemoveFromFront ();
				} catch (Exception e) {
					_WritingStream = false;
				}
			}
			log.Debug ("exit writing");
			_ProcessCount.Release ();
		}

		//int totalrecvsize = 0;
		//int totalsentsize = 0;

		protected bool _ReadingStream = false;
		protected bool _ProcessingEvent = false;
		protected bool _AbortReadStream = false;
		protected bool _AbortEventProcess = false;
		protected int _RespInfoSize = Marshal.SizeOf (typeof (RespHeadInfo));
		protected int _CurWaitReadSize = 0;
        public virtual void ReadProcess()
        {
            _ProcessCount.WaitOne ();
            _socket.ReceiveTimeout = _ReceiveTimeout;
			while (true) {
                if (_AbortReadStream) {
					break;
				}
                if (_socket.Available < _CurWaitReadSize) {
					if (!this.IsConnected ()) {
                        _ConnEventR.WaitOne ();
                        if (!this.IsConnected ()) {
                            continue;
						}
					}

					if (!_Poll (_PollTimeout, SelectMode.SelectRead)) {
                        continue;
					}
				}

                try {
                    int respheadsize = Marshal.SizeOf (typeof (RespHeadInfo));
                    byte[] recvBytes = new byte[_socket.ReceiveBufferSize];// new byte[respheadsize + 4];
					int nResult = _Receive (recvBytes, _socket.ReceiveBufferSize-4/*respheadsize*/, SocketFlags.Peek);
					var respinfo = (RespHeadInfo) DataTypeUtil.BytesToStruct (recvBytes, typeof (RespHeadInfo), respheadsize);
					if (nResult == 0) {
                        _socket.Receive(recvBytes, 0, SocketFlags.None);
                        continue;
					} else if (respinfo.mark != RespHeadInfo.headmark) {
						log.Warn ("unmatch headmark");
						_Receive (recvBytes, 1, SocketFlags.None);
						continue;
					}
					const int respendsize = sizeof (uint);
					int bodysize = respinfo.len;
					var totalsize = respheadsize + bodysize + respendsize;
					if (_socket.Available < totalsize) {
						_CurWaitReadSize = totalsize;
						continue;
					}
					byte[] respBytes = new byte[totalsize + 4];
					byte[] bodyBytes = new byte[bodysize + 4];
					byte[] endBytes = new byte[respendsize + 4];

					var nResult2 = _Receive (respBytes, totalsize, SocketFlags.Peek);
					if (nResult2 == 0 || nResult2 < totalsize) {
						Thread.Sleep (_DataLackWaitingTime);
						continue;
					}
					//totalrecvsize += nResult2;
					//log.Debug("size: {0} {1}", totalrecvsize, nResult);
					_CurWaitReadSize = _RespInfoSize;

					Buffer.BlockCopy (respBytes, respheadsize, bodyBytes, 0, bodysize);
					Buffer.BlockCopy (respBytes, respheadsize + bodysize, endBytes, 0, respendsize);

					var end = (RespEndInfo) DataTypeUtil.BytesToStruct (endBytes, typeof (RespEndInfo), respendsize);

					if (end.mark != RespEndInfo.endmark) {
						log.Warn ("error:unmatched mark");
					}
					_ReadingStream = true;
					_Receive (respBytes, totalsize, SocketFlags.None);
					_ReadingStream = false;
					// var str = Encoding.Unicode.GetString (bodyBytes, 0, bodysize);
					// log.Debug (str);

					this._OnReceiveData (bodyBytes, bodysize);

				} catch (SocketException e) {
                    log.Error(string.Format("socket error: {0}", e));
                } catch (InvalidOperationException e) {
                    log.Error(string.Format("socket op error: {0}", e));
                } catch (Exception e) {
					log.Error (string.Format ("error: {0}", e));
				} finally {
					_ReadingStream = false;
				}
			}
			log.Debug ("exit reading");
			_ProcessCount.Release ();
		}

		protected void _OnReceiveData (byte[] bodyBytes, int bodysize) {
            // Console.WriteLine($"received data: {bodysize}");
			var respdata = new RespRawData { data = bodyBytes, size = bodysize };
			this._ReceivedEventQueue.AddToBack (respdata);
		}

		#endregion
		#region received data process
		protected virtual void ProcessReceiveEvents () {
			_ProcessCount.WaitOne ();
			while (true) {
				if (_AbortEventProcess) {
					break;
				}
				if (_socket.Available < _CurWaitReadSize && _ReceivedEventQueue.Count <= 0) {
					if (!IsConnected ()) {
						try {
							NotifyDisconnect?.Invoke ();
						} catch (Exception e) {
							log.Error (string.Format ("error: {0}", e));
						}
					}
				}
				var respdata = _ReceivedEventQueue.TryRemoveFromFront ();
				if (respdata.data == null) {
					continue;
				}
				try {
					NotifyReceivedData?.Invoke (respdata);
				} catch (Exception e) {
					log.Error (string.Format ("error: {0}", e));
				}
			}
			log.Debug ("exit processing event");
			_ProcessCount.Release ();
		}

		public virtual event Action NotifyDisconnect;
		public virtual event Action<RespRawData> NotifyReceivedData;

		public virtual void Post (ReqInfo info) {
			// var count = _PostDeque.Count;
			this._PostDeque.AddToBack (info);
			// if (count == 0 && _PostDeque.Count != 0) {
			// 	_QueueEvent.Set ();
			// }
		}

		public virtual void Send (ReqInfo info) {
			// var count = _PostDeque.Count;
			this._PostDeque.AddToFront (info);
			// if (count == 0 && _PostDeque.Count != 0) {
			// 	_QueueEvent.Set ();
			// }
		}

		public virtual void AbortAllReq () {
			_PostDeque.Clear ();
		}
		#endregion

		#region connect
        public virtual Task ConnectAsync(string ip,int port)
        {
            return Task.Run(() => {
                var e = new AutoResetEvent(false);
                Exception ee = null;
                this._Connect(_ip, _port, () => {
                    e.Set();
                }, (err) =>
                {
                    ee = err;
                    e.Set();
                });
                e.WaitOne();
                if (ee != null)
                {
                    throw ee;
                }
                return;
            });
        }

		public virtual bool Connect (string ip, int port) {
			_ip = ip;
			_port = port;

			if (_socket.Connected) {
				return true;
			}
			if (_ReconnectTimes > 0) {
				return true;
			}
            var e = new AutoResetEvent(false);
            var ret = false;
            Exception ee=null;
            this._Connect (_ip, _port,()=> {
                ret = true;
                e.Set();
            },(err)=>
            {
                ee = err;
                e.Set();
            });
            e.WaitOne();
            if (ee != null)
            {
                log.Error(ee);
            }
            return ret;
		}

		protected int _ReconnectTimes = 0;
		public int MaxReconnectTime = -1;
		public int ReconnectInterval = 3000;
		protected virtual bool _Connect (string ip, int port,Action onSuccess=null,Action<Exception> onFailed=null) {
			_ReconnectTimes++;
			if (MaxReconnectTime != -1 && _ReconnectTimes > MaxReconnectTime) {
				_ReconnectTimes = 0;
                if (onFailed != null)
                {
                    onFailed(new Exception("reconnect timeout"));
                }
                return false;
			}
            try
            {
                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                _socket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), port), (ar) =>
                {
                    if (_socket.Connected)
                    {
                        _socket.EndConnect(ar);
                        _ConnEventR.Set();
                        _ConnEventW.Set();
                        _ReconnectTimes = 0;
                        this._OnConnected();
                        if (onSuccess != null)
                        {
                            onSuccess();
                        }
                    }
                }, null);
            }catch(InvalidOperationException excpt)
            {
                if (onFailed != null)
                {
                    onFailed(excpt);
                }
            }

            System.Timers.Timer t = new System.Timers.Timer(ReconnectInterval);
            t.Elapsed += new System.Timers.ElapsedEventHandler((sender, e2) =>
            {
                if (_socket.Connected)
                {
                    return;
                }
                this._Connect(ip, port);
            });
            t.AutoReset = false;
            t.Enabled = true;
            return true;
		}

		public virtual event Action NotifyConnected;
		public void _OnConnected () {
			try {
				this.NotifyConnected?.Invoke ();
			} catch (Exception e) {
				log.Error (string.Format ("error: {0}", e));
			}
		}

		bool _Reconnect () {
			Disconnect ();
			return Connect (_ip, _port);
		}

		public virtual void Disconnect () {
			if (_socket.Connected) {
				_socket.Disconnect (true);
			}
		}

        public virtual void Close()
        {
            this.Disconnect();
            _socket.Close();
        }

		public virtual bool MaintainConnection () {
			if (_socket.Connected) {
				if (IsConnected ()) {
					return true;
				} else {
					_Reconnect ();
				}
			} else {
				Connect (_ip, _port);
			}
			return true;
		}

		#endregion
	}
}