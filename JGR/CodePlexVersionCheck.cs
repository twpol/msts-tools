//------------------------------------------------------------------------------
// Jgr library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Jgr
{
	/// <summary>
	/// Checks for CodePlex project releases newer than a given <see cref="DateTime"/>.
	/// </summary>
	public class CodePlexVersionCheck
	{
		public event EventHandler CheckComplete;
		public bool HasLatestVersion { get; private set; }
		public DateTime LatestVersionDate { get; private set; }
		public string LatestVersionTitle { get; private set; }
		public Uri LatestVersionUri { get; private set; }
		public bool IsNewVersion { get; private set; }
		readonly ApplicationSettings Settings;
		readonly Uri Feed;
		readonly string ReleaseName;
		readonly DateTime CurrentVersionReleaseDate;

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
		}

		/// <summary>
		/// Initializes the CodePlex check.
		/// </summary>
		/// <param name="settings">The <see cref="ApplicationSettings"/> to read, write and cache update checks via.</param>
		/// <param name="projectName">The name of the CodePlex project, as used in the hostname of the CodePlex website.</param>
		/// <param name="releaseName">The prefix of the releases to include, e.g. "Foo Editor" will match any release who's name starts "Foo Editor". An empty string will match all releases in the project.</param>
		/// <param name="currentVersionReleaseDate">The data and time with which to compare.</param>
		public CodePlexVersionCheck(ApplicationSettings settings, string projectName, string releaseName, DateTime currentVersionReleaseDate)
			: this(projectName, releaseName, currentVersionReleaseDate) {
			Settings = settings;
		}

		/// <summary>
		/// Starts the check going in the background. <see cref="CheckComplete"/> will be called when the check is done.
		/// </summary>
		public void Check() {
			new Thread(CheckThread).Start();
		}

		const long FileTimeAdjustment = 100000000;

		void CheckThread() {
			var lastCheck = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			if ((Settings != null) && (Settings.Integer["UpdateCheckTime"] != 0)) {
				lastCheck = DateTime.FromFileTimeUtc((long)Settings.Integer["UpdateCheckTime"] * FileTimeAdjustment);
			}

			if ((Settings == null) || ((DateTime.Now - lastCheck).TotalDays > 1)) {
				FetchAndProcess();
				if ((Settings != null) && HasLatestVersion) {
					Settings.Integer["UpdateCheckTime"] = (int)(DateTime.Now.ToFileTimeUtc() / FileTimeAdjustment);
					Settings.Boolean["UpdateIsNew"] = IsNewVersion;
					Settings.Integer["UpdateDate"] = (int)(LatestVersionDate.ToFileTimeUtc() / FileTimeAdjustment);
					Settings.String["UpdateTitle"] = LatestVersionTitle;
					Settings.String["UpdateUri"] = LatestVersionUri.ToString();
				}
			} else {
				HasLatestVersion = true;
				IsNewVersion = Settings.Boolean["UpdateIsNew"];
				LatestVersionDate = DateTime.FromFileTimeUtc((long)Settings.Integer["UpdateDate"] * FileTimeAdjustment);
				LatestVersionTitle = Settings.String["UpdateTitle"];
				LatestVersionUri = new Uri(Settings.String["UpdateUri"]);
			}

			var checkComplete = CheckComplete;
			if (checkComplete != null) {
				checkComplete(this, new EventArgs());
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

			var item = xml.Element("rss").Element("channel").Elements("item").Where(x => x.Element("title").Value.StartsWith("Released: " + ReleaseName, StringComparison.OrdinalIgnoreCase) || x.Element("title").Value.StartsWith("Updated Release: " + ReleaseName, StringComparison.OrdinalIgnoreCase)).OrderBy(x => DateTime.Parse(x.Element("pubDate").Value, CultureInfo.InvariantCulture).Ticks).LastOrDefault();
			if (null == item) return;

			HasLatestVersion = true;
			LatestVersionDate = DateTime.Parse(item.Element("pubDate").Value, CultureInfo.InvariantCulture);
			LatestVersionTitle = item.Element("title").Value.Substring(item.Element("title").Value.IndexOf(": ", StringComparison.Ordinal) + 2);
			LatestVersionUri = new Uri(item.Element("link").Value);
			IsNewVersion = LatestVersionDate > CurrentVersionReleaseDate;
		}
	}
}
