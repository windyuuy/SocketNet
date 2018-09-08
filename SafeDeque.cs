using System;
using System.Threading;
using Nito.Collections;

namespace SocketNet {

	public class SafeDeque<T> {
		protected Deque<T> _Deque;
		private Object thisLock = new Object ();
		public SafeDeque () {
			_Deque = new Deque<T> ();
		}

		public int Count {
			get {
				lock (thisLock) {
					return _Deque.Count;
				}
			}
		}

		public T GetRemoveFromFront () {
			lock (thisLock) {
				return _Deque.RemoveFromFront ();
			}
		}

		public T this [int index] {
			get {
				lock (thisLock) {
					return _Deque[index];
				}
			}
			set {
				lock (thisLock) {
					_Deque[index] = value;
				}
			}
		}

		public void AddToBack(T value) {
			lock (thisLock) {
				_Deque.AddToBack(value);
			}
		}

		public void AddToFront(T value) {
			lock (thisLock) {
				_Deque.AddToFront(value);
			}
		}

		public void Clear() {
			lock (thisLock) {
				_Deque.Clear();
			}
		}
	}
}