//------------------------------------------------------------------------------
// Jgr.Gui library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Windows.Forms;

namespace Jgr.Gui {
	public partial class FeedbackPrompt : Form {
		public string AllData;

		public FeedbackPrompt() {
			InitializeComponent();
		}

		void LinkViewAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			TaskDialog.Show(this, TaskDialogCommonIcon.None, "Collected Data for Feedback", "IMPORTANT: Only the following data is collected; any incidental information, e.g. IP Address, is not saved with the report.\n\n" + AllData);
		}

		void Feedback_Shown(object sender, EventArgs e) {
			TextComments.Focus();
		}
	}
}
