//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;

namespace Jgr.Gui {
	public enum FeedbackType {
		ApplicationFailure,
		UserComment,
	}

	/// <summary>
	/// Simple interface for collecting and sending user feedback and application errors to a central store.
	/// </summary>
	public class Feedback {
		readonly string UID;
		readonly string EnvironmentOS;
		readonly Version EnvironmentOSVersion;
		readonly int EnvironmentCores;
		readonly Version EnvironmentCLR;
		readonly int EnvironmentCLRBitness;
		readonly DateTime Time;
		readonly string ApplicationName;
		readonly string ApplicationVersion;
		readonly StackFrame Source;
		readonly FeedbackType Type;
		readonly string Operation;
		readonly IDictionary<string, string> Details;
		string Email;
		string Comments;

		static string[] FeedbackTypeFaces = {
												"☹",
												"☺☹",
											};
		static string[] FeedbackTypeIntros = {
												 "{1} encountered an error while {0}. This wasn't meant to happen and we'd like you to send this problem report to us so we can make sure it never happens again. If you want to include some comments, that's fine with us.",
												 "Dying to tell us what you think of {1}? Write your comments below and send - we don't even need your e-mail address. If you want us to get back to you about your comments, however, you'll need to provide it.",
											 };
		static string[] FeedbackTypeNames = {
												"Application Failure",
												"User Comment",
											};

