//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Jgr.Gui;
using Microsoft.Win32;

namespace Jgr.Gui {
	public class Feedback {
		readonly string UID;
		public readonly string EnvironmentOS;
		public readonly Version EnvironmentOSVersion;
		public readonly int EnvironmentCores;
		public readonly Version EnvironmentCLR;
		public readonly int EnvironmentCLRBitness;
		public readonly DateTime Time;
		public readonly string ApplicationName;
		public readonly string ApplicationVersion;
		public readonly string LocationMethod;
		public readonly string LocationFileName;
		public readonly int LocationFileLine;
		public readonly int LocationFileColumn;
		public readonly string Type;
		public readonly string Details;
		string Email;
		string Comments;

		Feedback(string type, string details) {
			var callingStackFrame = new StackTrace(2, true).GetFrames().First(f => !f.GetMethod().DeclaringType.FullName.StartsWith("System.", StringComparison.OrdinalIgnoreCase));

			EnvironmentOS = Environment.OSVersion.ToString();
			EnvironmentOSVersion = Environment.OSVersion.Version;
			EnvironmentCores = Environment.ProcessorCount;
			EnvironmentCLR = Environment.Version;
			EnvironmentCLRBitness = IntPtr.Size * 8;
			Time = DateTime.Now;
			ApplicationName = Application.ProductName;
			ApplicationVersion = Application.ProductVersion;
			LocationMethod = callingStackFrame.GetMethod().DeclaringType.FullName + "." + callingStackFrame.GetMethod().Name;
			LocationFileName = callingStackFrame.GetFileName();
			LocationFileLine = callingStackFrame.GetFileLineNumber();
			LocationFileColumn = callingStackFrame.GetFileColumnNumber();
			Type = type;
			Details = details;
			Email = "";
			Comments = "";

			using (var key = Registry.CurrentUser.CreateSubKey(@"Software\JGR\" + ApplicationName, RegistryKeyPermissionCheck.ReadWriteSubTree)) {
				UID = (string)key.GetValue("UID", "");
				if (UID.Length != 16) {
					UID = GenerateUID();
					key.SetValue("UID", UID, RegistryValueKind.String);
				}
				Email = (string)key.GetValue("Email", "");
			}
		}

		public Feedback(Exception e)
			: this("Application Error", e.ToString()) {
		}

		public Feedback()
			: this("User Comment", "") {
		}

		public void PromptAndSend(Form owner) {
			var report =
				"User ID: " + UID + " (random unique identifier, not shared between applications)\n" +
				"Operating System: " + EnvironmentOS + "\n" +
				"Processor Cores: " + EnvironmentCores + "\n" +
				"Runtime Version: " + EnvironmentCLR + " (" + EnvironmentCLRBitness + "bit)" + "\n" +
				"Report Time: " + Time.ToString("F") + "\n" +
				"Report Application: " + ApplicationName + " " + ApplicationVersion + "\n" +
				"Report Source: " + LocationMethod + " (" + LocationFileName + ":" + LocationFileLine + ":" + LocationFileColumn + ")\n" +
				"Report Type: " + Type + 
				(Details.Length > 0 ? 
					"\n" +
					"Report Details:\n" +
					"\n" +
					Details
				: "");

			using (var feedback = new FeedbackPrompt()) {
				feedback.TextApplication.Text = ApplicationName + " " + ApplicationVersion;
				feedback.TextType.Text = Type;
				feedback.TextEmail.Text = Email;
				feedback.AllData = report;
				if (feedback.ShowDialog(owner) == DialogResult.OK) {
					Email = feedback.TextEmail.Text;
					Comments = feedback.TextComments.Text;
					Send(owner);
				}
			}
		}

		public void Send(Form owner) {
			var reportXML = new XDocument(
					new XDeclaration("1.0", "utf-8", "yes"),
					new XElement(XName.Get("report"),
						new XAttribute(XName.Get("version"), "1.0"),
						new XAttribute(XName.Get("uid"), UID),
						new XAttribute(XName.Get("time"), Time.ToString("O")),
						new XAttribute(XName.Get("type"), Type),
						new XAttribute(XName.Get("email"), Email),
						new XElement(XName.Get("environment"),
							new XElement(XName.Get("os"),
								EnvironmentOS,
								new XAttribute(XName.Get("version"), EnvironmentOSVersion.ToString())),
							new XElement(XName.Get("processor"),
								new XAttribute(XName.Get("cores"), EnvironmentCores)),
							new XElement(XName.Get("clr"),
								new XAttribute(XName.Get("bits"), EnvironmentCLRBitness.ToString()),
								new XAttribute(XName.Get("version"), EnvironmentCLR))),
						new XElement(XName.Get("application"),
							ApplicationName,
							new XAttribute(XName.Get("version"), ApplicationVersion)),
						new XElement(XName.Get("source"),
							LocationMethod,
							new XAttribute(XName.Get("file"), LocationFileName),
							new XAttribute(XName.Get("line"), LocationFileLine),
							new XAttribute(XName.Get("column"), LocationFileColumn)),
						new XElement(XName.Get("details"), Details),
						new XElement(XName.Get("comments"), Comments)));

			var uri = new Uri("http://twpol.dyndns.org/projects/jgrmsts/reports/upload?uid=" + UID);
			var wc = new WebClient();
			try {
				wc.Encoding = Encoding.UTF8;
				wc.Headers["Content-Type"] = "application/xml";
				wc.UploadString(uri, reportXML.Declaration.ToString() + "\r\n" + reportXML.ToString());
			} catch (Exception ex) {
				TaskDialog.Show(owner, TaskDialogCommonIcon.Error, "Unable to send feedback:", ex.ToString());
				return;
			}

			TaskDialog.Show(owner, TaskDialogCommonIcon.Information, "Feedback sent successfully.", "");

			using (var key = Registry.CurrentUser.CreateSubKey(@"Software\JGR\" + ApplicationName, RegistryKeyPermissionCheck.ReadWriteSubTree)) {
				key.SetValue("Email", Email);
			}
		}

		const string uidChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		string GenerateUID() {
			var uid = new StringBuilder(16);
			var rand = new Random();
			for (var i = 0; i < uid.Capacity; i++) {
				uid.Append(uidChars[rand.Next(uidChars.Length)]);
			}
			return uid.ToString();
		}
	}
}
