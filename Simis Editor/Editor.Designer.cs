namespace SimisEditor
{
	partial class Editor
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.StatusStrip statusBar;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Editor));
			this.statusBarProgress = new System.Windows.Forms.ToolStripProgressBar();
			this.statusBarLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.BottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.TopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.RightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.LeftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.ContentPanel = new System.Windows.Forms.ToolStripContentPanel();
			this.SimisProperties = new System.Windows.Forms.PropertyGrid();
			this.SimisTree = new System.Windows.Forms.TreeView();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.nodeLabelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomInToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomOutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomToWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.actualSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.homepageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.updatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.discussionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.issueTrackerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.reloadSimisResourcesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
			this.sendFeedbackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AceContainer = new System.Windows.Forms.Panel();
			this.AceImage = new System.Windows.Forms.PictureBox();
			this.AceChannels = new System.Windows.Forms.Panel();
			this.FileStatus = new System.Windows.Forms.Label();
			statusBar = new System.Windows.Forms.StatusStrip();
			statusBar.SuspendLayout();
			this.contextMenuStrip.SuspendLayout();
			this.menuStrip.SuspendLayout();
			this.AceContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.AceImage)).BeginInit();
			this.SuspendLayout();
			// 
			// statusBar
			// 
			statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusBarProgress,
            this.statusBarLabel});
			statusBar.Location = new System.Drawing.Point(0, 540);
			statusBar.Name = "statusBar";
			statusBar.Size = new System.Drawing.Size(784, 22);
			statusBar.TabIndex = 3;
			statusBar.Text = "statusStrip1";
			// 
			// statusBarProgress
			// 
			this.statusBarProgress.Name = "statusBarProgress";
			this.statusBarProgress.Size = new System.Drawing.Size(100, 16);
			this.statusBarProgress.Visible = false;
			// 
			// statusBarLabel
			// 
			this.statusBarLabel.AutoSize = false;
			this.statusBarLabel.Name = "statusBarLabel";
			this.statusBarLabel.Size = new System.Drawing.Size(769, 17);
			this.statusBarLabel.Spring = true;
			this.statusBarLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// BottomToolStripPanel
			// 
			this.BottomToolStripPanel.Location = new System.Drawing.Point(0, 0);
			this.BottomToolStripPanel.Name = "BottomToolStripPanel";
			this.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.BottomToolStripPanel.Size = new System.Drawing.Size(0, 0);
			// 
			// TopToolStripPanel
			// 
			this.TopToolStripPanel.Location = new System.Drawing.Point(0, 0);
			this.TopToolStripPanel.Name = "TopToolStripPanel";
			this.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.TopToolStripPanel.Size = new System.Drawing.Size(0, 0);
			// 
			// RightToolStripPanel
			// 
			this.RightToolStripPanel.Location = new System.Drawing.Point(0, 0);
			this.RightToolStripPanel.Name = "RightToolStripPanel";
			this.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.RightToolStripPanel.Size = new System.Drawing.Size(0, 0);
			// 
			// LeftToolStripPanel
			// 
			this.LeftToolStripPanel.Location = new System.Drawing.Point(0, 0);
			this.LeftToolStripPanel.Name = "LeftToolStripPanel";
			this.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.LeftToolStripPanel.Size = new System.Drawing.Size(0, 0);
			// 
			// ContentPanel
			// 
			this.ContentPanel.Size = new System.Drawing.Size(734, 562);
			// 
			// SimisProperties
			// 
			this.SimisProperties.Dock = System.Windows.Forms.DockStyle.Right;
			this.SimisProperties.Location = new System.Drawing.Point(482, 24);
			this.SimisProperties.Name = "SimisProperties";
			this.SimisProperties.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
			this.SimisProperties.Size = new System.Drawing.Size(302, 516);
			this.SimisProperties.TabIndex = 2;
			this.SimisProperties.ToolbarVisible = false;
			this.SimisProperties.Visible = false;
			this.SimisProperties.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.SimisProperties_PropertyValueChanged);
			// 
			// SimisTree
			// 
			this.SimisTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.SimisTree.ContextMenuStrip = this.contextMenuStrip;
			this.SimisTree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SimisTree.HideSelection = false;
			this.SimisTree.Location = new System.Drawing.Point(0, 24);
			this.SimisTree.Name = "SimisTree";
			this.SimisTree.ShowNodeToolTips = true;
			this.SimisTree.ShowRootLines = false;
			this.SimisTree.Size = new System.Drawing.Size(482, 516);
			this.SimisTree.TabIndex = 0;
			this.SimisTree.Visible = false;
			this.SimisTree.MouseUp += new System.Windows.Forms.MouseEventHandler(this.SimisTree_MouseUp);
			this.SimisTree.Enter += new System.EventHandler(this.SimisTree_Enter);
			this.SimisTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.SimisTree_AfterSelect);
			this.SimisTree.Leave += new System.EventHandler(this.SimisTree_Leave);
			// 
			// contextMenuStrip
			// 
			this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nodeLabelToolStripMenuItem});
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size(132, 26);
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
			// 
			// nodeLabelToolStripMenuItem
			// 
			this.nodeLabelToolStripMenuItem.Enabled = false;
			this.nodeLabelToolStripMenuItem.Name = "nodeLabelToolStripMenuItem";
			this.nodeLabelToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
			this.nodeLabelToolStripMenuItem.Text = "NodeLabel";
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Padding = new System.Windows.Forms.Padding(0, 0, 0, 1);
			this.menuStrip.Size = new System.Drawing.Size(784, 24);
			this.menuStrip.TabIndex = 1;
			this.menuStrip.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 23);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// newToolStripMenuItem
			// 
			this.newToolStripMenuItem.Enabled = false;
			this.newToolStripMenuItem.Name = "newToolStripMenuItem";
			this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.newToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.newToolStripMenuItem.Text = "&New";
			this.newToolStripMenuItem.ToolTipText = "Create a new Simis file.";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.openToolStripMenuItem.Text = "&Open";
			this.openToolStripMenuItem.ToolTipText = "Open an existing Simis file.";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Enabled = false;
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.ToolTipText = "Save the current Simis file.";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Enabled = false;
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.saveAsToolStripMenuItem.Text = "Save &As...";
			this.saveAsToolStripMenuItem.ToolTipText = "Save the current Simis file with a different name.";
			this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(143, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.ToolTipText = "Close Simis Editor.";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripMenuItem2,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.toolStripMenuItem3,
            this.selectAllToolStripMenuItem});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 23);
			this.editToolStripMenuItem.Text = "&Edit";
			this.editToolStripMenuItem.DropDownOpening += new System.EventHandler(this.editToolStripMenuItem_DropDownOpening);
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Enabled = false;
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.undoToolStripMenuItem.Text = "&Undo";
			this.undoToolStripMenuItem.ToolTipText = "Undo the most recent operation.";
			this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Enabled = false;
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.redoToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.redoToolStripMenuItem.Text = "&Redo";
			this.redoToolStripMenuItem.ToolTipText = "Redo the most recently undone operation.";
			this.redoToolStripMenuItem.Click += new System.EventHandler(this.redoToolStripMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(161, 6);
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Enabled = false;
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeyDisplayString = "";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.cutToolStripMenuItem.Text = "Cu&t";
			this.cutToolStripMenuItem.ToolTipText = "Cut the selection, removing it from the source.";
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripMenuItem_Click);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Enabled = false;
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.copyToolStripMenuItem.Text = "&Copy";
			this.copyToolStripMenuItem.ToolTipText = "Copy the current selection.";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Enabled = false;
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.pasteToolStripMenuItem.Text = "&Paste";
			this.pasteToolStripMenuItem.ToolTipText = "Paste the contents of the clipboard.";
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Enabled = false;
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.deleteToolStripMenuItem.Text = "De&lete";
			this.deleteToolStripMenuItem.ToolTipText = "Remove the contents of the current selection.";
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(161, 6);
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Enabled = false;
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
			this.selectAllToolStripMenuItem.Text = "Select &All";
			this.selectAllToolStripMenuItem.ToolTipText = "Select everything.";
			this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.selectAllToolStripMenuItem_Click);
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.zoomInToolStripMenuItem,
            this.zoomOutToolStripMenuItem,
            this.zoomToWindowToolStripMenuItem,
            this.actualSizeToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 23);
			this.viewToolStripMenuItem.Text = "&View";
			// 
			// zoomInToolStripMenuItem
			// 
			this.zoomInToolStripMenuItem.Enabled = false;
			this.zoomInToolStripMenuItem.Name = "zoomInToolStripMenuItem";
			this.zoomInToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.zoomInToolStripMenuItem.Text = "Zoom &In";
			this.zoomInToolStripMenuItem.ToolTipText = "Zoom in on the image.";
			// 
			// zoomOutToolStripMenuItem
			// 
			this.zoomOutToolStripMenuItem.Enabled = false;
			this.zoomOutToolStripMenuItem.Name = "zoomOutToolStripMenuItem";
			this.zoomOutToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.zoomOutToolStripMenuItem.Text = "Zoom &Out";
			this.zoomOutToolStripMenuItem.ToolTipText = "Zoom out of the image.";
			// 
			// zoomToWindowToolStripMenuItem
			// 
			this.zoomToWindowToolStripMenuItem.Enabled = false;
			this.zoomToWindowToolStripMenuItem.Name = "zoomToWindowToolStripMenuItem";
			this.zoomToWindowToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.zoomToWindowToolStripMenuItem.Text = "Zoom to &Window";
			this.zoomToWindowToolStripMenuItem.ToolTipText = "Zoom the image so it fits in the window.";
			// 
			// actualSizeToolStripMenuItem
			// 
			this.actualSizeToolStripMenuItem.Enabled = false;
			this.actualSizeToolStripMenuItem.Name = "actualSizeToolStripMenuItem";
			this.actualSizeToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.actualSizeToolStripMenuItem.Text = "&Actual Size";
			this.actualSizeToolStripMenuItem.ToolTipText = "Zoom the image to its actual size.";
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.homepageToolStripMenuItem,
            this.updatesToolStripMenuItem,
            this.discussionsToolStripMenuItem,
            this.issueTrackerToolStripMenuItem,
            this.toolStripMenuItem4,
            this.reloadSimisResourcesToolStripMenuItem,
            this.toolStripMenuItem5,
            this.sendFeedbackToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 23);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// homepageToolStripMenuItem
			// 
			this.homepageToolStripMenuItem.Name = "homepageToolStripMenuItem";
			this.homepageToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.homepageToolStripMenuItem.Text = "&Homepage";
			this.homepageToolStripMenuItem.Click += new System.EventHandler(this.homepageToolStripMenuItem_Click);
			// 
			// updatesToolStripMenuItem
			// 
			this.updatesToolStripMenuItem.Name = "updatesToolStripMenuItem";
			this.updatesToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.updatesToolStripMenuItem.Text = "&Updates";
			this.updatesToolStripMenuItem.Click += new System.EventHandler(this.updatesToolStripMenuItem_Click);
			// 
			// discussionsToolStripMenuItem
			// 
			this.discussionsToolStripMenuItem.Name = "discussionsToolStripMenuItem";
			this.discussionsToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.discussionsToolStripMenuItem.Text = "&Discussions";
			this.discussionsToolStripMenuItem.Click += new System.EventHandler(this.discussionsToolStripMenuItem_Click);
			// 
			// issueTrackerToolStripMenuItem
			// 
			this.issueTrackerToolStripMenuItem.Name = "issueTrackerToolStripMenuItem";
			this.issueTrackerToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.issueTrackerToolStripMenuItem.Text = "&Issue Tracker";
			this.issueTrackerToolStripMenuItem.Click += new System.EventHandler(this.issueTrackerToolStripMenuItem_Click);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(194, 6);
			// 
			// reloadSimisResourcesToolStripMenuItem
			// 
			this.reloadSimisResourcesToolStripMenuItem.Name = "reloadSimisResourcesToolStripMenuItem";
			this.reloadSimisResourcesToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.reloadSimisResourcesToolStripMenuItem.Text = "Reload Simis Resources";
			this.reloadSimisResourcesToolStripMenuItem.Click += new System.EventHandler(this.reloadSimisResourcesToolStripMenuItem_Click);
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size(194, 6);
			// 
			// sendFeedbackToolStripMenuItem
			// 
			this.sendFeedbackToolStripMenuItem.Name = "sendFeedbackToolStripMenuItem";
			this.sendFeedbackToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
			this.sendFeedbackToolStripMenuItem.Text = "Send Feedback...";
			this.sendFeedbackToolStripMenuItem.Click += new System.EventHandler(this.sendFeedbackToolStripMenuItem_Click);
			// 
			// AceContainer
			// 
			this.AceContainer.AutoScroll = true;
			this.AceContainer.AutoScrollMargin = new System.Drawing.Size(3, 3);
			this.AceContainer.AutoSize = true;
			this.AceContainer.Controls.Add(this.AceImage);
			this.AceContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.AceContainer.Location = new System.Drawing.Point(0, 24);
			this.AceContainer.Name = "AceContainer";
			this.AceContainer.Size = new System.Drawing.Size(282, 516);
			this.AceContainer.TabIndex = 4;
			this.AceContainer.TabStop = true;
			this.AceContainer.Visible = false;
			// 
			// AceImage
			// 
			this.AceImage.BackColor = System.Drawing.Color.White;
			this.AceImage.Location = new System.Drawing.Point(3, 3);
			this.AceImage.Name = "AceImage";
			this.AceImage.Size = new System.Drawing.Size(100, 100);
			this.AceImage.TabIndex = 0;
			this.AceImage.TabStop = false;
			// 
			// AceChannels
			// 
			this.AceChannels.AutoScroll = true;
			this.AceChannels.AutoScrollMargin = new System.Drawing.Size(3, 3);
			this.AceChannels.Dock = System.Windows.Forms.DockStyle.Right;
			this.AceChannels.Location = new System.Drawing.Point(282, 24);
			this.AceChannels.Name = "AceChannels";
			this.AceChannels.Size = new System.Drawing.Size(200, 516);
			this.AceChannels.TabIndex = 5;
			this.AceChannels.Visible = false;
			// 
			// FileStatus
			// 
			this.FileStatus.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FileStatus.Location = new System.Drawing.Point(0, 24);
			this.FileStatus.Name = "FileStatus";
			this.FileStatus.Size = new System.Drawing.Size(784, 516);
			this.FileStatus.TabIndex = 1;
			this.FileStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// Editor
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(784, 562);
			this.Controls.Add(this.AceContainer);
			this.Controls.Add(this.AceChannels);
			this.Controls.Add(this.SimisTree);
			this.Controls.Add(this.SimisProperties);
			this.Controls.Add(this.FileStatus);
			this.Controls.Add(statusBar);
			this.Controls.Add(this.menuStrip);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip;
			this.Name = "Editor";
			this.Text = "Simis Editor";
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Editor_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Editor_DragEnter);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Editor_FormClosing);
			statusBar.ResumeLayout(false);
			statusBar.PerformLayout();
			this.contextMenuStrip.ResumeLayout(false);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.AceContainer.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.AceImage)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.PropertyGrid SimisProperties;
		private System.Windows.Forms.TreeView SimisTree;
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem homepageToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem updatesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem discussionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem issueTrackerToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem reloadSimisResourcesToolStripMenuItem;
		private System.Windows.Forms.ToolStripProgressBar statusBarProgress;
		private System.Windows.Forms.ToolStripStatusLabel statusBarLabel;
		private System.Windows.Forms.ToolStripPanel BottomToolStripPanel;
		private System.Windows.Forms.ToolStripPanel TopToolStripPanel;
		private System.Windows.Forms.ToolStripPanel RightToolStripPanel;
		private System.Windows.Forms.ToolStripPanel LeftToolStripPanel;
		private System.Windows.Forms.ToolStripContentPanel ContentPanel;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem nodeLabelToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
		private System.Windows.Forms.ToolStripMenuItem sendFeedbackToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoomInToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoomOutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem zoomToWindowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem actualSizeToolStripMenuItem;
		private System.Windows.Forms.Panel AceContainer;
		private System.Windows.Forms.PictureBox AceImage;
		private System.Windows.Forms.Panel AceChannels;
		private System.Windows.Forms.Label FileStatus;
	}
}

