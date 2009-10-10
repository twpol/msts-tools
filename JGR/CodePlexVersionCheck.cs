//------------------------------------------------------------------------------
// JGR library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Jgr
{
	/// <summary>
	/// Checks for CodePlex project releases newer than a given <see cref="DateTime"/>.
	/// </summary>
	public class CodePlexVersionCheck
	{
		public event EventHandler CheckComplete;
		public bool HasLatestVersion { get; private set; }
		public string LatestVersionTitle { get; private set; }
		public Uri LatestVersionUri { get; private set; }
		public bool IsNewVersion { get; private set; }
		Uri Feed;
		string ReleaseName;
		DateTime CurrentVersionReleaseDate;

		/// <summary>
		/// Initializes the CodePlex check.
		/// </summary>
		/// <param name="projectName">The name of the CodePlex project, as used in the hostname of the CodePlex website.</param>
		/// <param name="releaseName">The prefix of the releases to include, e.g. "Foo Editor" will match any release who's name starts "Foo Editor". An empty string will match all releases in the project.</param>
		/// <param name="currentVersionReleaseDate">The data and time with which to compare.</param>
		public CodePlexVersionCheck(string projectName, string releaseName, DateTime currentVersionReleaseDate) {
			Feed = new Uri("http://" + projectName + ".codeplex.com/Project/ProjectRss.aspx?ProjectRSSFeed=" + Uri.EscapeDataString("codeplex://release/" + projectName), UriKind.Absolute);
			ReleaseName = releaseName;
			CurrentVersionReleaseDate = currentVersionReleaseDate;
			HasLatestVersion = false;
			LatestVersionTitle = "";
			LatestVersionUri = null;
			IsNewVersion = false;
		}

		/// <summary>
		/// Starts the check going in the background. <see cref="CheckComplete"/> will be called when the check is done.
		/// </summary>
		public void Check() {
			var thread = new Thread(() => CheckThread());
			thread.Start();
		}

		void CheckThread() {
			FetchAndProcess();

			if (null != CheckComplete) {
				CheckComplete(this, new EventArgs());
			}
		}

		void FetchAndProcess() {
			var request = WebRequest.Create(Feed);
			HttpWebResponse response;
			try {
				response = (HttpWebResponse)request.GetResponse();
			} catch (WebException) {
				return;
			}
			if (response.StatusCode != HttpStatusCode.OK) {
				return;
			}

			var xml = XDocument.Load(XmlReader.Create(response.GetResponseStream()));

			var item = xml.Element("rss").Element("channel").Elements("item").Where<XElement>(x => x.Element("title").Value.StartsWith("Released: " + ReleaseName) || x.Element("title").Value.StartsWith("Updated Release: " + ReleaseName)).OrderBy<XElement, long>(x => DateTime.Parse(x.Element("pubDate").Value).Ticks).LastOrDefault();
			if (null == item) return;

			HasLatestVersion = true;
			LatestVersionTitle = item.Element("title").Value.Substring(item.Element("title").Value.IndexOf(": ") + 2);
			LatestVersionUri = new Uri(item.Element("link").Value);
			IsNewVersion = DateTime.Parse(item.Element("pubDate").Value) > CurrentVersionReleaseDate;
		}
	}
}
