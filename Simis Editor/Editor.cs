//------------------------------------------------------------------------------
// Simis Editor, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Jgr;
using Jgr.Grammar;
using Jgr.Gui;
using Jgr.IO.Parser;
using Microsoft.CSharp;
using SimisEditor.Properties;

namespace SimisEditor
{
	public partial class Editor : Form
	{
		static TraceSwitch TraceSwitch = new TraceSwitch("editor", "Trace Editor");

		string Filename = "";
		string FilenameTitle {
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
		SimisFile File;
		SimisTreeNode SavedFileTree;
		TreeNode SelectedNode;
		TreeNode ContextNode;
		SimisProvider SimisProvider;

		#region Initialization

		public Editor() {
			InitializeComponent();
			InitializeEditor();
			InitializeSimisProvider();
			InitializeNewVersionCheck();
			InitializeFromCommandLine();
		}

		void InitializeEditor() {
			ToolStripManager.Renderer = new ToolStripNativeRenderer();
			foreach (ToolStripMenuItem item in menuStrip.Items) {
				InitializeMenu(item);
			}
		}

		void InitializeMenu(ToolStripItem toolStripItem) {
			var menuItem = toolStripItem as ToolStripMenuItem;
			if (menuItem == null) {
				return;
			}
			if (!String.IsNullOrEmpty(menuItem.ToolTipText)) {
				menuItem.MouseEnter += new EventHandler(toolStrip_Enter);
				menuItem.MouseLeave += new EventHandler(toolStrip_Leave);
			}
			menuItem.DropDown.ShowItemToolTips = false;
			foreach (ToolStripItem item in menuItem.DropDown.Items) {
				InitializeMenu(item);
			}
		}

		void toolStrip_Enter(object sender, EventArgs e) {
			statusBarLabel.Text = ((ToolStripMenuItem)sender).ToolTipText;
		}

		void toolStrip_Leave(object sender, EventArgs e) {
			statusBarLabel.Text = "";
		}

		void InitializeNewVersionCheck() {
			if (Environment.GetCommandLineArgs().Contains("/noversioncheck") || Environment.GetCommandLineArgs().Contains("-noversioncheck") || (Settings.Default.UpdateCheckLastTime >= DateTime.Today)) {
				return;
			}

			Settings.Default.UpdateCheckLastTime = DateTime.Today;
			Settings.Default.Save();

			var versionCheck = new CodePlexVersionCheck(Settings.Default.UpdateCheckCodePlexProjectUrl, Settings.Default.UpdateCheckCodePlexProjectName, Settings.Default.UpdateCheckCodePlexReleaseDate);
			versionCheck.CheckComplete += (o, e) => this.Invoke((MethodInvoker)(() => {
				if (versionCheck.HasLatestVersion) {
					if (versionCheck.IsNewVersion) {
						var item = menuStrip.Items.Add("New Version: " + versionCheck.LatestVersionTitle);
						item.Alignment = ToolStripItemAlignment.Right;
						item.Click += (o2, e2) => Process.Start(versionCheck.LatestVersionUri.AbsoluteUri);
					}
				} else {
					var item = menuStrip.Items.Add("Error Checking for New Version");
					item.Alignment = ToolStripItemAlignment.Right;
					item.Click += (o2, e2) => Process.Start(Settings.Default.AboutUpdatesUrl);
				}
			}));
			versionCheck.Check();
		}

		void InitializeSimisProvider() {
			var resourcesDirectory = Application.ExecutablePath;
			resourcesDirectory = resourcesDirectory.Substring(0, resourcesDirectory.LastIndexOf('\\')) + @"\Resources";
			SimisProvider = new SimisProvider(resourcesDirectory);
			var thread = new Thread(() => WaitForSimisProvider());
			thread.Start();
		}

		bool WaitForSimisProvider() {
			try {
				SimisProvider.Join();
			} catch (FileException ex) {
				this.Invoke((MethodInvoker)(() => {
					using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
						ShowMessageBox(ex.ToString(), "Load Resources", MessageBoxIcon.Error);
					}
				}));
				return false;
			}
			return true;
		}

		bool UpdateFromSimisProvider() {
			if (!WaitForSimisProvider()) return false;

			var generalName = "Train Simulator files";

			var simisFormats = new List<List<string>>();
			simisFormats.Add(new List<string>(new string[] { "All " + generalName }));
			foreach (var format in SimisProvider.Formats) {
				var mask = format.Extension.Contains(".") ? format.Extension : "*." + format.Extension;
				simisFormats[0].Add(mask);
				simisFormats.Add(new List<string>(new string[] { format.Name + " files", mask }));
			}
			simisFormats.Add(new List<string>(new string[] { "All files", "*.*" }));
			openFileDialog.Filter = String.Join("|", simisFormats.Select(l => l[0] + "|" + String.Join(";", l.ToArray(), 1, l.Count - 1)).ToArray());

			var streamFormats = new string[] { "Text", "Binary", "Compressed Binary" };
			saveFileDialog.Filter = String.Join("|", streamFormats.Select(s => s + " " + generalName + "|" + String.Join(";", simisFormats[0].ToArray(), 1, simisFormats[0].Count - 1)).ToArray());

			return true;
		}

