//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jgr.Gui {
	public class EditState {
		public Form Owner { get; private set; }

		public EditState(Form owner) {
			Owner = owner;
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
		internal static extern IntPtr GetFocus();

		const int WM_CUT = 0x0300;
		const int WM_COPY = 0x0301;
		const int WM_PASTE = 0x0302;
		const int WM_CLEAR = 0x0303;

		static Control GetFocusedControl() {
			var handle = GetFocus();
			if (handle == IntPtr.Zero) {
				return null;
			}
			return Control.FromHandle(handle);
		}

		TextBox GetTextBox() {
			var control = GetFocusedControl();
			var textbox = control as TextBox;
			if (textbox != null) {
				return textbox;
			}
			return null;
		}

		public bool CanCut {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && textbox.Enabled && !textbox.ReadOnly && (textbox.SelectionLength > 0);
			}
		}

		public bool CanCopy {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && (textbox.SelectionLength > 0);
			}
		}

		public bool CanPaste {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && textbox.Enabled && !textbox.ReadOnly && Clipboard.ContainsText(TextDataFormat.Text);
			}
		}

		public bool CanDelete {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && textbox.Enabled && !textbox.ReadOnly && (textbox.SelectionLength > 0);
			}
		}

		public bool CanSelectAll {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && textbox.CanSelect;
			}
		}

		public void DoCut() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			if (SendMessage(textbox.Handle, WM_CUT, 0, 0) != 0) throw new Win32Exception();
		}

		public void DoCopy() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			if (SendMessage(textbox.Handle, WM_COPY, 0, 0) != 0) throw new Win32Exception();
		}

		public void DoPaste() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			if (SendMessage(textbox.Handle, WM_PASTE, 0, 0) != 0) throw new Win32Exception();
		}

		public void DoDelete() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			if (SendMessage(textbox.Handle, WM_CLEAR, 0, 0) != 0) throw new Win32Exception();
		}

		public void DoSelectAll() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			textbox.SelectAll();
		}
	}
}
