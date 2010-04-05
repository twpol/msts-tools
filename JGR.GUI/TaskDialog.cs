//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jgr.Gui {
	public class TaskDialog {
		static Version WindowsVista = new Version(6, 0);

		static bool IsTaskDialogSupported() {
			//return false;
			return Environment.OSVersion.Version >= WindowsVista;
		}

		static string GetMessageBoxTitle() {
			return Application.ProductName;
		}

		static MessageBoxIcon GetMessageBoxIcon(TaskDialogCommonIcon icon) {
			switch (icon) {
				case TaskDialogCommonIcon.Warning:
					return MessageBoxIcon.Warning;
				case TaskDialogCommonIcon.Error:
					return MessageBoxIcon.Error;
				case TaskDialogCommonIcon.Information:
					return MessageBoxIcon.Information;
				default:
					return MessageBoxIcon.None;
			}
		}

		static int Show(Form owner, string title, TaskDialogCommonIcon icon, string mainInstruction, string content, TaskDialogCommonButtons commonButtons, TaskDialogButton[] buttons) {
			var tdConfig = new TaskDialogConfig();
			tdConfig.Size = (uint)Marshal.SizeOf(tdConfig);
			Debug.Assert(tdConfig.Size == 160);

			tdConfig.Parent = owner.Handle;
			tdConfig.Instance = NativeMethods.GetModuleHandle(null);
			tdConfig.Flags = TaskDialogFlags.PositionRelativeToWindow | (owner.RightToLeft == RightToLeft.Yes ? TaskDialogFlags.RTLLayout : TaskDialogFlags.None);
			tdConfig.CommonButtons = commonButtons;
			tdConfig.WindowTitle = title;
			tdConfig.MainIcon = new IntPtr((int)icon);
			tdConfig.MainInstruction = mainInstruction;
			tdConfig.Content = content;

			if (buttons.Length > 0) {
				var buttonStructSize = Marshal.SizeOf(typeof(TaskDialogButton));
				tdConfig.Buttons = Marshal.AllocHGlobal(buttonStructSize * buttons.Length);
				for (var i = 0; i < buttons.Length; i++) {
					Marshal.StructureToPtr(buttons[i], new IntPtr(tdConfig.Buttons.ToInt64() + i * buttonStructSize), false);
				}
				tdConfig.ButtonCount = (uint)buttons.Length;
			}

			try {
				var button = 0;
				var radioButton = 0;
				var verificationFlag = false;
				var rv = NativeMethods.TaskDialogIndirect(ref tdConfig, out button, out radioButton, out verificationFlag);
				if (rv != 0) throw new InvalidOperationException("TaskDialogIndirect failed: " + rv.ToString("X8"));
				return button;
			} finally {
				if (buttons.Length > 0) {
					Marshal.FreeHGlobal(tdConfig.Buttons);
				}
			}
		}

		public static void Show(Form owner, TaskDialogCommonIcon icon, string mainInstruction, string content) {
			if (IsTaskDialogSupported()) {
				Show(owner, GetMessageBoxTitle(), icon, mainInstruction, content, TaskDialogCommonButtons.None, new TaskDialogButton[0]);
			} else {
				using (new AutoCenterWindows(owner, AutoCenterWindowsMode.FirstWindowOnly)) {
					MessageBox.Show(owner, String.Join("\n\n", new string[] { mainInstruction, content }), GetMessageBoxTitle(), 0, GetMessageBoxIcon(icon));
				}
			}
		}

		public static DialogResult ShowYesNo(Form owner, TaskDialogCommonIcon icon, string mainInstruction, string content, string yes, string no) {
			var button = DialogResult.None;
			if (IsTaskDialogSupported()) {
				button = (DialogResult)Show(owner, GetMessageBoxTitle(), icon, mainInstruction, content, TaskDialogCommonButtons.None, new TaskDialogButton[] { new TaskDialogButton() { ButtonID = (int)DialogResult.Yes, ButtonText = yes }, new TaskDialogButton() { ButtonID = (int)DialogResult.No, ButtonText = no } });
			} else {
				using (new AutoCenterWindows(owner, AutoCenterWindowsMode.FirstWindowOnly)) {
					button = MessageBox.Show(owner, String.Join("\n\n", new string[] { mainInstruction, content }), GetMessageBoxTitle(), MessageBoxButtons.YesNo, GetMessageBoxIcon(icon));
				}
			}
			return (DialogResult)button;
		}

		public static DialogResult ShowYesNoCancel(Form owner, TaskDialogCommonIcon icon, string mainInstruction, string content, string yes, string no, string cancel) {
			var button = DialogResult.None;
			if (IsTaskDialogSupported()) {
				button = (DialogResult)Show(owner, GetMessageBoxTitle(), icon, mainInstruction, content, TaskDialogCommonButtons.None, new TaskDialogButton[] { new TaskDialogButton() { ButtonID = (int)DialogResult.Yes, ButtonText = yes }, new TaskDialogButton() { ButtonID = (int)DialogResult.No, ButtonText = no }, new TaskDialogButton() { ButtonID = (int)DialogResult.Cancel, ButtonText = cancel } });
			} else {
				using (new AutoCenterWindows(owner, AutoCenterWindowsMode.FirstWindowOnly)) {
					button = MessageBox.Show(owner, String.Join("\n\n", new string[] { mainInstruction, content }), GetMessageBoxTitle(), MessageBoxButtons.YesNoCancel, GetMessageBoxIcon(icon));
				}
			}
			return (DialogResult)button;
		}
	}
}
