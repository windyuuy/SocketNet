using System;

namespace SocketNet {
	public class EventHub {
		public event Action<string, object> notify;
        public void input(string key, object paras) => this.notify?.Invoke(key, paras);

        public void clear () {
			this.notify = null;
		}
	}

	public class EventFilter {
		public event Action<string, object> notify;
		public event Action<string, object> once;
		public EventFilter (string key) {
			this.key = key;
		}
		public string key;
		public Func<string, object, bool> filter = null;
		public void input (string key, object paras) {
			if (key == this.key) {
				if ((this.filter == null) || (this.filter (key, paras))) {
					this.notify?.Invoke (key, paras);
					if (this.once != null) {
						this.once (key, paras);
						this.once = null;
					}
				}
			}
		}
		public void clear () {
			this.notify = null;
		}
	}
}