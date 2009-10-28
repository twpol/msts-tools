//------------------------------------------------------------------------------
// Simis Editor, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Jgr;
using Jgr.IO.Parser;
using Jgr.IO;
using System.Text;
using Jgr.Gui;
using SimisEditor.Properties;

namespace SimisEditor
{
	public partial class Test : Form
	{
		SimisProvider SimisProvider;
		List<string> Extensions;
		List<string> Files;
		Thread BackgroundThread = null;
		Messages Messages = null;

		public Test(SimisProvider provider) {
			InitializeComponent();
			SimisProvider = provider;
			Extensions = new List<string>(SimisProvider.Formats.Select<SimisFormat, string>(f => "." + f.Extension.ToLower()));
			Files = new List<string>();
			TestFormats.Items.Add("All Train Simulator files (" + String.Join(";", SimisProvider.Formats.Select<SimisFormat, string>(f => "*." + f.Extension).ToArray()) + ")");
			TestFormats.Items.AddRange(SimisProvider.Formats.Select<SimisFormat, string>(f => f.Name + " files (*." + f.Extension.ToLower() + ")").ToArray());
			TestTests.Items.AddRange(new string[] { "Read", "Read and Write" });

			try {
				TestFormats.SelectedIndex = Settings.Default.TestFormat.Length == 0 ? 0 : Extensions.IndexOf(Settings.Default.TestFormat) + 1;
				TestTests.SelectedIndex = Settings.Default.TestTests;
				TestPath.Text = Settings.Default.TestPath;
			} catch (ArgumentOutOfRangeException) {
			}
			ScanForFiles();
		}

		void ScanForFiles() {
			var path = TestPath.Text;
			if (path.StartsWith("<")) return;

			TestResults.Enabled = false;
			TestFileStatus.Text = "Scanning for files...";
			Messages = null;

			Settings.Default.TestPath = TestPath.Text;
			Settings.Default.TestFormat = TestFormats.SelectedIndex == 0 ? "" : Extensions[TestFormats.SelectedIndex - 1];
			Settings.Default.TestTests = TestTests.SelectedIndex;
			Settings.Default.Save();

			Files = new List<string>();
			var allowedExtensions = TestFormats.SelectedIndex == 0 ? Extensions.ToArray() : new string[] { Extensions[TestFormats.SelectedIndex - 1] };

			if (BackgroundThread != null) {
				BackgroundThread.Abort();
			}

			BackgroundThread = new Thread(() => {
				BackgroundScanForFilesRecursive(path, allowedExtensions);
				this.Invoke((MethodInvoker)(() => {
					//TestFileStatus.Text = "Found " + Files.Count + " files to test.";
					TestFiles();
				}));
			});
			BackgroundThread.Start();
		}

		void BackgroundScanForFilesRecursive(string path, IEnumerable<string> allowedExtensions) {
			try {
				foreach (var file in Directory.GetFiles(path)) {
					if (file.IndexOf(".") >= 0) {
						var ext = file.Substring(file.LastIndexOf(".")).ToLower();
						if (allowedExtensions.Contains(ext)) {
							Files.Add(file);
						}
					}
				}
				foreach (var directory in Directory.GetDirectories(path)) {
					BackgroundScanForFilesRecursive(directory, allowedExtensions);
				}
			} catch (DirectoryNotFoundException) {
			}
		}

