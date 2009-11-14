//------------------------------------------------------------------------------
// Simis Editor, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jgr.Gui {
	public enum AutoCenterWindowsMode {
		AllWindows,
		FirstWindowOnly
	}

	[StructLayout(LayoutKind.Sequential)]
	struct RECT {
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
		public int Width { get { return Right - Left; } }
		public int Height { get { return Bottom - Top; } }
	}

	/// <summary>
	/// Automatically centers all new windows shown by the current thread over a given owner <see cref="Form"/>.
	/// </summary>
	public class AutoCenterWindows : IDisposable {
		delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
		
		[DllImport("kernel32.dll")]
		static extern int GetCurrentThreadId();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		static extern IntPtr GetModuleHandle(string modName);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		static extern bool SetWindowPos(HandleRef hWnd, HandleRef hWndInsertAfter, int x, int y, int cx, int cy, int flags);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SetWindowsHookEx(int hookid, HookProc pfnhook, HandleRef hinst, int threadid);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        static extern bool UnhookWindowsHookEx(HandleRef hhook);

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
				var cb = new HookProc(WindowHookProc);
				Hook = SetWindowsHookEx(WH_CBT, cb, new HandleRef(null, GetModuleHandle(null)), GetCurrentThreadId());
				Hooked = true;
			}
		}

		void UnsetHook() {
			if (Hooked) {
				UnhookWindowsHookEx(new HandleRef(null, Hook));
				Hooked = false;
			}
		}

		IntPtr WindowHookProc(int lMsg, IntPtr wParam, IntPtr lParam) {
			if (lMsg == HCBT_ACTIVATE) {
				var dialog = new RECT();
				GetWindowRect(new HandleRef(null, wParam), ref dialog);
				var x = (Owner.Left + (Owner.Right - Owner.Left) / 2) - ((dialog.Right - dialog.Left) / 2);
				var y = (Owner.Top + (Owner.Bottom - Owner.Top) / 2) - ((dialog.Bottom - dialog.Top) / 2);
				var screen = Screen.FromHandle(wParam);
				if (x + dialog.Width > screen.WorkingArea.Right) x = screen.WorkingArea.Right - dialog.Width;
				if (y + dialog.Height > screen.WorkingArea.Bottom) y = screen.WorkingArea.Bottom - dialog.Height;
				if (x < screen.WorkingArea.Left) x = screen.WorkingArea.Left;
				if (y < screen.WorkingArea.Top) y = screen.WorkingArea.Top;
				SetWindowPos(new HandleRef(null, wParam), new HandleRef(null, IntPtr.Zero), x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
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
		}

		#endregion
	}
}
