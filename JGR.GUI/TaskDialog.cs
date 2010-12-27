//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Jgr.Gui {
	/// <summary>
	/// Provides Task Dialog support for Windows Vista and later, with automatic fallbacks for earlier operating systems.
	/// </summary>
	public static class TaskDialog {
		static Version WindowsVista = new Version(6, 0);

		static bool IsTaskDialogSupported() {
			return Environment.OSVersion.Version >= WindowsVista;
		}

		static string GetMessageBoxTitle() {
			return ApplicationSettings.ApplicationTitle;
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

		[SecurityPermission(SecurityAction.Demand)]
		static int Show(Form owner, string title, TaskDialogCommonIcon icon, string mainInstruction, string content, TaskDialogCommonButtons commonButtons, TaskDialogButton[] buttons) {
			if (buttons.Length > 10) throw new ArgumentOutOfRangeException("buttons", "Maximum number of buttons is 10.");

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
				if (rv != 0) throw new InvalidOperationException("TaskDialogIndirect failed: " + rv.ToString("X8", CultureInfo.CurrentCulture));
				return button;
			} finally {
				if (buttons.Length > 0) {
					Marshal.FreeHGlobal(tdConfig.Buttons);
				}
			}
		}

		/// <summary>
		/// Shows a message with an <paramref name="icon"/> and an OK button.
		/// </summary>
		/// <param name="owner">The <see cref="Form"/> to parent the message on.</param>
		/// <param name="icon">The <see cref="TaskDialogCommonIcon"/> to show with the message.</param>
		/// <param name="mainInstruction">The main heading for the message.</param>
		/// <param name="content">The details for the message, shown below the <paramref name="mainInstruction"/>.</param>
		public static void Show(Form owner, TaskDialogCommonIcon icon, string mainInstruction, string content) {
			if (IsTaskDialogSupported()) {
				Show(owner, GetMessageBoxTitle(), icon, mainInstruction, content, TaskDialogCommonButtons.None, new TaskDialogButton[0]);
			} else {
				using (new AutoCenterWindows(owner, AutoCenterWindowsMode.FirstWindowOnly)) {
					MessageBox.Show(owner, String.Join("\n\n", new string[] { mainInstruction, content }), GetMessageBoxTitle(), 0, GetMessageBoxIcon(icon), 0, owner.RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RtlReading : 0);
				}
			}
		}

		/// <summary>
		/// Shows a message with an <paramref name="icon"/> and yes and no buttons.
		/// </summary>
		/// <param name="owner">The <see cref="Form"/> to parent the message on.</param>
		/// <param name="icon">The <see cref="TaskDialogCommonIcon"/> to show with the message.</param>
		/// <param name="mainInstruction">The main heading for the message.</param>
		/// <param name="content">The details for the message, shown below the <paramref name="mainInstruction"/>.</param>
		/// <param name="yes">The <see cref="string"/> to use for the yes button.</param>
		/// <param name="no">The <see cref="string"/> to use for the no button.</param>
		/// <returns>The <see cref="DialogResult"/> indicating which button was selected.</returns>
		public static DialogResult ShowYesNo(Form owner, TaskDialogCommonIcon icon, string mainInstruction, string content, string yes, string no) {
			var button = DialogResult.None;
			if (IsTaskDialogSupported()) {
				button = (DialogResult)Show(owner, GetMessageBoxTitle(), icon, mainInstruction, content, TaskDialogCommonButtons.None, new TaskDialogButton[] { new TaskDialogButton() { ButtonID = (int)DialogResult.Yes, ButtonText = yes }, new TaskDialogButton() { ButtonID = (int)DialogResult.No, ButtonText = no } });
			} else {
				using (new AutoCenterWindows(owner, AutoCenterWindowsMode.FirstWindowOnly)) {
					button = MessageBox.Show(owner, String.Join("\n\n", new string[] { mainInstruction, content }), GetMessageBoxTitle(), MessageBoxButtons.YesNo, GetMessageBoxIcon(icon), 0, owner.RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RtlReading : 0);
				}
			}
			return (DialogResult)button;
		}

		/// <summary>
		/// Shows a message with an <paramref name="icon"/> and yes, no and cancel buttons.
		/// </summary>
		/// <param name="owner">The <see cref="Form"/> to parent the message on.</param>
		/// <param name="icon">The <see cref="TaskDialogCommonIcon"/> to show with the message.</param>
		/// <param name="mainInstruction">The main heading for the message.</param>
		/// <param name="content">The details for the message, shown below the <paramref name="mainInstruction"/>.</param>
		/// <param name="yes">The <see cref="string"/> to use for the yes button.</param>
		/// <param name="no">The <see cref="string"/> to use for the no button.</param>
		/// <param name="cancel">The <see cref="string"/> to use for the cancel button.</param>
		/// <returns>The <see cref="DialogResult"/> indicating which button was selected.</returns>
		public static DialogResult ShowYesNoCancel(Form owner, TaskDialogCommonIcon icon, string mainInstruction, string content, string yes, string no, string cancel) {
			var button = DialogResult.None;
			if (IsTaskDialogSupported()) {
				button = (DialogResult)Show(owner, GetMessageBoxTitle(), icon, mainInstruction, content, TaskDialogCommonButtons.None, new TaskDialogButton[] { new TaskDialogButton() { ButtonID = (int)DialogResult.Yes, ButtonText = yes }, new TaskDialogButton() { ButtonID = (int)DialogResult.No, ButtonText = no }, new TaskDialogButton() { ButtonID = (int)DialogResult.Cancel, ButtonText = cancel } });
			} else {
				using (new AutoCenterWindows(owner, AutoCenterWindowsMode.FirstWindowOnly)) {
					button = MessageBox.Show(owner, String.Join("\n\n", new string[] { mainInstruction, content }), GetMessageBoxTitle(), MessageBoxButtons.YesNoCancel, GetMessageBoxIcon(icon), 0, owner.RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RtlReading : 0);
				}
			}
			return (DialogResult)button;
		}
	}
}
