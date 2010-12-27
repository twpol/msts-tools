//------------------------------------------------------------------------------
// Tools, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using Jgr;

namespace Tools {
	public partial class Tools : Form {
		readonly ApplicationSettings Settings;
		public Tools() {
			InitializeComponent();
			Settings = new ApplicationSettings();
			imageListIcons.ImageSize = SystemInformation.IconSize;
			new Thread(LoadTools).Start();
			UpdateCheck();
		}

		void LoadTools() {
			var startMenuBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ApplicationSettings.ApplicationCompany);
			UpdateStartMenu(startMenuBasePath);

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

				var name = String.IsNullOrEmpty(version.FileDescription) ? Path.GetFileNameWithoutExtension(filePath) : version.FileDescription;

				Invoke((Action)(() => {
					var listItem = listViewTools.Items.Add(name, fileName);
					listItem.Tag = filePath;
				}));

				// Don't create shortcuts to console applications.
				using (var reader = new BinaryReader(System.IO.File.OpenRead(filePath))) {
					if (GetPESubsystem(reader) != 3) {
						UpdateStartMenu(startMenuBasePath, filePath, name);
					}
				}
			}
		}

		void UpdateStartMenu(string startMenuBasePath) {
			if (!Directory.Exists(startMenuBasePath)) {
				Directory.CreateDirectory(startMenuBasePath);
			}
			UpdateStartMenu(startMenuBasePath, Application.ExecutablePath, ApplicationSettings.ApplicationProduct, ApplicationSettings.ApplicationTitle);
		}

		void UpdateStartMenu(string startMenuBasePath, string filePath, string name) {
			UpdateStartMenu(startMenuBasePath, filePath, name, name);
		}

		void UpdateStartMenu(string startMenuBasePath, string filePath, string name, string settingsGroups) {
			Console.WriteLine("UpdateStartMenu: " + filePath + " --> " + name);
			if (!Settings[settingsGroups].Boolean["CreatedStartMenuLink"]) {
				Settings[settingsGroups].Boolean["CreatedStartMenuLink"] = true;

				var shell = new WshShell();
				var shortcut = (IWshShortcut)shell.CreateShortcut(Path.Combine(startMenuBasePath, string.Format("{0} {1}.lnk", ApplicationSettings.ApplicationCompany, name)));
				shortcut.TargetPath = filePath;
				shortcut.WorkingDirectory = Path.GetDirectoryName(filePath);
				shortcut.Save();
			}
		}

		void UpdateCheck() {
			buttonUpdate.Text = "Checking for updates...";
			buttonUpdate.Enabled = false;
			var versionCheck = new CodePlexVersionCheck(Settings, "jgrmsts", "MSTS Editors and Tools", DateTime.Parse("26/09/2010"));
			versionCheck.CheckComplete += (o1, e1) => this.Invoke((MethodInvoker)(() => {
				if (versionCheck.HasLatestVersion) {
					if (versionCheck.IsNewVersion) {
						buttonUpdate.Enabled = true;
						buttonUpdate.Text = "Download update from website";
						toolTip.SetToolTip(buttonUpdate, versionCheck.LatestVersionTitle);
						buttonUpdate.Click += (o2, e2) => Process.Start(versionCheck.LatestVersionUri.AbsoluteUri);
					} else {
						buttonUpdate.Text = "No updates available";
					}
				} else {
					buttonUpdate.Text = "Error checking for updates";
				}
			}));
			versionCheck.Check();
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
				using (var reader = new BinaryReader(System.IO.File.OpenRead(ToolFilePath))) {
					ToolIsConsole = GetPESubsystem(reader) == 3;
				}

				var toolName = listViewTools.SelectedItems[0].Text;
				var descFile = Path.GetFileNameWithoutExtension(ToolFilePath) + ".txt";

				labelTool.Text = toolName;
				if (System.IO.File.Exists(descFile)) {
					textBoxDescription.Text = System.IO.File.ReadAllText(descFile);
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
