//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;

namespace Jgr.Gui {
	public class RedrawBlock : IDisposable {
		public readonly IntPtr Handle;
		const int WM_SETREDRAW = 0x000B;

		public RedrawBlock(IntPtr handle) {
			Handle = handle;
			NativeMethods.SendMessage(Handle, WM_SETREDRAW, 0, 0);
		}

		protected virtual void Dispose(bool disposing) {
			NativeMethods.SendMessage(Handle, WM_SETREDRAW, 1, 0);
		}

		~RedrawBlock() {
			Dispose(false);
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
