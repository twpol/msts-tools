//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Jgr.Gui {
	public static class EditState {
		const int WM_CUT = 0x0300;
		const int WM_COPY = 0x0301;
		const int WM_PASTE = 0x0302;
		const int WM_CLEAR = 0x0303;

		static Control GetFocusedControl() {
			var handle = NativeMethods.GetFocus();
			if (handle == IntPtr.Zero) {
				return null;
			}
			return Control.FromHandle(handle);
		}

		static TextBox GetTextBox() {
			var control = GetFocusedControl();
			var textbox = control as TextBox;
			if (textbox != null) {
				return textbox;
			}
			return null;
		}

		public static bool CanCut {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && textbox.Enabled && !textbox.ReadOnly && (textbox.SelectionLength > 0);
			}
		}

		public static bool CanCopy {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && (textbox.SelectionLength > 0);
			}
		}

		public static bool CanPaste {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && textbox.Enabled && !textbox.ReadOnly && Clipboard.ContainsText(TextDataFormat.Text);
			}
		}

		public static bool CanDelete {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && textbox.Enabled && !textbox.ReadOnly && (textbox.SelectionLength > 0);
			}
		}

		public static bool CanSelectAll {
			get {
				var textbox = GetTextBox();
				return (textbox != null) && textbox.CanSelect;
			}
		}

		public static void DoCut() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			if (NativeMethods.SendMessage(textbox.Handle, WM_CUT, 0, 0) != 0) throw new Win32Exception();
		}

		public static void DoCopy() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			if (NativeMethods.SendMessage(textbox.Handle, WM_COPY, 0, 0) != 0) throw new Win32Exception();
		}

		public static void DoPaste() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			if (NativeMethods.SendMessage(textbox.Handle, WM_PASTE, 0, 0) != 0) throw new Win32Exception();
		}

		public static void DoDelete() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			if (NativeMethods.SendMessage(textbox.Handle, WM_CLEAR, 0, 0) != 0) throw new Win32Exception();
		}

		public static void DoSelectAll() {
			var textbox = GetTextBox();
			if (textbox == null) {
				throw new InvalidOperationException("Non-TextBbox control is focused.");
			}
			textbox.SelectAll();
		}
	}
}
