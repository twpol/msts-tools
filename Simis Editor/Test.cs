//------------------------------------------------------------------------------
// Simis Editor, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JGR;
using JGR.Grammar;
using JGR.IO.Parser;

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
				var totalTime = (double)0;
				foreach (var file in Files) {
					var success = false;
					var time_start = DateTime.Now;
					var newFile = new SimisFile(file, SimisProvider);
					try {
						newFile.ReadFile();
						success = true;
					} catch (FileException ex) {
						messageLog.MessageAccept("Test", BufferedMessageSource.LEVEL_ERROR, ex.ToString());
					}
					var time_end = DateTime.Now;
					if (success) {
						successCount++;
						totalTime += (time_end - time_start).TotalMilliseconds;
					}
					//messageLog.MessageAccept("Test", BufferedMessageSource.LEVEL_INFORMATION, (time_end - time_start).TotalMilliseconds + "ms for <" + file + ">.");

					count++;
					this.Invoke((MethodInvoker)(() => {
						TestFileStatus.Text = "Testing " + Files.Count + " files... " + successCount + " of " + count + " passed (" + ((double)100 * successCount / count).ToString("F0") + "%)";
						TestProgress.Value = count;
					}));
				}
				messageLog.MessageAccept("Test", BufferedMessageSource.LEVEL_INFORMATION, "Tested " + Files.Count + " files; " + successCount + " passed (" + ((double)100 * successCount / Files.Count).ToString("F0") + "%). Took " + totalTime + "ms, " + (totalTime / count).ToString("F0") + "ms/file.");
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
