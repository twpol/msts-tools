using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using JGR;
using JGR.Grammar;
using JGR.IO.Parser;

namespace SimisEditor
{
	public partial class Editor : Form
	{
		protected bool Modified = true;
		protected string Filename = "";
		protected string FilenameTitle {
			get {
				var title = Filename;
				if (title == "") {
					title = "Untitled";
				} else if (title.LastIndexOf('\\') > 2) {
					title = title.Substring(title.LastIndexOf('\\') + 1);
				}
				return title;
			}
		}
		protected SimisFile File;

		public Editor() {
			InitializeComponent();
			NewFile();
		}

		private void NewFile() {
			Filename = "";
			Modified = false;
			UpdateTitle();
			File = null;
			SimisTree.Nodes.Clear();
			SimisTree.ExpandAll();
		}

		private void OpenFile(string filename) {
			var applicationDirectory = @"E:\Users\James\Documents\Visual Studio 2008\Projects\JGR MSTS Editor\MSTS Route Editor";
			var BNFs = new Dictionary<string, BNF>();
			foreach (var bnfFilename in Directory.GetFiles(applicationDirectory + @"\BNF", "*.bnf")) {
				var bnf = new BNFFile(bnfFilename);
				try {
					bnf.ReadFile();
				} catch (FileException ex) {
					using (var messages = new Messages()) {
						bnf.RegisterMessageSink(messages);
						messages.MessageAccept("Editor", BufferedMessageSource.LEVEL_CRITIAL, ex.ToString());
						messages.ShowDialog();
						bnf.UnregisterMessageSink(messages);
					}
					return;
				}
				BNFs.Add(bnf.FileType + bnf.FileTypeVer, bnf.BNF);
			}

			var newFile = new SimisFile(filename, BNFs);
			try {
				newFile.ReadFile();
			} catch (FileException ex) {
				using (var messages = new Messages()) {
					newFile.RegisterMessageSink(messages);
					messages.MessageAccept("Editor", BufferedMessageSource.LEVEL_CRITIAL, ex.ToString());
					messages.ShowDialog();
					newFile.UnregisterMessageSink(messages);
				}
				return;
			}

			Filename = filename;
			Modified = false;
			UpdateTitle();
			File = newFile;
			SimisTree.Nodes.Clear();
			InsertSimisBlock(SimisTree.Nodes, File.Root);
			SimisTree.TopNode = SimisTree.Nodes[0];
		}

		private void FileModified() {
			Modified = true;
			UpdateTitle();
		}

		private void SaveFile() {
			// TODO: Save file here.
			Modified = false;
			UpdateTitle();
		}

		private void UpdateTitle() {
			Text = FilenameTitle + (Modified ? "*" : "") + " - " + Application.ProductName;
		}

		private bool InsertSimisBlock(TreeNodeCollection treeNodes, SimisBlock simisBlock) {
			var treeNode = treeNodes.Add("<fail>");
			treeNode.Tag = simisBlock;

			if (simisBlock is SimisBlockValueString) {
				var bv = (SimisBlockValue)simisBlock;
				treeNode.Name = bv.Name + "=\"" + bv + "\"";
			} else if (simisBlock is SimisBlockValue) {
				var bv = (SimisBlockValue)simisBlock;
				treeNode.Name = bv.Name + "=" + bv;
			} else {
				treeNode.Name = simisBlock.Token;
				if (simisBlock.Name.Length > 0) {
					treeNode.Name += " \"" + simisBlock.Name + "\"";
				}
			}

			var allChildrenAreValues = true;
			foreach (var child in simisBlock.Nodes) {
				allChildrenAreValues &= InsertSimisBlock(treeNode.Nodes, child);
			}
			// If we have just one child, and it is a value, omit its name.
			if ((simisBlock.Nodes.Count == 1) && (simisBlock.Nodes[0] is SimisBlockValue)) {
				if (simisBlock.Nodes[0] is SimisBlockValueString) {
					var bv = (SimisBlockValue)simisBlock.Nodes[0];
					treeNode.Nodes[0].Name = "\"" + bv + "\"";
				} else {
					var bv = (SimisBlockValue)simisBlock.Nodes[0];
					treeNode.Nodes[0].Name = bv.ToString();
				}
			}
			// If all children are values, don't expand the node.
			if (allChildrenAreValues) {
				treeNode.Text = GetNodeText(treeNode);
			} else {
				treeNode.Expand();
			}

			return (simisBlock is SimisBlockValue);
		}

		private bool SaveFileIfModified() {
			if (Modified) {
				switch (MessageBox.Show("Do you want to save changes to '" + FilenameTitle + "'?", Application.ProductName, MessageBoxButtons.YesNoCancel)) {
					case DialogResult.Yes:
						SaveFile();
						break;
					case DialogResult.Cancel:
						return false;
				}
			}
			UpdateTitle();
			return true;
		}

		private string GetNodeText(TreeNode node) {
			if (node.IsExpanded) {
				return node.Name;
			}
			return GetCollapsedNodeText(node);
		}

		private string GetCollapsedNodeText(TreeNode node) {
			if (node.Nodes.Count == 0) {
				return node.Name;
			}
			return node.Name + " (" + String.Join(" ", node.Nodes.Cast<TreeNode>().Select<TreeNode, string>(n => GetCollapsedNodeText(n)).ToArray<string>()) + ")";
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!SaveFileIfModified()) {
				return;
			}
			NewFile();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!SaveFileIfModified()) {
				return;
			}
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				OpenFile(openFileDialog.FileName);
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFile();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
			if (saveFileDialog.ShowDialog() == DialogResult.OK) {
				Filename = saveFileDialog.FileName;
				SaveFile();
			}
		}


		private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!SaveFileIfModified()) {
				return;
			}
			Close();
		}

		private void SimisTree_AfterCollapse(object sender, TreeViewEventArgs e) {
			e.Node.Text = GetNodeText(e.Node);
		}

		private void SimisTree_AfterExpand(object sender, TreeViewEventArgs e) {
			e.Node.Text = GetNodeText(e.Node);
		}
	}
}
