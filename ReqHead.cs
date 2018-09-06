using System;
using System.Collections.Generic;
using System.Text;

namespace SocketNet {
	public struct ReqHead {
		const Int32 ENABLE_SESSION = 1 << 0;
		const Int32 ENABLE_COMPRESS = 1 << 1;

		static bool IsEnabled ( in Int32 input, Int32 bit) {
			return 0 != (input & bit);
		}

		static Int32 EnableBit (ref Int32 input, Int32 bit) {
			return input |= bit;
		}

		static Int32 DisableBit (ref Int32 input, Int32 bit) {
			return input ^= bit;
		}

		public Options Option;
		public struct Options {
			public Int32 Value;

			public bool IsSessionEnabled {
				get {
					return IsEnabled (Value, ENABLE_SESSION);
				}
				set {
					if (value) {
						EnableBit (ref Value, ENABLE_SESSION);
					} else {
						DisableBit (ref Value, ENABLE_SESSION);
					}
				}
			}

			public bool IsCompressed {
				get {
					return IsEnabled (Value, ENABLE_COMPRESS);
				}
				set {
					if (value) {
						EnableBit (ref Value, ENABLE_COMPRESS);
					} else {
						DisableBit (ref Value, ENABLE_COMPRESS);
					}
				}
			}
		};

		public Int32 SessionId;
	};
}