using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketNet {
    public class AsyncTask {
        public delegate void EDataCallback<T> (T o);
        internal static Func<Req, Task<Resp>> Make<Req,Resp> (Func<Req, DataCallback, bool> fn) {
            return (Req req) => {
                return Task.Run (() => {
                    var e = new AutoResetEvent (false);
                    object data = null;
                    fn (req, (o) => {
                        data = o;
                        e.Set ();
                    });
                    e.WaitOne ();
                    return (Resp) data;
                });
            };
        }

        internal static Task<Resp> Run<Req,Resp> (Func<Req, DataCallback, bool> fn, Req req) {
            return Task.Run (() => {
                var e = new AutoResetEvent (false);
                object data = null;
                fn (req, (o) => {
                    data = o;
                    e.Set ();
                });
                e.WaitOne ();
                return (Resp) data;
            });
        }
    }
}