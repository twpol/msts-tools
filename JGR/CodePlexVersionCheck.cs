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

namespace JGR
{
	public class CodePlexVersionCheck
	{
		Uri Feed;
		string ReleaseName;
		DateTime CurrentVersionReleaseDate;
		public event EventHandler CheckComplete;
		public bool HasLatestVersion { get; protected set; }
		public string LatestVersionTitle { get; protected set; }
		public Uri LatestVersionUri { get; protected set; }
		public bool IsNewVersion { get; protected set; }

		public CodePlexVersionCheck(string projectName, string releaseName, DateTime currentVersionReleaseDate) {
			Feed = new Uri("http://" + projectName + ".codeplex.com/Project/ProjectRss.aspx?ProjectRSSFeed=" + Uri.EscapeDataString("codeplex://release/" + projectName), UriKind.Absolute);
			ReleaseName = releaseName;
			CurrentVersionReleaseDate = currentVersionReleaseDate;
			HasLatestVersion = false;
			LatestVersionTitle = "";
			LatestVersionUri = null;
			IsNewVersion = false;
		}

		public void Check() {
			var thread = new Thread(() => CheckThread());
			thread.Start();
		}

		private void CheckThread() {
			FetchAndProcess();

			if (null != CheckComplete) {
				CheckComplete(this, new EventArgs());
			}
		}

		private void FetchAndProcess() {
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
