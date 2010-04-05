//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jgr.Gui {
	[StructLayout(LayoutKind.Sequential)]
	struct RECT {
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
		public int Width { get { return Right - Left; } }
		public int Height { get { return Bottom - Top; } }
	}

	[Flags]
	enum TaskDialogCommonButtons {
		None = 0x0000,
		Ok = 0x0001,
		Yes = 0x0002,
		No = 0x0004,
		Cancel = 0x0008,
		Retry = 0x0010,
		Close = 0x0020,
	}

	public enum TaskDialogCommonIcon {
		None = 0x0000,
		Warning = 0xFFFF,
		Error = 0xFFFE,
		Information = 0xFFFD,
		Shield = 0xFFFC,
	}

	[Flags]
	enum TaskDialogFlags {
		None = 0x0000,
		EnableHyperlinks = 0x0001,
		UseHIconMain = 0x0002,
		UseHIconFooter = 0x0004,
		AllowDialogCancellation = 0x0008,
		UseCommandLinks = 0x0010,
		UseCommandLinksNoIcon = 0x0020,
		ExpandFooterArea = 0x0040,
		ExpandedByDefault = 0x0080,
		VerificationFlagChecked = 0x0100,
		ShowProgressBar = 0x0200,
		ShowMarqueeProgressBar = 0x0400,
		CallbackTimer = 0x0800,
		PositionRelativeToWindow = 0x1000,
		RTLLayout = 0x2000,
		NoDefaultRadioButton = 0x4000,
		CanBeMinimized = 0x8000,
		SizeToContent = 0x1000000,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct TaskDialogButton {
		public int ButtonID;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ButtonText;
	}

	public delegate int TaskDialogCallback(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr lpRefData);

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct TaskDialogConfig {
		public uint Size;
		public IntPtr Parent;
		public IntPtr Instance;
		public TaskDialogFlags Flags;
		public TaskDialogCommonButtons CommonButtons;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string WindowTitle;
		[MarshalAs(UnmanagedType.SysUInt)]
		public IntPtr MainIcon;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string MainInstruction;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Content;
		public uint ButtonCount;
		public IntPtr Buttons;
		public int DefaultButton;
		public uint RadioButtonCount;
		public IntPtr RadioButtons;
		public int DefaultRadioButton;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string VerificationText;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ExpandedInformation;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ExpandedControlText;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string CollapsedControlText;
		public IntPtr FooterIcon;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Footer;
		public TaskDialogCallback Callback;
		public IntPtr CallbackData;
		public uint Width;
	};

	internal class NativeMethods {
		[DllImport("kernel32.dll")]
		public static extern int GetCurrentThreadId();

		[DllImport("user32.dll")]
		public static extern IntPtr GetFocus();

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string modName);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

		public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetWindowsHookEx(int hookid, HookProc pfnhook, IntPtr hinst, int threadid);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhook);

		[DllImport("comctl32.dll", CharSet = CharSet.Unicode)]
		public static extern int TaskDialog(IntPtr hWndParent, IntPtr hInstance, string pszWindowTitle, string pszMainInstruction, string pszContent, TaskDialogCommonButtons dwCommonButtons, TaskDialogCommonIcon pszIcon, out DialogResult pnButton);

		[DllImport("comctl32.dll", CharSet = CharSet.Unicode)]
		public static extern int TaskDialog(IntPtr hWndParent, IntPtr hInstance, string pszWindowTitle, string pszMainInstruction, string pszContent, TaskDialogCommonButtons dwCommonButtons, string pszIcon, out DialogResult pnButton);

		[DllImport("comctl32.dll", CharSet = CharSet.Unicode)]
		public static extern int TaskDialogIndirect(ref TaskDialogConfig pTaskConfig, out int pnButton, out int pnRadioButton, out bool pfVerificationFlagChecked);
	}

}
