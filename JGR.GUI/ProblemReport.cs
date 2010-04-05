//------------------------------------------------------------------------------
// Jgr.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Jgr.Gui;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Jgr.IO {
	public class ProblemReport {
		public readonly string EnvironmentOS;
		public readonly int EnvironmentCores;
		public readonly string EnvironmentCLR;
		public readonly string ApplicationName;
		public readonly string ApplicationVersion;
		public readonly DateTime Time;
		public readonly string LocationMethod;
		public readonly string LocationFileName;
		public readonly int LocationFileLine;
		public readonly int LocationFileColumn;
		public readonly string Type;
		public readonly string Details;

		ProblemReport(string type, string details) {
			var callingStackFrame = new StackTrace(2, true).GetFrames().First(f => !f.GetMethod().DeclaringType.FullName.StartsWith("System.", StringComparison.OrdinalIgnoreCase));

			EnvironmentOS = Environment.OSVersion.ToString();
			EnvironmentCores = Environment.ProcessorCount;
			EnvironmentCLR = Environment.Version.ToString() + " (" + IntPtr.Size * 8 + "bit)";
			ApplicationName = Application.ProductName;
			ApplicationVersion = Application.ProductVersion;
			Time = DateTime.Now;
			LocationMethod = callingStackFrame.GetMethod().DeclaringType.FullName + "." + callingStackFrame.GetMethod().Name;
			LocationFileName = callingStackFrame.GetFileName();
			LocationFileLine = callingStackFrame.GetFileLineNumber();
			LocationFileColumn = callingStackFrame.GetFileColumnNumber();
			Type = type;
			Details = details;
		}

		public ProblemReport(Exception e)
			: this("Exception", e.ToString()) {
		}

		public void PromptAndSend(Form owner) {
			var report =
				"Operating System: " + EnvironmentOS + "\n" +
				"Processor Cores: " + EnvironmentCores + "\n" +
				".NET CLR Version: " + EnvironmentCLR + "\n" +
				"Report Time: " + Time.ToString("F") + "\n" +
				"Report Application: " + ApplicationName + " " + ApplicationVersion + "\n" +
				"Report Location: " + LocationMethod + " (" + LocationFileName + ":" + LocationFileLine + ":" + LocationFileColumn + ")\n" +
				"Report Type: " + Type + "\n" +
				"Report Details:\n" +
				"\n" +
				Details;

			if (TaskDialog.ShowYesNo(owner, TaskDialogCommonIcon.None, "Send the following report?", "IMPORTANT: XXX PRIVACY NOTICE HERE XXX.\n\n" + report, "Send Report", "Don't Send Report") == DialogResult.Yes) {
				Send(owner);
			}
		}

		public void Send(Form owner) {
			var reportXML = new XDocument(
					new XDeclaration("1.0", "utf-8", "yes"),
					new XElement(XName.Get("report"),
						new XElement(XName.Get("environment"),
							new XElement(XName.Get("os"), EnvironmentOS),
							new XElement(XName.Get("cores"), EnvironmentCores),
							new XElement(XName.Get("clr"), EnvironmentCLR)
						),
						new XElement(XName.Get("application"),
							new XElement(XName.Get("name"), ApplicationName),
							new XElement(XName.Get("version"), ApplicationVersion)
						),
						new XElement(XName.Get("caller"),
							new XElement(XName.Get("method"), LocationMethod),
							new XElement(XName.Get("filename"), LocationFileName),
							new XElement(XName.Get("fileline"), LocationFileLine),
							new XElement(XName.Get("filecolumn"), LocationFileColumn)
						),
						new XElement(XName.Get("time"), Time.ToString("O")),
						new XElement(XName.Get("type"), Type),
						new XElement(XName.Get("details"), Details)
					)
				);

			TaskDialog.Show(owner, TaskDialogCommonIcon.None, "XML REPORT", reportXML.ToString());
		}
	}
}
