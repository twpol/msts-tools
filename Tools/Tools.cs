//------------------------------------------------------------------------------
// Simis File, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Tools {
	public partial class Tools : Form {
		public Tools() {
			InitializeComponent();
			imageListIcons.ImageSize = SystemInformation.IconSize;
			new Thread(LoadTools).Start();
		}

		void LoadTools() {
			foreach (var filePath in Directory.GetFiles(Path.GetDirectoryName(Application.ExecutablePath), "*.exe")) {
				if (filePath.Equals(Application.ExecutablePath, StringComparison.InvariantCultureIgnoreCase)) {
					continue;
				}

				var fileName = Path.GetFileNameWithoutExtension(filePath);

				var icon = GetAssociatedIcon(filePath);
				imageListIcons.Images.Add(fileName, icon);

				var version = FileVersionInfo.GetVersionInfo(filePath);
				if (version.FileDescription == "vshost.exe") {
					continue;
				}

				var name = String.IsNullOrEmpty(version.ProductName) ? Path.GetFileNameWithoutExtension(filePath) : version.ProductName;

				Invoke((Action)(() => {
					var listItem = listViewTools.Items.Add(name, fileName);
					listItem.Tag = filePath;
				}));
			}
		}

		[DllImport("shell32.dll")]
		static extern IntPtr ExtractAssociatedIcon(IntPtr instance, string iconPath, out ushort iconIndex);

		Icon GetAssociatedIcon(string filePath) {
			ushort index;
			return Icon.FromHandle(ExtractAssociatedIcon(IntPtr.Zero, filePath, out index));
		}

		int GetPESubsystem(BinaryReader stream) {
			try {
				var baseOffset = stream.BaseStream.Position;

				// WORD IMAGE_DOS_HEADER.e_magic = 0x4D5A
				stream.BaseStream.Seek(baseOffset + 0, SeekOrigin.Begin);
				var dosMagic = stream.ReadUInt16();
				if (dosMagic != 0x5A4D) {
					return 0;
				}

				//// WORD IMAGE_DOS_HEADER.e_sp = 0x0
				//stream.BaseStream.Seek(baseOffset + 16, SeekOrigin.Begin);
				//var coffMagic = stream.ReadUInt16();
				//if (coffMagic != 0) {
				//	return 0;
				//}

				// LONG IMAGE_DOS_HEADER.e_lfanew
				stream.BaseStream.Seek(baseOffset + 60, SeekOrigin.Begin);
				var ntHeaderOffset = stream.ReadUInt32();
				if (ntHeaderOffset == 0) {
					return 0;
				}

				// DWORD IMAGE_NT_HEADERS.Signature = 0x00004550
				stream.BaseStream.Seek(baseOffset + ntHeaderOffset, SeekOrigin.Begin);
				var ntMagic = stream.ReadUInt32();
				if (ntMagic != 0x00004550) {
					return 0;
				}

				// WORD IMAGE_OPTIONAL_HEADER.Magic = 0x010B
				stream.BaseStream.Seek(baseOffset + ntHeaderOffset + 20 - 4 + IntPtr.Size, SeekOrigin.Begin);
				var peMagic = stream.ReadUInt16();
				if (peMagic != 0x010B) {
					return 0;
				}

				// WORD IMAGE_OPTIONAL_HEADER.Subsystem
				stream.BaseStream.Seek(baseOffset + ntHeaderOffset + 88 - 4 + IntPtr.Size, SeekOrigin.Begin);
				var peSubsystem = stream.ReadUInt16();

				return peSubsystem;
			} catch (EndOfStreamException) {
				return 0;
			}
		}

		void buttonExit_Click(object sender, EventArgs e) {
			Close();
		}

		string ToolFilePath;
		bool ToolIsConsole;

		void listViewTools_SelectedIndexChanged(object sender, EventArgs e) {
			if (listViewTools.SelectedItems.Count > 0) {
				ToolFilePath = (string)listViewTools.SelectedItems[0].Tag;
				using (var reader = new BinaryReader(File.OpenRead(ToolFilePath))) {
					ToolIsConsole = GetPESubsystem(reader) == 3;
				}

				var toolName = listViewTools.SelectedItems[0].Text;
				var descFile = Path.GetFileNameWithoutExtension(ToolFilePath) + ".txt";

				labelTool.Text = toolName;
				if (File.Exists(descFile)) {
					textBoxDescription.Text = File.ReadAllText(descFile);
				} else {
					textBoxDescription.Text = "";
				}
				buttonLaunch.Text = String.Format("Launch {0}", toolName);
				buttonLaunch.Enabled = true;
			} else {
				labelTool.Text = "";
				textBoxDescription.Text = "";
				buttonLaunch.Text = String.Format("Launch {0}", "Tool");
				buttonLaunch.Enabled = false;
			}
		}

		void buttonLaunch_Click(object sender, EventArgs e) {
			if (ToolIsConsole) {
				Process.Start("cmd", "/k \"" + ToolFilePath + "\"");
			} else {
				Process.Start(ToolFilePath);
			}
		}

		void listViewTools_DoubleClick(object sender, EventArgs e) {
			if (buttonLaunch.Enabled) {
				buttonLaunch_Click(sender, e);
			}
		}
	}
}
