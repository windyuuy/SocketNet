using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MyLogger;

/*
丢包检测
每次收到的sessionid和上次sessionid不连续，则以新的sessionid重传
丢包预防
每次发包都进行重复发包操作，减少回包时才检测到丢包的几率
*/
/*
丢包重传
丢包重传和过包、队列重制三种事件
丢包请求作为普通请求，收到丢包请求，派发丢包重传请求
收到丢包请求默认进行重传处理
*/
/*
分包策略
MTU
	局域网 1472字节
	英特网 548字节
包分片，用分片序号标记顺序，单包分片序号为0
分片作为普通单包传输，丢包按照丢包策略执行
*/
/*
数据结构
	sessionid
	reqid
	sliceindex
	curaccmin
*/
namespace SocketNet
{
    class UdpWrapper : SocketWrapper
    {

        public UdpWrapper()
        {
            log = new Logger("UdpWrapper");
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        protected override bool _Connect(string ip, int port, Action onSuccess = null, Action<Exception> onFailed = null)
        {
            _ReconnectTimes++;
            if (MaxReconnectTime != -1 && _ReconnectTimes > MaxReconnectTime)
            {
                _ReconnectTimes = 0;

                _isConnected = false;
                onFailed?.Invoke(new Exception("reconnect timeout"));
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

                        _isConnected = true;
                        onSuccess?.Invoke();
                    }
                }, null);
            }
            catch (InvalidOperationException excpt)
            {
                _isConnected = false;
                onFailed?.Invoke(excpt);
            }

            return true;
        }

        public override void Disconnect()
        {
            _isConnected = false;
        }

        protected bool _isConnected = false;
        public override bool IsConnected()
        {
            if (!_isConnected)
            {
                return false;
            }

            return base.IsConnected();
        }
    }
}