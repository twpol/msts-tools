//------------------------------------------------------------------------------
// Simis Editor, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using JGR;

namespace SimisEditor
{
	public partial class Messages : Form, IMessageSink
	{
		public Messages() {
			InitializeComponent();
			UpdateColumnWidths();
			UpdateSelectedMessage();
		}

		private void UpdateColumnWidths() {
			int width = ListOfMessages.ClientRectangle.Width;
			for (int i = 0; i < ListOfMessages.Columns.Count; i++) {
				if (i != 2) {
					width -= ListOfMessages.Columns[i].Width;
				}
			}
			ListOfMessages.Columns[2].Width = (width > 50 ? width : 50);
		}

		private void UpdateSelectedMessage() {
			if (ListOfMessages.SelectedItems.Count > 0) {
				MessageText.Lines = ((string)ListOfMessages.SelectedItems[0].SubItems[2].Text).Split('\n');
			} else {
				MessageText.Text = "<no message selected>";
			}
		}

		#region IMessageSink Members

		public void MessageAccept(string source, byte level, string message) {
			var item = ListOfMessages.Items.Add(source);
			item.SubItems.Add(level.ToString());
			item.SubItems.Add(message);
			if (item.SubItems[0].Text.LastIndexOf(".") > 0) {
				item.SubItems[0].Text = item.SubItems[0].Text.Substring(item.SubItems[0].Text.LastIndexOf(".") + 1);
			}
		}

		#endregion

		private void Messages_Resize(object sender, EventArgs e) {
			UpdateColumnWidths();
		}

		private void ListOfMessages_SelectedIndexChanged(object sender, EventArgs e) {
			UpdateSelectedMessage();
		}

		private void Messages_Shown(object sender, EventArgs e) {
			UpdateColumnWidths();
			ListOfMessages.FocusedItem = ListOfMessages.Items[ListOfMessages.Items.Count - 1];
			ListOfMessages.FocusedItem.Selected = true;
			ListOfMessages.TopItem = ListOfMessages.FocusedItem;
		}
	}
}