		void TestFiles() {
			TestProgress.Minimum = 0;
			TestProgress.Maximum = Files.Count;
			TestProgress.Value = 0;
			TestFileStatus.Text = "Testing " + Files.Count + " files...";

			var doWriteTest = TestTests.SelectedIndex == 1;

			BackgroundThread = new Thread(() => {
				var messageLog = new BufferedMessageSource();
				var count = 0;
				var successCount = 0;
				var timeStart = DateTime.Now;

				foreach (var file in Files) {
					var success = true;
					var newFile = new SimisFile(file, SimisProvider);
					Stream readStream = File.OpenRead(file);
					Stream saveStream = new MemoryStream();

					// First, read the file in.
					if (success) {
						try {
							newFile.ReadFile();
						} catch (FileException ex) {
							success = false;
							messageLog.MessageAccept("Read", BufferedMessageSource.LevelError, ex.ToString());
						}
					}

					// Second, write the file out into memory.
					if (success && doWriteTest) {
						try {
							newFile.WriteStream(saveStream);
						} catch (FileException ex) {
							success = false;
							messageLog.MessageAccept("Write", BufferedMessageSource.LevelError, ex.ToString());
						}
					}

					// Third, verify that the output is the same as the input.
					if (success && doWriteTest) {
						readStream.Seek(0, SeekOrigin.Begin);
						saveStream.Seek(0, SeekOrigin.Begin);
						var readReader = new BinaryReader(new SimisTestableStream(readStream), newFile.StreamFormat == SimisStreamFormat.Binary ? new ByteEncoding() : Encoding.Unicode);
						var saveReader = new BinaryReader(new SimisTestableStream(saveStream), newFile.StreamFormat == SimisStreamFormat.Binary ? new ByteEncoding() : Encoding.Unicode);
						while ((readReader.BaseStream.Position < readReader.BaseStream.Length) && (saveReader.BaseStream.Position < saveReader.BaseStream.Length)) {
							var oldPos = readReader.BaseStream.Position;
							var fileChar = readReader.ReadChar();
							var saveChar = saveReader.ReadChar();
							if (fileChar != saveChar) {
								success = false;
								var readEx = new ReaderException(readReader, newFile.StreamFormat == SimisStreamFormat.Binary, (int)(readReader.BaseStream.Position - oldPos), "");
								var saveEx = new ReaderException(saveReader, newFile.StreamFormat == SimisStreamFormat.Binary, (int)(readReader.BaseStream.Position - oldPos), "");
								messageLog.MessageAccept("Compare", BufferedMessageSource.LevelError, String.Format("{0}\n\nFile character {1:N0} does not match: {2:X4} vs {3:X4}.\n\n{4}{5}", file, oldPos, fileChar, saveChar, readEx.ToString(), saveEx.ToString()));
								break;
							}
						}
						if (success && (readReader.BaseStream.Length != saveReader.BaseStream.Length)) {
							success = false;
							var readEx = new ReaderException(readReader, newFile.StreamFormat == SimisStreamFormat.Binary, 0, "");
							var saveEx = new ReaderException(saveReader, newFile.StreamFormat == SimisStreamFormat.Binary, 0, "");
							messageLog.MessageAccept("Compare", BufferedMessageSource.LevelError, String.Format("{0}\n\nFile and stream length do not match: {1:N0} vs {2:N0}.\n\n{3}{4}", file, readReader.BaseStream.Length, saveReader.BaseStream.Length, readEx.ToString(), saveEx.ToString()));
						}
					}

					// It all worked!
					if (success) {
						successCount++;
						messageLog.MessageAccept("Test", BufferedMessageSource.LevelInformation, String.Format("{0}\n\nFile successfully read and written.", file));
					}

					count++;
					this.Invoke((MethodInvoker)(() => {
						var timeTaken = DateTime.Now - timeStart;
						TestFileStatus.Text = String.Format("Testing {0} files... {1} of {2} passed ({3:F0}%). About {4:F0} minutes remaining.", Files.Count, successCount, count, ((double)100 * successCount / count), (Files.Count - count) * timeTaken.TotalMinutes / count);
						TestProgress.Value = count;
					}));
				}

				{
					var timeTaken = DateTime.Now - timeStart;
					messageLog.MessageAccept("Test", BufferedMessageSource.LevelInformation, String.Format("Tested {0} files; {1} passed ({2:F0}%). Took {3:F0} minutes.", Files.Count, successCount, ((double)100 * successCount / Files.Count), timeTaken.TotalMinutes));
				}

				this.Invoke((MethodInvoker)(() => {
					TestResults.Enabled = true;
					TestFileStatus.Text = "Tested " + Files.Count + " files; " + successCount + " passed (" + ((double)100 * successCount / Files.Count).ToString("F0") + "%)";

					Messages = new Messages();
					messageLog.RegisterMessageSink(Messages);
				}));
				BackgroundThread = null;
			});
			BackgroundThread.Start();
		}

		void Test_FormClosing(object sender, FormClosingEventArgs e) {
			if (BackgroundThread != null) {
				BackgroundThread.Abort();
				BackgroundThread = null;
			}
		}

		private void TestPathBrowse_Click(object sender, EventArgs e) {
			using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
				TestPathBrowseDialog.Description = "Select a folder to test files from.";
				if (TestPathBrowseDialog.ShowDialog(this) == DialogResult.OK) {
					TestPath.Text = TestPathBrowseDialog.SelectedPath;
					ScanForFiles();
				}
			}
		}

		private void TestFormats_SelectedIndexChanged(object sender, EventArgs e) {
			ScanForFiles();
		}

		private void TestTests_SelectedIndexChanged(object sender, EventArgs e) {
			ScanForFiles();
		}

		private void TestResults_Click(object sender, EventArgs e) {
			Messages.ShowDialog(this);
		}
	}
}
