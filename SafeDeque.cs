using System;
using System.Threading;
using Nito.Collections;

namespace SocketNet {

	public class SafeDeque<T> {
		protected Deque<T> _Deque;
		private Object thisLock = new Object ();
		Semaphore _QueueEvent = new Semaphore (0, 100000000);
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

		public T RemoveFromFront () {
			_QueueEvent.WaitOne ();
			lock (thisLock) {
				return _Deque.RemoveFromFront ();
			}
		}

		public T TryRemoveFromFront () {
			_QueueEvent.WaitOne ();
			lock (thisLock) {
				if (_Deque.Count > 0) {
					return _Deque.RemoveFromFront ();
				} else {
					_QueueEvent.Release ();
					return default(T);
				}
			}
		}

		public T this [int index] {
			get {
				_QueueEvent.WaitOne ();
				lock (thisLock) {
					_QueueEvent.Release ();
					return _Deque[index];
				}
			}
			set {
				lock (thisLock) {
					_Deque[index] = value;
				}
			}
		}

		public void AddToBack (T value) {
			lock (thisLock) {
				_Deque.AddToBack (value);
				_QueueEvent.Release ();
			}
		}

		public void AddToFront (T value) {
			lock (thisLock) {
				_Deque.AddToFront (value);
				_QueueEvent.Release ();
			}
		}

		public void Clear () {
			lock (thisLock) {
				_QueueEvent.Close ();
				_Deque.Clear ();
			}
		}

		public void SetEvent () {
			_QueueEvent.Release ();
		}
		public void WaitOneEvent () {
			_QueueEvent.WaitOne ();
		}

	}
}