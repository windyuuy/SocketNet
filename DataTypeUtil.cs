using System;
using System.Runtime.InteropServices;

namespace SocketNet {
	class DataTypeUtil {

		public static byte[] StructToBytes (object structObj, int size = 0) {
			if (size == 0) {
				size = Marshal.SizeOf (structObj); //得到结构体大小
			}
			IntPtr buffer = Marshal.AllocHGlobal (size); //开辟内存空间
			try {
				Marshal.StructureToPtr (structObj, buffer, false); //填充内存空间
				byte[] bytes = new byte[size];
				Marshal.Copy (buffer, bytes, 0, size); //填充数组
				return bytes;
			} catch (Exception ex) {
				// Debug.LogError("struct to bytes error:" + ex);
				return null;
			} finally {
				Marshal.FreeHGlobal (buffer); //释放内存
			}
		}
		public static object BytesToStruct (byte[] bytes, Type strcutType, int nSize, int offset = 0) {
			if (bytes == null) {
				// Debug.LogError("null bytes!!!!!!!!!!!!!");
			}
			int size = Marshal.SizeOf (strcutType);
			IntPtr buffer = Marshal.AllocHGlobal (nSize);
			//Debug.LogError("Type: " + strcutType.ToString() + "---TypeSize:" + size + "----packetSize:" + nSize);
			try {
				Marshal.Copy (bytes, offset, buffer, nSize);
				return Marshal.PtrToStructure (buffer, strcutType);
			} catch (Exception ex) {
				// Debug.LogError("Type: " + strcutType.ToString() + "---TypeSize:" + size + "----packetSize:" + nSize);
				return null;
			} finally {
				Marshal.FreeHGlobal (buffer);
			}
		}

	}
}