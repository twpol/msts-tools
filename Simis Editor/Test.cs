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
using JGR;
using JGR.IO.Parser;
using JGR.IO;
using System.Text;

namespace SimisEditor
{
	public partial class Test : Form
	{
		public Test() {
			InitializeComponent();
		}

		protected SimisProvider SimisProvider;

		protected List<string> Extensions;
		protected List<string> Files;

		protected Thread BackgroundThread = null;

		public void SetupTest(string path, SimisProvider provider) {
			TestPath.Text = path;

			SimisProvider = provider;

			Extensions = new List<string>(SimisProvider.Formats.Select<SimisFormat, string>(f => "." + f.Extension.ToLower()));
			Files = new List<string>();

			FindFilesToTest();
		}

		private void FindFilesToTest() {
			var path = TestPath.Text;
			TestFileStatus.Text = "Scanning for files...";
			BackgroundThread = new Thread(() => {
				BackgroundFindFilesToTestRecursive(path);
				this.Invoke((MethodInvoker)(() => {
					TestFileStatus.Text = "Found " + Files.Count + " files to test.";
					TestRun.Enabled = true;
				}));
				BackgroundThread = null;
			});
			BackgroundThread.Start();
		}

		private void BackgroundFindFilesToTestRecursive(string path) {
			foreach (var file in Directory.GetFiles(path)) {
				if (file.IndexOf(".") >= 0) {
					var ext = file.Substring(file.LastIndexOf(".")).ToLower();
					if (Extensions.Contains(ext)) {
						Files.Add(file);
					}
				}
			}
			foreach (var directory in Directory.GetDirectories(path)) {
				BackgroundFindFilesToTestRecursive(directory);
			}
		}

		private void TestFiles() {
			TestRun.Enabled = false;
			TestProgress.Minimum = 0;
			TestProgress.Maximum = Files.Count;
			TestProgress.Value = 0;
			TestFileStatus.Text = "Testing " + Files.Count + " files...";

			BackgroundThread = new Thread(() => {
				var messageLog = new BufferedMessageSource();
				var count = 0;
				var successCount = 0;

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
							messageLog.MessageAccept("Read", BufferedMessageSource.LEVEL_ERROR, ex.ToString());
						}
					}

					// Second, write the file out into memory.
					if (success) {
						try {
							newFile.WriteStream(saveStream);
						} catch (FileException ex) {
							success = false;
							messageLog.MessageAccept("Write", BufferedMessageSource.LEVEL_ERROR, ex.ToString());
						}
					}

					// Third, verify that the output is the same as the input.
					if (success) {
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
								messageLog.MessageAccept("Compare", BufferedMessageSource.LEVEL_ERROR, String.Format("{0}\n\nFile character {1:N0} does not match: {2:X4} vs {3:X4}.\n\n{4}{5}", file, oldPos, fileChar, saveChar, readEx.ToString(), saveEx.ToString()));
								break;
							}
						}
						if (success && (readReader.BaseStream.Length != saveReader.BaseStream.Length)) {
							success = false;
							var readEx = new ReaderException(readReader, newFile.StreamFormat == SimisStreamFormat.Binary, 0, "");
							var saveEx = new ReaderException(saveReader, newFile.StreamFormat == SimisStreamFormat.Binary, 0, "");
							messageLog.MessageAccept("Compare", BufferedMessageSource.LEVEL_ERROR, String.Format("{0}\n\nFile and stream length do not match: {1:N0} vs {2:N0}.\n\n{3}{4}", file, readReader.BaseStream.Length, saveReader.BaseStream.Length, readEx.ToString(), saveEx.ToString()));
						}
					}
					if (success) {
						successCount++;
						messageLog.MessageAccept("Test", BufferedMessageSource.LEVEL_INFORMATION, String.Format("{0}\n\nFile successfully read and written.", file));
					}

					count++;
					this.Invoke((MethodInvoker)(() => {
						TestFileStatus.Text = "Testing " + Files.Count + " files... " + successCount + " of " + count + " passed (" + ((double)100 * successCount / count).ToString("F0") + "%)";
						TestProgress.Value = count;
					}));
				}

				messageLog.MessageAccept("Test", BufferedMessageSource.LEVEL_INFORMATION, "Tested " + Files.Count + " files; " + successCount + " passed (" + ((double)100 * successCount / Files.Count).ToString("F0") + "%).");
				
				this.Invoke((MethodInvoker)(() => {
					TestRun.Enabled = true;
					TestFileStatus.Text = "Tested " + Files.Count + " files; " + successCount + " passed (" + ((double)100 * successCount / Files.Count).ToString("F0") + "%)";
					using (var messages = new Messages()) {
						messageLog.RegisterMessageSink(messages);
						messages.ShowDialog(this);
						messageLog.UnregisterMessageSink(messages);
					}
				}));
				BackgroundThread = null;
			});
			BackgroundThread.Start();
		}

		private void TestRun_Click(object sender, EventArgs e) {
			TestFiles();
		}

		private void Test_FormClosing(object sender, FormClosingEventArgs e) {
			if (BackgroundThread != null) {
				BackgroundThread.Abort();
				BackgroundThread = null;
			}
		}
	}
}
