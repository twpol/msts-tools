//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jgr.Gui {
	/// <summary>
	/// Specifies when <see cref="AutoCenterWindows"/> should stop operations.
	/// </summary>
	public enum AutoCenterWindowsMode {
		AllWindows,
		FirstWindowOnly
	}

	/// <summary>
	/// Automatically centers all new windows shown by the current thread over a given owner <see cref="Form"/>.
	/// </summary>
	public sealed class AutoCenterWindows : IDisposable {
		const int GWL_HINSTANCE = -6;
		const int HCBT_ACTIVATE = 5;
		const int SWP_NOSIZE = 0x1;
		const int SWP_NOZORDER = 0x4;
		const int SWP_NOACTIVATE = 0x10;
		const int WH_CBT = 5;

		Form Owner;
		AutoCenterWindowsMode Mode;
		IntPtr Hook;
		bool Hooked;

		/// <summary>
		///  Initializes a new instance of the <see cref="AutoCenterWindows"/> class with a given <see cref="Form"/> and <see cref="AutoCenterWindowsMode"/>.
		/// </summary>
		/// <param name="owner">The <see cref="Form"/> over which all new windows should be centered.</param>
		/// <param name="mode">The <see cref="AutoCenterWindowsMode"/> to operate in; either <see cref="AutoCenterWindowsMode.AllWindows"/> or
		/// <see cref="AutoCenterWindowsMode.FirstWindowOnly"/>.</param>
        public AutoCenterWindows(Form owner, AutoCenterWindowsMode mode) {
			Owner = owner;
			Mode = mode;
			SetHook();
        }

		~AutoCenterWindows() {
			UnsetHook();
		}

		void SetHook() {
			if (!Hooked) {
				var cb = new NativeMethods.HookProc(WindowHookProc);
				Hook = NativeMethods.SetWindowsHookEx(WH_CBT, cb, NativeMethods.GetModuleHandle(null), NativeMethods.GetCurrentThreadId());
				Hooked = true;
			}
		}

		void UnsetHook() {
			if (Hooked) {
				NativeMethods.UnhookWindowsHookEx(Hook);
				Hooked = false;
			}
		}

		IntPtr WindowHookProc(int lMsg, IntPtr wParam, IntPtr lParam) {
			if (lMsg == HCBT_ACTIVATE) {
				var dialog = new RECT();
				NativeMethods.GetWindowRect(wParam, ref dialog);
				var x = (Owner.Left + (Owner.Right - Owner.Left) / 2) - ((dialog.Right - dialog.Left) / 2);
				var y = (Owner.Top + (Owner.Bottom - Owner.Top) / 2) - ((dialog.Bottom - dialog.Top) / 2);
				var screen = Screen.FromHandle(wParam);
				if (x + dialog.Width > screen.WorkingArea.Right) x = screen.WorkingArea.Right - dialog.Width;
				if (y + dialog.Height > screen.WorkingArea.Bottom) y = screen.WorkingArea.Bottom - dialog.Height;
				if (x < screen.WorkingArea.Left) x = screen.WorkingArea.Left;
				if (y < screen.WorkingArea.Top) y = screen.WorkingArea.Top;
				NativeMethods.SetWindowPos(wParam, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
				if (Mode == AutoCenterWindowsMode.FirstWindowOnly) {
					UnsetHook();
				}
			}
            return IntPtr.Zero;
        }

		#region IDisposable Members

		/// <summary>
		/// Releases all resources used by the <see cref="AutoCenterWindows"/>.
		/// </summary>
		public void Dispose() {
			UnsetHook();
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