		Feedback(FeedbackType type, string operation, IDictionary<string, string> details, StackTrace source) {
			var callingStackFrame = source.GetFrames().First(f => !f.GetMethod().DeclaringType.FullName.StartsWith("System.", StringComparison.OrdinalIgnoreCase));

			EnvironmentOS = Environment.OSVersion.ToString();
			EnvironmentOSVersion = Environment.OSVersion.Version;
			EnvironmentCores = Environment.ProcessorCount;
			EnvironmentCLR = Environment.Version;
			EnvironmentCLRBitness = IntPtr.Size * 8;
			Time = DateTime.Now;
			ApplicationName = Application.ProductName;
			ApplicationVersion = Application.ProductVersion;
			Source = callingStackFrame;
			Type = type;
			Operation = operation;
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

		/// <summary>
		/// Prepares a feedback report of a specific <see cref="FeedbackType"/> for the given <paramref name="operation"/>.
		/// </summary>
		/// <param name="type">The type of feedback being collected.</param>
		/// <param name="operation">The operation that failed if <paramref name="type"/> is <see cref="FeedbackType.ApplicationFailure"/>.</param>
		/// <param name="details">Any associated data to send with the report.</param>
		public Feedback(FeedbackType type, string operation, IDictionary<string, string> details)
			: this(type, operation, details, new StackTrace(1, true)) {
		}

		/// <summary>
		/// Prepares a feedback report for an <see cref="FeedbackType.ApplicationFailure"/>.
		/// </summary>
		/// <param name="ex">The <see cref="Exception"/> which caused this feedback.</param>
		/// <param name="operation">The operation that was being performed.</param>
		public Feedback(Exception ex, string operation)
			: this(FeedbackType.ApplicationFailure, operation, new Dictionary<string, string> { { "", ex.ToString() } }, new StackTrace(1, true)) {
		}

		/// <summary>
		/// Prepares a feedback report for a <see cref="FeedbackType.UserComment"/>.
		/// </summary>
		public Feedback()
			: this(FeedbackType.UserComment, "", new Dictionary<string, string> { }, new StackTrace(1, true)) {
		}

		/// <summary>
		/// Shows the report to the user, asking for their comments, and offering to send to the central store.
		/// </summary>
		/// <param name="owner">A <see cref="Form"/> to modally parent the <see cref="FeedbackPrompt"/> on.</param>
		public void PromptAndSend(Form owner) {
			var report =
				"User ID: " + UID + " (random unique identifier, not shared between applications)\n" +
				"Operating System: " + EnvironmentOS + "\n" +
				"Processor Cores: " + EnvironmentCores + "\n" +
				"Runtime Version: " + EnvironmentCLR + " (" + EnvironmentCLRBitness + "bit)" + "\n" +
				"Time: " + Time.ToString("F", CultureInfo.CurrentCulture) + "\n" +
				"Application: " + ApplicationName + " " + ApplicationVersion + "\n" +
				"Source: " + FormatMethodName(Source) + "\n" +
				"Type: " + FeedbackTypeNames[(int)Type] +
				(Details.ContainsKey("") ? "\nDetails:\n\n" + Details[""] : "");
			foreach (var item in Details.Where(i => i.Key.Length > 0)) {
				report += "\n\nAttachment (" + item.Key + "):\n\n" +
					item.Value;
			}

			using (var feedback = new FeedbackPrompt()) {
				feedback.LabelFace.Text = FeedbackTypeFaces[(int)Type];
				feedback.LabelIntro.Text = String.Format(CultureInfo.CurrentCulture, FeedbackTypeIntros[(int)Type], Operation, ApplicationName);
				feedback.TextApplication.Text = ApplicationName + " " + ApplicationVersion;
				feedback.TextType.Text = FeedbackTypeNames[(int)Type];
				feedback.TextEmail.Text = Email;
				feedback.AllData = report;
				var rv = feedback.ShowDialog(owner);
				if (rv != DialogResult.Cancel) {
					Email = feedback.TextEmail.Text;
					Comments = feedback.TextComments.Text;
					using (var key = Registry.CurrentUser.CreateSubKey(@"Software\JGR\" + ApplicationName, RegistryKeyPermissionCheck.ReadWriteSubTree)) {
						key.SetValue("Email", Email);
					}
					if (rv == DialogResult.Yes) {
						Send(owner);
					}
				}
			}
		}

		/// <summary>
		/// Sends the report to the central store and shows an error/success confirmation message.
		/// </summary>
		/// <param name="owner"></param>
		public void Send(Form owner) {
			var reportXML = new XDocument(
					new XDeclaration("1.0", "utf-8", "yes"),
					new XElement(XName.Get("report"),
						new XAttribute(XName.Get("version"), "1.0"),
						new XAttribute(XName.Get("uid"), UID),
						new XAttribute(XName.Get("time"), Time.ToString("O", CultureInfo.InvariantCulture)),
						new XAttribute(XName.Get("type"), Type),
						new XAttribute(XName.Get("email"), Email),
						new XElement(XName.Get("environment"),
							new XElement(XName.Get("os"),
								new XAttribute(XName.Get("version"), EnvironmentOSVersion.ToString()),
								EnvironmentOS),
							new XElement(XName.Get("processor"),
								new XAttribute(XName.Get("cores"), EnvironmentCores)),
							new XElement(XName.Get("clr"),
								new XAttribute(XName.Get("bits"), EnvironmentCLRBitness.ToString(CultureInfo.InvariantCulture)),
								new XAttribute(XName.Get("version"), EnvironmentCLR))),
						new XElement(XName.Get("application"),
							new XAttribute(XName.Get("version"), ApplicationVersion),
							ApplicationName),
						new XElement(XName.Get("source"),
							Source.GetFileName() != null ? new XAttribute(XName.Get("file"), Source.GetFileName()) : null,
							Source.GetFileName() != null ? new XAttribute(XName.Get("line"), Source.GetFileLineNumber()) : null,
							Source.GetFileName() != null ? new XAttribute(XName.Get("column"), Source.GetFileColumnNumber()) : null,
							FormatMethodName(Source)),
						Details.Select(d => d.Key.Length == 0 ? new XElement(XName.Get("details"), d.Value) : new XElement(XName.Get("details"), new XAttribute(XName.Get("name"), d.Key), d.Value)),
						new XElement(XName.Get("comments"), Comments)));

			var uri = new Uri("http://twpol.dyndns.org/projects/jgrmsts/reports/upload?uid=" + UID);
			var wc = new WebClient();
			try {
				wc.Encoding = Encoding.UTF8;
				wc.Headers["User-Agent"] = ApplicationName.Replace(' ', '_') + "/" + ApplicationVersion + " (" + EnvironmentOS + "; .NET CLR " + EnvironmentCLR + "; " + EnvironmentCLRBitness + "bit)";
				wc.Headers["Content-Type"] = "application/xml";
				wc.UploadString(uri, reportXML.Declaration.ToString() + "\r\n" + reportXML.ToString());
			} catch (Exception ex) {
				TaskDialog.Show(owner, TaskDialogCommonIcon.Error, "Unable to send feedback:", ex.ToString());
				return;
			}

			TaskDialog.Show(owner, TaskDialogCommonIcon.Information, "Feedback sent successfully.", "");
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

		string FormatMethodName(StackFrame frame) {
			return frame.GetMethod().DeclaringType.FullName + "." + frame.GetMethod().Name;
		}
	}
}
