using System;

namespace SocketNet {
	class EventHub {
		public event Action<string, object> notify;
		public void input(string key,object paras)=>this.notify(key,paras);
	}
}