		void InitializeFromCommandLine() {
			this.Shown += (o, e) => {
				foreach (var argument in Environment.GetCommandLineArgs().Where((s, i) => i > 0)) {
					if (argument.StartsWith("/", StringComparison.Ordinal) || argument.StartsWith("-", StringComparison.Ordinal)) continue;
					OpenFile(argument);
					break;
				}
			};
		}

		#endregion

		#region File Operations (New, Open, Save)

		void NewFile() {
			WaitForSimisProvider();
			Filename = "";
			File = new SimisFile("", SimisProvider);
			SavedFileTree = File.Tree;
			SelectNode(null);
			ResyncSimisNodes();
			UpdateTitle();
			UpdateMenu();
		}

		bool OpenFile(string filename) {
			if (!WaitForSimisProvider()) return false;
			var newFile = new SimisFile(filename, SimisProvider);
			try {
				newFile.ReadFile();
			} catch (FileException ex) {
				using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
					ShowMessageBox(ex.ToString(), "Open File", MessageBoxIcon.Error);
				}
				return false;
			}

			Filename = filename;
			File = newFile;
			SavedFileTree = File.Tree;
			SimisTree.SelectedNode = null;
			ResyncSimisNodes();
			SimisTree.ExpandAll();
			if (SimisTree.Nodes.Count > 0) {
				SimisTree.TopNode = SimisTree.Nodes[0];
			}
			UpdateTitle();
			UpdateMenu();
			return true;
		}

		bool SaveFile() {
			if (File.FileName != Filename) {
				File.FileName = Filename;
			}
			try {
				File.WriteFile();
			} catch (FileException ex) {
				using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
					if (ex.InnerException is UnauthorizedAccessException) {
						ShowMessageBox(ex.InnerException.Message, "Save File", MessageBoxIcon.Error);
					} else {
						ShowMessageBox(ex.ToString(), "Save File", MessageBoxIcon.Error);
					}
				}
				return false;
			}

