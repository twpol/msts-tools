//------------------------------------------------------------------------------
// Simis Editor, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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

		#region Fields

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
		UndoRedoSimisFile File;
		SimisTreeNode SavedFileTree;
		TreeNode SelectedNode;
		TreeNode ContextNode;
		SimisProvider SimisProvider;
		Thread SimisProviderThread;
		SimisAceImageType AceType = SimisAceImageType.ColorAndAlpha;
		int AceZoom = 0;

		#endregion

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
			UpdateTitle();
			UpdateMenu();
			UpdateViewer();
			UpdateStatusbar();
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
			UpdateStatusbar();
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
			SimisProviderThread = new Thread(() => {
				try {
					SimisProvider = new SimisProvider(Path.GetDirectoryName(Application.ExecutablePath) + @"\Resources");
				} catch (FileException ex) {
					this.Invoke((MethodInvoker)(() => {
						new Feedback(ex, "loading Simis Resource '" + Path.GetFileName(ex.FileName) + "'").PromptAndSend(this);
					}));
				}
			});
			SimisProviderThread.Start();
		}

		bool WaitForSimisProvider() {
			SimisProviderThread.Join();
			return true;
		}

		bool UpdateFromSimisProvider() {
			if (SimisProvider == null) return false;

			var generalName = "Train Simulator files";

			var simisFormats = new List<List<string>>();
			simisFormats.Add(new List<string>(new[] { "All " + generalName + " (*.*)" }));
			foreach (var format in SimisProvider.Formats) {
				var mask = format.Extension.Contains(".") ? format.Extension : "*." + format.Extension;
				simisFormats[0].Add(mask);
				simisFormats.Add(new List<string>(new[] { format.Name + " files", mask }));
			}
			simisFormats.Add(new List<string>(new[] { "All files", "*.*" }));
			openFileDialog.Filter = String.Join("|", simisFormats.Select(l => l[0] + "|" + String.Join(";", l.ToArray(), 1, l.Count - 1)).ToArray());

			var streamFormats = new[] { "Text", "Binary", "Compressed Binary" };
			saveFileDialog.Filter = String.Join("|", streamFormats.Select(s => s + " " + generalName + " (*.*)|" + String.Join(";", simisFormats[0].ToArray(), 1, simisFormats[0].Count - 1)).ToArray());

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
			File = new UndoRedoSimisFile("", SimisProvider);
			SavedFileTree = File.Tree;
			SelectNode(null);
			ResyncSimisNodes();
			UpdateTitle();
			UpdateMenu();
			UpdateViewer();
			UpdateStatusbar();
		}

		bool OpenFile(string filename) {
			if (!WaitForSimisProvider()) return false;
			var newFile = new UndoRedoSimisFile(filename, SimisProvider.GetForPath(filename));
			try {
				newFile.Read();
			} catch (FileException ex) {
				new Feedback(ex, "loading '" + Path.GetFileName(ex.FileName) + "'").PromptAndSend(this);
				return false;
			}

			// Put UI in to loading state.
			Filename = "";
			File = null;
			SelectNode(null);
			UpdateTitle();
			UpdateMenu();
			UpdateViewer(true);
			UpdateImage();
			UpdateImageSidebar();
			UpdateStatusbar();
			Refresh();

			// Wipe out anything we had left.
			SimisTree.Nodes.Clear();

			// Set up for new file.
			Filename = filename;
			File = newFile;
			if (File.Tree != null) {
				SavedFileTree = File.Tree;
				ResyncSimisNodes();
				SimisTree.ExpandAll();
				if (SimisTree.Nodes.Count > 0) {
					SimisTree.TopNode = SimisTree.Nodes[0];
					SimisTree.SelectedNode = SimisTree.Nodes[0];
				}
			} else if (File.Ace != null) {
				SelectAceType(File.Ace.HasAlpha ? SimisAceImageType.ColorAndAlpha : File.Ace.HasMask ? SimisAceImageType.ColorAndMask : SimisAceImageType.ColorOnly);
				SelectAceZoomToWindow();
			}
			UpdateTitle();
			UpdateMenu();
			UpdateViewer();
			UpdateImage();
			UpdateImageSidebar();
			UpdateStatusbar();
			return true;
		}

		bool SaveFile() {
			if (File.FileName != Filename) {
				File.FileName = Filename;
			}
			try {
				File.Write();
			} catch (FileException ex) {
				if (ex.InnerException is UnauthorizedAccessException) {
					TaskDialog.Show(this, TaskDialogCommonIcon.Error, "Unable to save '" + Path.GetFileName(ex.FileName) + "'", ex.InnerException.Message);
				} else {
					TaskDialog.Show(this, TaskDialogCommonIcon.Error, "Unable to save '" + Path.GetFileName(ex.FileName) + "'", ex.ToString());
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
				switch (TaskDialog.ShowYesNoCancel(this, 0, "Do you want to save changes to '" + FilenameTitle + "'?", "", "Save", "Don't Save", "Cancel")) {
					case DialogResult.Yes:
						if (!SaveFile()) {
							return false;
						}
						break;
					case DialogResult.Cancel:
						return false;
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
			saveFileDialog.FilterIndex = File.StreamIsCompressed ? 3 : File.JinxStreamIsBinary ? 2 : 1;
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK) {
				Filename = saveFileDialog.FileName;
				File.StreamIsBinary = saveFileDialog.FilterIndex >= 2;
				File.StreamIsCompressed = saveFileDialog.FilterIndex == 3;
				File.JinxStreamIsBinary = saveFileDialog.FilterIndex >= 2;
				SaveFile();
			}
		}

		void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			Close();
		}

		#endregion

		#region Menu Events - Edit

		void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e) {
			UpdateEditState();
		}

		void undoToolStripMenuItem_Click(object sender, EventArgs e) {
			File.Undo();
			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
			UpdateTitle();
			UpdateMenu();
			UpdateStatusbar();
		}

		void redoToolStripMenuItem_Click(object sender, EventArgs e) {
			File.Redo();
			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
			UpdateTitle();
			UpdateMenu();
			UpdateStatusbar();
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

		#region Menu Events - View

		void zoomInToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceZoom(AceZoom + 1);
		}

		void zoomOutToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceZoom(AceZoom - 1);
		}

		void zoomToWindowToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceZoomToWindow();
		}

		void actualSizeToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceZoom(0);
		}

		void colorOnlyToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceType(SimisAceImageType.ColorOnly);
		}

		void alphaOnlyToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceType(SimisAceImageType.AlphaOnly);
		}

		void maskOnlyToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceType(SimisAceImageType.MaskOnly);
		}

		void colorAndAlphaToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceType(SimisAceImageType.ColorAndAlpha);
		}

		void colorAndMaskToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectAceType(SimisAceImageType.ColorAndMask);
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

		void sendFeedbackToolStripMenuItem_Click(object sender, EventArgs e) {
			new Feedback().PromptAndSend(this);
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
			UpdateStatusbar();
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
				foreach (var path in pathsBefore.Select(p => String.Join(" ", p.ToArray())).OrderBy(s => s)) {
					addMenuItem("Insert " + path.Replace(" ( ", "(").Replace(" )", ")") + " before " + target.Type, "[insertbefore]" + path);
				}
				needSeparator = true;
			}
			if (pathsChildBefore.Any(_ => true)) {
				if (needSeparator) addMenuSeparator();
				foreach (var path in pathsChildBefore.Select(p => String.Join(" ", p.ToArray())).OrderBy(s => s)) {
					addMenuItem("Prepend " + path.Replace(" ( ", "(").Replace(" )", ")") + " to " + target.Type, "[childbefore]" + path);
				}
				needSeparator = true;
			}
			if (pathsChildAfter.Any(_ => true)) {
				if (needSeparator) addMenuSeparator();
				foreach (var path in pathsChildAfter.Select(p => String.Join(" ", p.ToArray())).OrderBy(s => s)) {
					addMenuItem("Append " + path.Replace(" ( ", "(").Replace(" )", ")") + " to " + target.Type, "[childafter]" + path);
				}
				needSeparator = true;
			}
			if (pathsAfter.Any(_ => true)) {
				if (needSeparator) addMenuSeparator();
				foreach (var path in pathsAfter.Select(p => String.Join(" ", p.ToArray())).OrderBy(s => s)) {
					addMenuItem("Insert " + path.Replace(" ( ", "(").Replace(" )", ")") + " after " + target.Type, "[insertafter]" + path);
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

			var blockPath = new Stack<SimisTreeNode>();
			{
				var node = ContextNode;
				while (node != null) {
					blockPath.Push((SimisTreeNode)node.Tag);
					node = node.Parent;
				}
			}
			var blockPathList = new List<SimisTreeNode>(blockPath);
			var simisTreeNode = (SimisTreeNode)ContextNode.Tag;
			var newNodeStack = new Stack<List<SimisTreeNode>>();
			newNodeStack.Push(new List<SimisTreeNode>());
			foreach (var newItem in newItems.Split(' ')) {
				switch (newItem) {
					case "(":
						newNodeStack.Push(new List<SimisTreeNode>());
						break;
					case ")":
						var nodes = newNodeStack.Pop();
						var index = newNodeStack.Peek().Count - 1;
						foreach (var node in nodes) {
							newNodeStack.Peek()[index] = newNodeStack.Peek()[index].AppendChild(node);
						}
						break;
					default:
						if (SimisTreeNodeValue.NodeTypes.ContainsKey(newItem)) {
							newNodeStack.Peek().Add((SimisTreeNode)SimisTreeNodeValue.NodeTypes[newItem].GetConstructor(new Type[] { typeof(string), typeof(string) }).Invoke(new object[] { newItem, "" }));
						} else {
							newNodeStack.Peek().Add(new SimisTreeNode(newItem, ""));
						}
						break;
				}
			}
			var newNodes = newNodeStack.Peek();

			switch (action) {
				case "insertbefore":
				case "insertafter":
					if (action == "insertafter") {
						blockPathList[blockPathList.Count - 1] = blockPathList[blockPathList.Count - 2].GetNextSibling(blockPathList[blockPathList.Count - 1]);
					}
					foreach (var newNode in newNodes) {
						File.Tree = File.Tree.GetPathList(blockPathList).Insert(newNode);
					}
					break;
				case "childbefore":
					var targetNode = blockPathList.Last().GetFirstChild();
					foreach (var newNode in newNodes) {
						File.Tree = File.Tree.GetPathList(blockPathList).AddPath(targetNode).Insert(newNode);
					}
					break;
				case "childafter":
					foreach (var newNode in newNodes) {
						File.Tree = File.Tree.GetPathList(blockPathList).Append(newNode);
					}
					break;
			}

			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
			UpdateTitle();
			UpdateMenu();
			UpdateStatusbar();
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
				File.Tree = File.Tree.GetPathList(blockPathList).AddPath(child).Set(new SimisTreeNodeValueString(child.Type, child.Name, (string)value));
			} else if (child is SimisTreeNodeValueIntegerUnsigned) {
				File.Tree = File.Tree.GetPathList(blockPathList).AddPath(child).Set(new SimisTreeNodeValueIntegerUnsigned(child.Type, child.Name, (uint)value));
			} else if (child is SimisTreeNodeValueIntegerSigned) {
				File.Tree = File.Tree.GetPathList(blockPathList).AddPath(child).Set(new SimisTreeNodeValueIntegerSigned(child.Type, child.Name, (int)value));
			} else if (child is SimisTreeNodeValueIntegerDWord) {
				File.Tree = File.Tree.GetPathList(blockPathList).AddPath(child).Set(new SimisTreeNodeValueIntegerDWord(child.Type, child.Name, (uint)value));
			} else if (child is SimisTreeNodeValueIntegerWord) {
				File.Tree = File.Tree.GetPathList(blockPathList).AddPath(child).Set(new SimisTreeNodeValueIntegerWord(child.Type, child.Name, (ushort)value));
			} else if (child is SimisTreeNodeValueIntegerByte) {
				File.Tree = File.Tree.GetPathList(blockPathList).AddPath(child).Set(new SimisTreeNodeValueIntegerByte(child.Type, child.Name, (byte)value));
			} else if (child is SimisTreeNodeValueFloat) {
				File.Tree = File.Tree.GetPathList(blockPathList).AddPath(child).Set(new SimisTreeNodeValueFloat(child.Type, child.Name, (float)value));
			}

			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
			UpdateTitle();
			UpdateMenu();
			UpdateStatusbar();
		}

		void AceImage_Paint(object sender, PaintEventArgs e) {
			if (AceImage.Tag != null) {
				var image = (Image)AceImage.Tag;
				e.Graphics.InterpolationMode = AceImage.Width >= image.Width ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
				e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
				e.Graphics.DrawImage(image, AceImage.ClientRectangle);
			}
		}

		#endregion

		#region Functionality - UI updaters

		void UpdateTitle() {
			if (File == null) {
				Text = ApplicationSettings.ApplicationTitle;
			} else {
				Text = FilenameTitle + (File.Tree != SavedFileTree ? "*" : "") + " - " + ApplicationSettings.ApplicationTitle;
			}
		}

		void UpdateMenu() {
			saveToolStripMenuItem.Enabled = File != null;
			saveAsToolStripMenuItem.Enabled = File != null;
		}

		void UpdateEditState() {
			cutToolStripMenuItem.Enabled = EditState.CanCut;
			copyToolStripMenuItem.Enabled = EditState.CanCopy;
			pasteToolStripMenuItem.Enabled = EditState.CanPaste;
			deleteToolStripMenuItem.Enabled = EditState.CanDelete;
			selectAllToolStripMenuItem.Enabled = EditState.CanSelectAll;
		}

		void UpdateViewer() {
			UpdateViewer(false);
		}

		void UpdateViewer(bool loading) {
			if (loading) {
				FileStatus.Text = "Loading...";
			} else {
				FileStatus.Text = "No file loaded.";
			}
			SimisTree.Visible = File != null && File.Tree != null && !String.IsNullOrEmpty(Filename);
			SimisProperties.Visible = File != null && File.Tree != null && !String.IsNullOrEmpty(Filename);
			AceContainer.Visible = File != null && File.Ace != null && !String.IsNullOrEmpty(Filename);
			AceChannels.Visible = File != null && File.Ace != null && !String.IsNullOrEmpty(Filename);

			zoomInToolStripMenuItem.Enabled = AceContainer.Visible;
			zoomOutToolStripMenuItem.Enabled = AceContainer.Visible;
			zoomToWindowToolStripMenuItem.Enabled = AceContainer.Visible;
			actualSizeToolStripMenuItem.Enabled = AceContainer.Visible;

			colorOnlyToolStripMenuItem.Enabled = AceContainer.Visible;
			alphaOnlyToolStripMenuItem.Enabled = AceContainer.Visible && File.Ace.HasAlpha;
			maskOnlyToolStripMenuItem.Enabled = AceContainer.Visible && File.Ace.HasMask;
			colorAndAlphaToolStripMenuItem.Enabled = AceContainer.Visible && File.Ace.HasAlpha;
			colorAndMaskToolStripMenuItem.Enabled = AceContainer.Visible && File.Ace.HasMask;

			colorOnlyToolStripMenuItem.Checked = AceType == SimisAceImageType.ColorOnly;
			alphaOnlyToolStripMenuItem.Checked = AceType == SimisAceImageType.AlphaOnly;
			maskOnlyToolStripMenuItem.Checked = AceType == SimisAceImageType.MaskOnly;
			colorAndAlphaToolStripMenuItem.Checked = AceType == SimisAceImageType.ColorAndAlpha;
			colorAndMaskToolStripMenuItem.Checked = AceType == SimisAceImageType.ColorAndMask;
		}

		void UpdateImage() {
			if ((File == null) || (File.Ace == null)) return;

			var zoom = Math.Pow(2, AceZoom);
			var image = File.Ace.Image[0].GetImage(AceType);
			AceImage.Tag = image;
			AceImage.Width = (int)(image.Width * zoom);
			AceImage.Height = (int)(image.Height * zoom);
			AceImage.Refresh();
		}

		void UpdateImageSidebar() {
			AceChannels.Controls.Clear();
			if ((File == null) || (File.Ace == null)) return;

			var padding = 3;
			var horizontalSpace = AceChannels.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2 * padding;
			var verticalPosition = padding;
			foreach (var image in File.Ace.Image) {
				foreach (var picture in new[] { image.ImageColor, image.ImageMask }) {
					if ((picture == image.ImageMask) && !File.Ace.HasMask) continue;

					var label = new Label();
					label.Text = String.Format("{0}x{1} {2}:", picture.Width, picture.Height, picture == image.ImageColor ? "color" : "mask");
					label.AutoSize = true;
					label.Left = padding;
					label.Top = verticalPosition;
					AceChannels.Controls.Add(label);
					verticalPosition += label.Height;

					var pictureBox = new PictureBox();
					pictureBox.Image = picture;
					pictureBox.BackColor = Color.White;
					pictureBox.BackgroundImage = AceImage.BackgroundImage;
					pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
					pictureBox.Width = Math.Min(picture.Width, AceChannels.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6);
					pictureBox.Height = picture.Height * pictureBox.Width / picture.Width;
					pictureBox.Left = padding + (horizontalSpace - pictureBox.Width) / 2;
					pictureBox.Top = verticalPosition;
					AceChannels.Controls.Add(pictureBox);
					verticalPosition += pictureBox.Height + padding;
				}
			}
		}

		void UpdateStatusbar() {
			if (File == null) {
				statusBarLabel.Text = "";
			} else if (File.Ace != null) {
				statusBarLabel.Text = String.Format("[ACE Image {2}x{3}] {1}%, {0}", new[] { "Color Only", "Alpha Only", "Mask Only", "Color and Alpha", "Color and Mask" }[(int)AceType], 100 * Math.Pow(2, AceZoom), File.Ace.Width, File.Ace.Height);
			} else if (SelectedNode == null) {
				statusBarLabel.Text = String.Format("[{0}]", File.JinxStreamFormat.Name);
			} else {
				statusBarLabel.Text = String.Format("[{0}] {1}", File.JinxStreamFormat.Name, GetBnfLocation(File, SelectedNode));
			}
		}

		#endregion

		#region Functionality - Simis Jinx

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
				SimisProperties.SelectedObject = null;
			} else {
				SelectedNode = treeNode;
				SimisProperties.SelectedObject = CreateEditObjectFor((SimisTreeNode)SelectedNode.Tag);
			}
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
			dUnit.ReferencedAssemblies.Add(Path.GetDirectoryName(Application.ExecutablePath) + @"\JGR.IO.Parser.dll");
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
				using (var codeStream = new MemoryStream()) {
					using (var codeStreamWriter = new StreamWriter(codeStream, Encoding.Unicode)) {
						compiler.GenerateCodeFromCompileUnit(dUnit, codeStreamWriter, new CodeGeneratorOptions());
					}

					var messages = new string[compileResults.Output.Count];
					compileResults.Output.CopyTo(messages, 0);

					new Feedback(FeedbackType.ApplicationFailure, "creating the edit object", new Dictionary<string, string> {
							{ "", String.Join("\n", messages) },
							{ "CreateEditObjectFor.cs", Encoding.Unicode.GetString(codeStream.ToArray()) },
						}).PromptAndSend(this);
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

		string GetBnfLocation(UndoRedoSimisFile file, TreeNode treeNode) {
			var nodes = new Stack<KeyValuePair<SimisTreeNode, bool>>();
			while (treeNode != null) {
				nodes.Push(new KeyValuePair<SimisTreeNode, bool>((SimisTreeNode)treeNode.Tag, true));
				treeNode = treeNode.Parent;
			}
			var str = String.Join("", nodes.Select(n => n.Key.Type + (n.Value ? " > " : " + ")).ToArray());
			return str.Substring(0, str.Length - 3);
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
			var bnfState = new BnfState(File.JinxStreamFormat.Bnf);
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
				paths.AddRange(GetBnfPaths(bnfState.Bnf, state, targetName, BnfPathDepth));
			}
			// Remove zero-step paths, as they go direct from the start to finish without adding any new states - and
			// we WANT to add states.
			return paths.Where(p => p.Count > 0).Select(p => (IEnumerable<string>)p);
		}

		IEnumerable<Stack<string>> GetBnfPaths(Bnf bnf, FsmState fsmState, string targetName, int depth) {
			var paths = new List<Stack<string>>();
			if ((depth < BnfPathDepth) && (fsmState.ReferenceName == targetName)) {
				// We're right at the target, return a single zero-step path.
				paths.Add(new Stack<string>());
				return paths;
			}
			if (fsmState.NextStateNames.Contains(targetName)) {
				// We know that the target is one of the next states, so pretend we called ourself and we returned a
				// single zero-step path (i.e. the codepath just above).
				paths.Add(new Stack<string>());
			} else if (depth > 1) {
				// Target isn't next and we've got depth quota, just go recursive.
				foreach (var state in fsmState.NextStates) {
					paths.AddRange(GetBnfPaths(bnf, state, targetName, depth - 1));
				}
			}
			// If we're not a reference node, we don't exist in the BNF.
			if (fsmState.IsReference) {
				if (paths.Any(p => p.Count == 0)) {
					// The target was found in the next state, so wipe out all other paths.
					paths = new List<Stack<string>> { new Stack<string>() };
				} else {
					// Path optimisation; for each path's first state, remove any other paths that go through that
					// state.
					for (var i = 0; i < paths.Count; i++) {
						if (paths.Count(p => p.Contains(paths[i].First())) > 1) {
							paths = new List<Stack<string>>(paths.Where(p => p == paths[i] || !p.Contains(paths[i].First())));
							i = -1;
						}
					}
				}
				if (bnf.Productions.ContainsKey(fsmState.ReferenceName)) {
					// We're a "block" of some kind, so find out the possible nested paths and insert them.
					var newPaths = new List<Stack<string>>();
					foreach (var nestedPath in GetBnfPaths(new BnfState(bnf, fsmState.ReferenceName), "<finish>")) {
						foreach (var path in paths) {
							newPaths.Add(nestedPath.Prefix("(").Concat(")").Concat(path).AsStack());
						}
					}
					paths = newPaths;
				}
				// All paths go through us.
				paths.ForEach(path => path.Push(fsmState.ReferenceName));
			}
			return paths;
		}

		#endregion

		#region Functionality - Simis Ace

		void SelectAceType(SimisAceImageType type) {
			AceType = type;
			UpdateViewer();
			UpdateImage();
			UpdateStatusbar();
		}

		void SelectAceZoom(int zoom) {
			AceZoom = zoom;
			UpdateImage();
		}

		void SelectAceZoomToWindow() {
			var image = (Image)AceImage.Tag;
			var space = AceContainer.ClientRectangle;
			space.Inflate(-3 - SystemInformation.VerticalScrollBarWidth, -3 - SystemInformation.HorizontalScrollBarHeight);

			var zoomX = Math.Log((double)space.Width / image.Width) / Math.Log(2);
			var zoomY = Math.Log((double)space.Height / image.Height) / Math.Log(2);
			SelectAceZoom((int)Math.Floor(Math.Min(zoomX, zoomY)));
		}

		#endregion
	}
}