			SavedFileTree = File.Tree;
			UpdateTitle();
			UpdateMenu();
			return true;
		}

		bool SaveFileIfModified() {
			if ((File != null) && (File.Tree != SavedFileTree)) {
				using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
					switch (ShowMessageBox("Do you want to save changes to '" + FilenameTitle + "'?", "", MessageBoxButtons.YesNoCancel)) {
						case DialogResult.Yes:
							if (!SaveFile()) {
								return false;
							}
							break;
						case DialogResult.Cancel:
							return false;
					}
				}
			}
			UpdateTitle();
			UpdateMenu();
			return true;
		}

		#endregion

		#region Menu Events - File

		void newToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!SaveFileIfModified()) {
				return;
			}
			NewFile();
		}

		void openToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!SaveFileIfModified()) {
				return;
			}
			if (!UpdateFromSimisProvider()) {
				return;
			}
			if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
				OpenFile(openFileDialog.FileName);
			}
		}

		void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFile();
		}

		void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
			// FilterIndex is 1-based, SIGH. Filters: 1=Text, 2=Binary, 3=Compressed Binary.
			if (!UpdateFromSimisProvider()) {
				return;
			}
			saveFileDialog.FilterIndex = File.StreamCompressed ? 3 : File.StreamFormat == SimisStreamFormat.Text ? 1 : 2;
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK) {
				Filename = saveFileDialog.FileName;
				File.StreamFormat = saveFileDialog.FilterIndex == 1 ? SimisStreamFormat.Text : SimisStreamFormat.Binary;
				File.StreamCompressed = saveFileDialog.FilterIndex == 3;
				SaveFile();
			}
		}

		void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			Close();
		}

		#endregion

		#region Menu Events - Edit

		void UpdateEditState() {
			cutToolStripMenuItem.Enabled = EditState.CanCut;
			copyToolStripMenuItem.Enabled = EditState.CanCopy;
			pasteToolStripMenuItem.Enabled = EditState.CanPaste;
			deleteToolStripMenuItem.Enabled = EditState.CanDelete;
			selectAllToolStripMenuItem.Enabled = EditState.CanSelectAll;
		}

		void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e) {
			UpdateEditState();
		}

		void undoToolStripMenuItem_Click(object sender, EventArgs e) {
			File.Undo();
			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
			UpdateTitle();
			UpdateMenu();
		}

		void redoToolStripMenuItem_Click(object sender, EventArgs e) {
			File.Redo();
			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
			UpdateTitle();
			UpdateMenu();
		}

		void cutToolStripMenuItem_Click(object sender, EventArgs e) {
			EditState.DoCut();
		}

		void copyToolStripMenuItem_Click(object sender, EventArgs e) {
			EditState.DoCopy();
		}

		void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
			EditState.DoPaste();
		}

		void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
			EditState.DoDelete();
		}

		void selectAllToolStripMenuItem_Click(object sender, EventArgs e) {
			EditState.DoSelectAll();
		}

		#endregion

		#region Menu Events - Help

		void homepageToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start(Settings.Default.AboutHomepageUrl);
		}

		void updatesToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start(Settings.Default.AboutUpdatesUrl);
		}

		void discussionsToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start(Settings.Default.AboutDiscussionsUrl);
		}

		void issueTrackerToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start(Settings.Default.AboutIssuesUrl);
		}

		void reloadSimisResourcesToolStripMenuItem_Click(object sender, EventArgs e) {
			InitializeSimisProvider();
		}

		#endregion

		#region Main Events (Editor, SimisTree, SimisProperties)

		void Editor_FormClosing(object sender, FormClosingEventArgs e) {
			e.Cancel = !SaveFileIfModified();
		}

		void Editor_DragEnter(object sender, DragEventArgs e) {
			e.Effect = DragDropEffects.None;
			if (!e.Data.GetDataPresent(DataFormats.FileDrop)) {
				return;
			}
			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length != 1) {
				return;
			}
			if (!WaitForSimisProvider()) {
				return;
			}
			if (!SimisProvider.Formats.Any((f) => files[0].EndsWith("." + f.Extension, StringComparison.OrdinalIgnoreCase))) {
				return;
			}
			e.Effect = DragDropEffects.Copy;
		}

		void Editor_DragDrop(object sender, DragEventArgs e) {
			if (!SaveFileIfModified()) {
				return;
			}
			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			OpenFile(files[0]);
		}

		void SimisTree_Enter(object sender, EventArgs e) {
			UpdateEditState();
		}

		void SimisTree_Leave(object sender, EventArgs e) {
			UpdateEditState();
		}

        void SimisTree_AfterSelect(object sender, TreeViewEventArgs e) {
            SelectNode(e.Node);
        }

		void SimisTree_MouseUp(object sender, MouseEventArgs e) {
			// Basically fake right-clicking the tree so we can get the tree node that was right-clicked (KB810001).
			if (e.Button == MouseButtons.Right) {
				var point = new Point(e.X, e.Y + 1);
				var node = SimisTree.GetNodeAt(point);
				if (node != null) {
					ContextNode = node;
					contextMenuStrip.Show(SimisTree, point);
				}
			}
		}

		void contextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e) {
			// No ContextNode means we're not coming from SimisTree_MouseUp; most likely from context menu key.
			if (ContextNode == null) {
				ContextNode = SimisTree.SelectedNode;
			}

			// No context at all, give up thanks.
			if ((ContextNode == null) || (ContextNode.Tag == null)) {
				e.Cancel = true;
				return;
			}

			// Make sure the user knows what they're on, as the tree doesn't keep the selection on the item.
			var target = ((SimisTreeNode)ContextNode.Tag);
			nodeLabelToolStripMenuItem.Visible = false;
			var needSeparator = false;
			// Remove all existing menu items.
			for (var i = 0; i < contextMenuStrip.Items.Count; i++) {
				if (contextMenuStrip.Items[i].Tag is string) {
					contextMenuStrip.Items.RemoveAt(i);
					i--;
				}
			}

			var hasChildren = ContextNode.FirstNode != null;
			var isValueNode = target is SimisTreeNodeValue;

			// Collect all the possible paths to insert before this node.
			var pathsBefore = GetBnfPaths(ContextNode.PrevNode, ContextNode, ContextNode.Parent);
			// Collect all the possible paths to insert before this node's first child IF it has children (otherwise no children means pathsChildBefore == pathsChildAfter).
			var pathsChildBefore = !isValueNode && hasChildren ? GetBnfPaths(null, ContextNode.FirstNode, ContextNode) : new string[0][];
			// Collect all the possible paths to insert after this node's last child.
			var pathsChildAfter = !isValueNode ? GetBnfPaths(ContextNode.LastNode, null, ContextNode) : new string[0][];
			// Collect all the possible paths to insert after this node.
			var pathsAfter = GetBnfPaths(ContextNode, ContextNode.NextNode, ContextNode.Parent);

			// Action to add a menu separator; we put one between each group of paths to insert.
			Action addMenuSeparator = () => {
				var item = contextMenuStrip.Items.Add("-");
				item.Tag = "";
			};

			// Action to add a path insertion item; hooks up the event and data.
			Action<string, string> addMenuItem = (label, path) => {
				var item = contextMenuStrip.Items.Add(label);
				item.Tag = path;
				item.Click += new EventHandler(contextMenuStripItem_Click);
			};

			if (pathsBefore.Any(_ => true)) {
				if (needSeparator) addMenuSeparator();
				foreach (var path in pathsBefore.OrderBy(p => String.Join(", ", p.ToArray()))) {
					addMenuItem("Insert " + String.Join(", ", path.ToArray()) + " before " + target.Type, "[insertbefore]" + String.Join(" ", path.ToArray()));
				}
				needSeparator = true;
			}
			if (pathsChildBefore.Any(_ => true)) {
				if (needSeparator) addMenuSeparator();
				foreach (var path in pathsChildBefore.OrderBy(p => String.Join(", ", p.ToArray()))) {
					addMenuItem("Prepend " + String.Join(", ", path.ToArray()) + " to " + target.Type, "[childbefore]" + String.Join(" ", path.ToArray()));
				}
				needSeparator = true;
			}
			if (pathsChildAfter.Any(_ => true)) {
				if (needSeparator) addMenuSeparator();
				foreach (var path in pathsChildAfter.OrderBy(p => String.Join(", ", p.ToArray()))) {
					addMenuItem("Append " + String.Join(", ", path.ToArray()) + " to " + target.Type, "[childafter]" + String.Join(" ", path.ToArray()));
				}
				needSeparator = true;
			}
			if (pathsAfter.Any(_ => true)) {
				if (needSeparator) addMenuSeparator();
				foreach (var path in pathsAfter.OrderBy(p => String.Join(", ", p.ToArray()))) {
					addMenuItem("Insert " + String.Join(", ", path.ToArray()) + " after " + target.Type, "[insertafter]" + String.Join(" ", path.ToArray()));
				}
				needSeparator = true;
			}
			if (!needSeparator) {
				nodeLabelToolStripMenuItem.Text = "No modifications possible here";
				nodeLabelToolStripMenuItem.Visible = true;
			}
		}

		void contextMenuStripItem_Click(object sender, EventArgs e) {
			var item = sender as ToolStripItem;
			var newItems = (string)item.Tag;
			Debug.Assert(newItems.StartsWith("["));
			var action = newItems.Substring(1, newItems.IndexOf("]") - 1);
			newItems = newItems.Substring(newItems.IndexOf("]") + 1);

			var node = ContextNode;
			var blockPath = new Stack<SimisTreeNode>();
			while (node != null) {
				blockPath.Push((SimisTreeNode)node.Tag);
				node = node.Parent;
			}
			var blockPathList = new List<SimisTreeNode>(blockPath);
			var simisTreeNode = (SimisTreeNode)ContextNode.Tag;
			var newNodes = newItems.Split(' ').Select(s => SimisTreeNodeValue.NodeTypes.ContainsKey(s) ? (SimisTreeNode)SimisTreeNodeValue.NodeTypes[s].GetConstructor(new Type[] { typeof(string), typeof(string) }).Invoke(new object[] { s, "" }) : new SimisTreeNode(s, ""));

			switch (action) {
				case "insertbefore":
				case "insertafter":
					var targetNode = blockPathList.Last();
					blockPathList.Remove(targetNode);
					if (action == "insertafter") {
						targetNode = blockPathList.Last().GetNextSibling(targetNode);
					}
					foreach (var newNode in newNodes) {
						File.Tree = File.Tree.Apply(blockPathList, n => n.InsertChild(newNode, targetNode));
					}
					break;
				case "childbefore":
					targetNode = blockPathList.Last().GetFirstChild();
					foreach (var newNode in newNodes) {
						File.Tree = File.Tree.Apply(blockPathList, n => n.InsertChild(newNode, targetNode));
					}
					break;
				case "childafter":
					foreach (var newNode in newNodes) {
						File.Tree = File.Tree.Apply(blockPathList, n => n.AppendChild(newNode));
					}
					break;
			}

			// TODO: New Simis nodes need filling in with the minimum path through the BNF!

			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
			UpdateTitle();
			UpdateMenu();
		}

		void SimisProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
			if (SelectedNode == null) {
				return;
			}

			var node = SelectedNode;
			if (node.Tag is SimisTreeNodeValue) node = node.Parent;
			var blockPath = new Stack<SimisTreeNode>();
			while (node != null) {
				blockPath.Push((SimisTreeNode)node.Tag);
				node = node.Parent;
			}

			var child = (SimisTreeNodeValue)SimisProperties.SelectedObject.GetType().GetProperty(e.ChangedItem.Label + "_SimisTreeNodeValue").GetValue(SimisProperties.SelectedObject, null);
			var value = e.ChangedItem.Value;

			var blockPathList = new List<SimisTreeNode>(blockPath);
			if (child is SimisTreeNodeValueString) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueString(child.Type, child.Name, (string)value), child));
			} else if (child is SimisTreeNodeValueIntegerUnsigned) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueIntegerUnsigned(child.Type, child.Name, (uint)value), child));
			} else if (child is SimisTreeNodeValueIntegerSigned) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueIntegerSigned(child.Type, child.Name, (int)value), child));
			} else if (child is SimisTreeNodeValueIntegerDWord) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueIntegerDWord(child.Type, child.Name, (uint)value), child));
			} else if (child is SimisTreeNodeValueIntegerWord) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueIntegerWord(child.Type, child.Name, (ushort)value), child));
			} else if (child is SimisTreeNodeValueIntegerByte) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueIntegerByte(child.Type, child.Name, (byte)value), child));
			} else if (child is SimisTreeNodeValueFloat) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueFloat(child.Type, child.Name, (float)value), child));
			}

			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
			UpdateTitle();
			UpdateMenu();
		}

		#endregion

		DialogResult ShowMessageBox(string text, string caption, MessageBoxButtons buttons) {
			return ShowMessageBox(text, caption, buttons, MessageBoxIcon.None);
		}

		DialogResult ShowMessageBox(string text, string caption, MessageBoxIcon icon) {
			return ShowMessageBox(text, caption, MessageBoxButtons.OK, icon);
		}

		DialogResult ShowMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) {
			return MessageBox.Show(text, caption + (caption.Length > 0 ? " - " : "") + Application.ProductName, buttons, icon, MessageBoxDefaultButton.Button1, RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading : 0);
		}

		void UpdateTitle() {
			if (File == null) {
				Text = Application.ProductName;
			} else {
				Text = FilenameTitle + (File.Tree != SavedFileTree ? "*" : "") + " - " + Application.ProductName;
			}
		}

		void UpdateMenu() {
			saveToolStripMenuItem.Enabled = File != null;
			saveAsToolStripMenuItem.Enabled = File != null;
		}

		void ResyncSimisNodes() {
			undoToolStripMenuItem.Enabled = File.CanUndo;
			redoToolStripMenuItem.Enabled = File.CanRedo;
			if (TraceSwitch.TraceVerbose) Trace.WriteLine(File.Tree.ToString());
			ResyncSimisNodes(SimisTree.Nodes, File.Tree);
		}

		void ResyncSimisNodes(TreeNodeCollection viewTreeNodes, IList<SimisTreeNode> simisTreeNodes) {
			var viewTreeNodeIndex = 0;
			var simisTreeNodeIndex = 0;

			if (simisTreeNodes.All(n => n is SimisTreeNodeValue)) {
				simisTreeNodes = new SimisTreeNode[0];
			}

			while ((viewTreeNodeIndex < viewTreeNodes.Count) && (simisTreeNodeIndex < simisTreeNodes.Count)) {
				if (viewTreeNodes[viewTreeNodeIndex].Tag == simisTreeNodes[simisTreeNodeIndex]) {
					// This view node is the same simis node, so it is unchanged.
					viewTreeNodeIndex++;
					simisTreeNodeIndex++;
					continue;
				}

				if (simisTreeNodes[simisTreeNodeIndex].EqualsByValue(viewTreeNodes[viewTreeNodeIndex].Tag)) {
					viewTreeNodes[viewTreeNodeIndex].Text = GetNodeText(simisTreeNodes[simisTreeNodeIndex]);
					viewTreeNodes[viewTreeNodeIndex].Tag = simisTreeNodes[simisTreeNodeIndex];
					if (TraceSwitch.TraceVerbose) Trace.WriteLine(viewTreeNodes[viewTreeNodeIndex].FullPath + " [VN:Updated]");
					ResyncSimisNodes(viewTreeNodes[viewTreeNodeIndex].Nodes, simisTreeNodes[simisTreeNodeIndex]);
					viewTreeNodeIndex++;
					simisTreeNodeIndex++;
					continue;
				}

				// View node may have been deleted or had a node inserted before in simis tree.
				for (var i = simisTreeNodeIndex + 1; i < simisTreeNodes.Count; i++) {
					if (viewTreeNodes[viewTreeNodeIndex].Tag == simisTreeNodes[i]) {
						// View node has had at least one node inserted before it in the simis tree.
						for (var j = simisTreeNodeIndex; j < i; j++) {
							var viewNode = viewTreeNodes.Insert(viewTreeNodeIndex++, GetNodeText(simisTreeNodes[j]));
							viewNode.Tag = simisTreeNodes[j];
							if (TraceSwitch.TraceVerbose) Trace.WriteLine(viewNode.FullPath + " [VN:Insert]");
							ResyncSimisNodes(viewNode.Nodes, simisTreeNodes[j]);
						}
						simisTreeNodeIndex = i;
						continue;
					}
				}
				// This view node wasn't found, remove it.
				if (TraceSwitch.TraceVerbose) Trace.WriteLine(viewTreeNodes[viewTreeNodeIndex].FullPath + " [VN:Remove]");
				viewTreeNodes.RemoveAt(viewTreeNodeIndex);
			}

			while (viewTreeNodeIndex < viewTreeNodes.Count) {
				viewTreeNodes.RemoveAt(viewTreeNodeIndex);
			}

			while (simisTreeNodeIndex < simisTreeNodes.Count) {
				var viewNode = viewTreeNodes.Add(GetNodeText(simisTreeNodes[simisTreeNodeIndex]));
				viewNode.Tag = simisTreeNodes[simisTreeNodeIndex];
				if (TraceSwitch.TraceVerbose) Trace.WriteLine(viewNode.FullPath + " [VN:Add]");
				ResyncSimisNodes(viewNode.Nodes, simisTreeNodes[simisTreeNodeIndex]);
				simisTreeNodeIndex++;
			}
		}

        void SelectNode(TreeNode treeNode) {
			if ((treeNode == null) || (treeNode.Tag == null)) {
				SelectedNode = null;
				statusBarLabel.Text = "";
				SimisProperties.SelectedObject = null;
				return;
			}
			SelectedNode = treeNode;
			statusBarLabel.Text = GetBnfLocation(File, SelectedNode);
			SimisProperties.SelectedObject = CreateEditObjectFor((SimisTreeNode)SelectedNode.Tag);
        }

		static string BlockToNameString(SimisTreeNode block) {
			if (block.Name.Length > 0) {
				return block.Type + " \"" + block.Name + "\"";
			}
			return block.Type;
		}

		static string BlockToNameOnlyString(SimisTreeNode block) {
			if (block.Name.Length > 0) {
				return block.Name;
			}
			return block.Type;
		}

		static string BlockToValueString(SimisTreeNodeValue block) {
			if (block is SimisTreeNodeValueString) {
				return "\"" + block.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
			}
			return block.ToString();
		}

		static string GetNodeText(SimisTreeNode block) {
			// Non-trivial node, don't bother trying.
			if (block.Count(b => !(b is SimisTreeNodeValue)) > 0) {
				return BlockToNameString(block);
			}

			// Just a single value child, easy.
			if (block.Count == 1) {
				return BlockToNameOnlyString(block) + "=" + BlockToValueString((SimisTreeNodeValue)block[0]);
			}

			// Multiple value children, quite easy.
			if (block.Count > 1) {
				var values = new List<string>();
				foreach (var child in block) {
					values.Add(BlockToNameOnlyString(child) + "=" + BlockToValueString((SimisTreeNodeValue)child));
				}
				return BlockToNameString(block) + " (" + String.Join(" ", values.ToArray()) + ")";
			}

			// No children.
			var blockValue = block as SimisTreeNodeValue;
			if (blockValue != null) {
				return BlockToNameOnlyString(block) + "=" + BlockToValueString(blockValue);
			}

			return BlockToNameString(block);
		}

		object CreateEditObjectFor(SimisTreeNode block) {
			if (block.Any(b => !(b is SimisTreeNodeValue))) {
				return null;
			}

			var dClassName = "block_" + block.Type.Replace(".", "_");

			// Create constructor.
			var dConstructor = new CodeConstructor();
			dConstructor.Attributes = MemberAttributes.Public;

			// Storage and action for adding a property to our class. Converts
			// a single SimisTreeNode into a single property, with correct
			// naming and typing.
			var dFields = new List<CodeMemberField>();
			var dProperties = new List<CodeMemberProperty>();
			var dBlockNames = new List<string>();
			var dPropertyValues = new Dictionary<string, object>();
			Action<SimisTreeNode> AddPropertyFor = (node) => {
				var dPropertyName = node.Name.Length > 0 ? node.Name : node.Type;
				if (block.Count(b => (b.Name.Length > 0 ? b.Name : b.Type) == dPropertyName) > 1) {
					var index = 1;
					while (dBlockNames.Contains((index == 0 ? "" : "_" + index.ToString("D2") + "_") + dPropertyName)) index++;
					dPropertyName = "_" + index.ToString("D2") + "_" + dPropertyName;
					dBlockNames.Add(dPropertyName);
				}
				if (block.Count == 1) {
					dPropertyName = block.Type;
				}

				var nodeValue = node as SimisTreeNodeValue;
				if (nodeValue == null) {
					return;
				}

				// Store the SimisBlockValue in a way we can find later.
				var dSimisProperty = CreateEditObjectFor_AddProperty(dFields, dProperties, dPropertyName + "_SimisTreeNodeValue", new CodeTypeReference(typeof(SimisTreeNodeValue)));
				dSimisProperty.CustomAttributes.Add(new CodeAttributeDeclaration("BrowsableAttribute", new CodeAttributeArgument(new CodePrimitiveExpression(false))));
				dPropertyValues.Add(dSimisProperty.Name, nodeValue);

				CodeTypeReference type;
				if (node is SimisTreeNodeValueString) {
					type = new CodeTypeReference(typeof(string));
				} else if (node is SimisTreeNodeValueIntegerUnsigned) {
					type = new CodeTypeReference(typeof(uint));
				} else if (node is SimisTreeNodeValueIntegerSigned) {
					type = new CodeTypeReference(typeof(int));
				} else if (node is SimisTreeNodeValueIntegerDWord) {
					type = new CodeTypeReference(typeof(uint));
				} else if (node is SimisTreeNodeValueIntegerWord) {
					type = new CodeTypeReference(typeof(ushort));
				} else if (node is SimisTreeNodeValueIntegerByte) {
					type = new CodeTypeReference(typeof(byte));
				} else if (node is SimisTreeNodeValueFloat) {
					type = new CodeTypeReference(typeof(float));
				} else {
					return;
				}

				var dProperty = CreateEditObjectFor_AddProperty(dFields, dProperties, dPropertyName, type);
				if (node is SimisTreeNodeValueString) {
					//dProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Editor", new CodeAttributeArgument(new CodePrimitiveExpression("System.ComponentModel.Design.ArrayEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")), new CodeAttributeArgument(new CodeTypeOfExpression("UITypeEditor"))));
					dProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Editor", new CodeAttributeArgument(new CodePrimitiveExpression("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")), new CodeAttributeArgument(new CodeTypeOfExpression("UITypeEditor"))));
				}

				dPropertyValues.Add(dProperty.Name, ((SimisTreeNodeValue)node).Value);
			};

			// Go through the Simis data to find suitable things for editing.
			if (block is SimisTreeNodeValue) {
				AddPropertyFor(block);
			} else {
				foreach (var child in block) {
					AddPropertyFor(child);
				}
			}

			// Create edit class. The attribute [TypeConverter(typeof(ExpandableObjectConverter))] allows the property grid to expand nested items.
			var dClass = new CodeTypeDeclaration(dClassName);
			dClass.IsClass = true;
			dClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
			dClass.CustomAttributes.Add(new CodeAttributeDeclaration("TypeConverter", new CodeAttributeArgument(new CodeTypeOfExpression("ExpandableObjectConverter"))));
			dClass.Members.Add(dConstructor);
			dClass.Members.AddRange(dFields.ToArray());
			dClass.Members.AddRange(dProperties.ToArray());

			// Create namespace, including imports needed for various code created below.
			var dNamespace = new CodeNamespace("SimisEditor.Editor.Dynamic");
			dNamespace.Imports.Add(new CodeNamespaceImport("System"));
			dNamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
			dNamespace.Imports.Add(new CodeNamespaceImport("System.Drawing.Design"));
			dNamespace.Imports.Add(new CodeNamespaceImport("Jgr.IO.Parser"));
			dNamespace.Types.Add(dClass);

			// Create compile unit, containing our dynamic namespace and classes.
			var dUnit = new CodeCompileUnit();
			dUnit.ReferencedAssemblies.Add("System.dll");
			dUnit.ReferencedAssemblies.Add("System.Drawing.dll");
			dUnit.ReferencedAssemblies.Add("System.Windows.Forms.dll");
			dUnit.ReferencedAssemblies.Add("JGR.IO.Parser.dll");
			dUnit.Namespaces.Add(dNamespace);

			// Set up compiler options.
			var compilerParams = new CompilerParameters();
			compilerParams.GenerateExecutable = false;
			compilerParams.GenerateInMemory = true;

			// Get the C# compiler and build a new assembly from the code we've just created.
			var compiler = new CSharpCodeProvider();
			var compileResults = compiler.CompileAssemblyFromDom(compilerParams, dUnit);

			// Did it work? Did it?
			if (compileResults.Errors.HasErrors) {
				// We failed so write out a file the user can look at.
				using (var writer = new StreamWriter(Application.ExecutablePath + @"\..\CreateEditObjectFor.cs", false, Encoding.UTF8)) {
					compiler.GenerateCodeFromCompileUnit(dUnit, writer, new CodeGeneratorOptions());
				}

				var messages = new string[compileResults.Output.Count];
				compileResults.Output.CopyTo(messages, 0);

				using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
					ShowMessageBox("CreateEditObjectFor() created C# code which failed to compile. Please look at 'CreateEditObjectFor.cs' in the application directory for the code.\n\n"
						+ String.Join("\n", messages), "Create Property Editor Data", MessageBoxIcon.Error);
				}
				return null;
			}

			var dType = compileResults.CompiledAssembly.GetType(dNamespace.Name + "." + dClass.Name);
			var dTypeConstructor = dType.GetConstructor(new Type[] { });
			var dInstance = dTypeConstructor.Invoke(new object[] { });

			foreach (var prop in dPropertyValues) {
				dInstance.GetType().GetProperty(prop.Key).SetValue(dInstance, prop.Value, new object[] { });
			}

			return dInstance;
		}

		static CodeMemberProperty CreateEditObjectFor_AddProperty(List<CodeMemberField> dFields, List<CodeMemberProperty> dProperties, string dPropertyName, CodeTypeReference type) {
			var dField = new CodeMemberField();
			dField.Attributes = MemberAttributes.Private;
			dField.Name = "_" + dPropertyName;
			dField.Type = type;
			dFields.Add(dField);

			var dProperty = new CodeMemberProperty();
			dProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			dProperty.Name = dPropertyName;
			dProperty.Type = type;
			dProperty.HasGet = true;
			dProperty.HasSet = true;
			dProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), dField.Name)));
			dProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), dField.Name), new CodeVariableReferenceExpression("value")));
			dProperties.Add(dProperty);
			return dProperty;
		}

		string GetBnfLocation(SimisFile file, TreeNode treeNode) {
			var nodes = new Stack<KeyValuePair<SimisTreeNode, bool>>();
			while (treeNode != null) {
				nodes.Push(new KeyValuePair<SimisTreeNode, bool>((SimisTreeNode)treeNode.Tag, true));
				//while (treeNode.PrevNode != null) {
				//    treeNode = treeNode.PrevNode;
				//    nodes.Push(new KeyValuePair<SimisTreeNode, bool>((SimisTreeNode)treeNode.Tag, false));
				//}
				treeNode = treeNode.Parent;
			}
			var str = String.Join("", nodes.Select(n => n.Key.Type + (n.Value ? " > " : " + ")).ToArray());
			return str.Substring(0, str.Length - 3);

			//var bnfState = GetBnfState(treeNode);
			//return bnfState.ToString();
		}

		BnfState GetBnfState(TreeNode treeNode) {
			return GetBnfState(treeNode, false);
		}

		BnfState GetBnfState(TreeNode treeNode, bool beforeChildren) {
			var nodes = new List<SimisTreeNode>();
			while (treeNode != null) {
				nodes.Insert(0, (SimisTreeNode)treeNode.Tag);
				treeNode = treeNode.Parent;
			}
			var bnfState = new BnfState(File.SimisFormat.Bnf);
			for (var i = 1; i < nodes.Count; i++) {
				NavigateBnfStateThrough(bnfState, nodes[i - 1], nodes[i]);
			}
			if (nodes.Count > 0) {
				if (beforeChildren) {
					bnfState.MoveTo(nodes[nodes.Count - 1].Type);
					if (bnfState.IsEnterBlockTime) {
						bnfState.EnterBlock();
					}
				} else {
					NavigateBnfStateThrough(bnfState, nodes[nodes.Count - 1], null);
				}
			}
			return bnfState;
		}

		void NavigateBnfStateThrough(BnfState bnfState, SimisTreeNode node, SimisTreeNode child) {
			bnfState.MoveTo(node.Type);
			if (bnfState.IsEnterBlockTime) {
				bnfState.EnterBlock();
				foreach (var cnode in node) {
					if (cnode == child) return;
					NavigateBnfStateThrough(bnfState, cnode, null);
				}
				bnfState.LeaveBlock();
			}
		}

		IEnumerable<IEnumerable<string>> GetBnfPaths(TreeNode treeNodeStart, TreeNode treeNodeFinish, TreeNode treeNodeParent) {
			if ((treeNodeStart == null) && (treeNodeFinish == null) && (((SimisTreeNode)treeNodeParent.Tag).Count > 0) && ((SimisTreeNode)treeNodeParent.Tag).All(n => n is SimisTreeNodeValue)) {
				// Empty node with all value children. This is a tree visualisation optimisation. We're a bit screwed here.
				// TODO: Work out what to do with this case (all-value children not in tree).
				return new string[0][];
			}
			try {
				var bnfState = treeNodeStart != null ? GetBnfState(treeNodeStart) : GetBnfState(treeNodeParent, true);
				var targetName = treeNodeFinish != null ? ((SimisTreeNode)treeNodeFinish.Tag).Type : "<finish>";
				return GetBnfPaths(bnfState, targetName);
			} catch (BnfStateException) {
				return new string[0][];
			}
		}

		const int BnfPathDepth = 10;

		IEnumerable<IEnumerable<string>> GetBnfPaths(BnfState bnfState, string targetName) {
			var paths = new List<Stack<string>>();
			foreach (var state in bnfState.State.NextStates) {
				paths.AddRange(GetBnfPaths(state, targetName, BnfPathDepth));
			}
			return paths.Where(p => p.Count > 0).Select(p => (IEnumerable<string>)p);
		}

		IEnumerable<Stack<string>> GetBnfPaths(FsmState fsmState, string targetName, int depth) {
			var paths = new List<Stack<string>>();
			if ((depth < BnfPathDepth) && (fsmState.ReferenceName == targetName)) {
				paths.Add(new Stack<string>());
				return paths;
			}
			if (fsmState.NextStateNames.Contains(targetName)) {
				paths.Add(new Stack<string>());
			} else if (depth > 1) {
				foreach (var state in fsmState.NextStates) {
					paths.AddRange(GetBnfPaths(state, targetName, depth - 1));
				}
			}
			if (fsmState.IsReference) {
				if (paths.Any(p => p.Count == 0)) {
					paths = new List<Stack<string>>(paths.Where(p => p.Count == 0).Take(1));
				} else {
					for (var i = 0; i < paths.Count; i++) {
						if (paths.Count(p => p.Contains(paths[i].First())) > 1) {
							paths = new List<Stack<string>>(paths.Where(p => p == paths[i] || !p.Contains(paths[i].First())));
							i = -1;
						}
					}
				}
				foreach (var path in paths) {
					path.Push(fsmState.ReferenceName);
				}
			}
			return paths;
		}
	}
}
