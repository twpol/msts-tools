//------------------------------------------------------------------------------
// Simis Editor, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Jgr;
using Jgr.Gui;
using Jgr.IO.Parser;
using Microsoft.CSharp;
using SimisEditor.Properties;

namespace SimisEditor
{
	public partial class Editor : Form
	{
		static TraceSwitch TraceSwitch = new TraceSwitch("editor", "Trace Editor");

		bool Modified = true;
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
		TreeNode SelectedNode;
		SimisProvider SimisProvider;

		#region Initialization

		public Editor() {
			InitializeComponent();
			ToolStripManager.Renderer = new ToolStripNativeRenderer();
			InitializeSimisProvider();
			InitializeNewVersionCheck();
			InitializeFromCommandLine();
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
						MessageBox.Show(ex.ToString(), "Load Resources - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
				simisFormats[0].Add("*." + format.Extension);
				simisFormats.Add(new List<string>(new string[] { format.Name + " files", "*." + format.Extension }));
			}
			simisFormats.Add(new List<string>(new string[] { "All files", "*.*" }));
			openFileDialog.Filter = String.Join("|", simisFormats.Select<List<string>, string>(l => l[0] + "|" + String.Join(";", l.ToArray(), 1, l.Count - 1)).ToArray());

			var streamFormats = new string[] { "Text", "Binary", "Compressed Binary" };
			saveFileDialog.Filter = String.Join("|", streamFormats.Select<string, string>(s => s + " " + generalName + "|" + String.Join(";", simisFormats[0].ToArray(), 1, simisFormats[0].Count - 1)).ToArray());

			return true;
		}

		void InitializeFromCommandLine() {
			this.Shown += (o, e) => {
				NewFile();
				foreach (var argument in Environment.GetCommandLineArgs().Where<string>((s, i) => i > 0)) {
					if (argument.StartsWith("/") || argument.StartsWith("-")) continue;
					OpenFile(argument);
					break;
				}
			};
		}

		#endregion

		#region File Operations (New, Open, Modified, Save)

		void NewFile() {
			//WaitForSimisProvider();
			Modified = false;
			Filename = "";
			File = new SimisFile("", SimisProvider);
			SelectNode(null);
			ResyncSimisNodes();
			UpdateTitle();
			UpdateMenu();
		}

		void OpenFile(string filename) {
			if (!WaitForSimisProvider()) return;
			var newFile = new SimisFile(filename, SimisProvider);
			try {
				newFile.ReadFile();
			} catch (FileException ex) {
				using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
					MessageBox.Show(ex.ToString(), "Open File - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				return;
			}

			Modified = false;
			Filename = filename;
			File = newFile;
			SelectNode(null);
			ResyncSimisNodes();
			SimisTree.ExpandAll();
			if (SimisTree.Nodes.Count > 0) {
				SimisTree.TopNode = SimisTree.Nodes[0];
			}
			UpdateTitle();
			UpdateMenu();
		}

		void FileModified() {
			Modified = true;
			UpdateTitle();
			UpdateMenu();
		}

		void SaveFile() {
			if (File.FileName != Filename) {
				File.FileName = Filename;
			}
			try {
				File.WriteFile();
			} catch (FileException ex) {
				using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
					if (ex.InnerException is UnauthorizedAccessException) {
						MessageBox.Show(ex.InnerException.Message, "Save File - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					} else {
						MessageBox.Show(ex.ToString(), "Save File - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				return;
			}

			Modified = false;
			UpdateTitle();
			UpdateMenu();
		}

		bool SaveFileIfModified() {
			if (Modified) {
				using (new AutoCenterWindows(this, AutoCenterWindowsMode.FirstWindowOnly)) {
					switch (MessageBox.Show("Do you want to save changes to '" + FilenameTitle + "'?", Application.ProductName, MessageBoxButtons.YesNoCancel)) {
						case DialogResult.Yes:
							SaveFile();
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
			if (!UpdateFromSimisProvider()) return;
			if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
				OpenFile(openFileDialog.FileName);
			}
		}

		void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFile();
		}

		void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
			// FilterIndex is 1-based, SIGH. Filters: 1=Text, 2=Binary, 3=Compressed Binary.
			if (!UpdateFromSimisProvider()) return;
			saveFileDialog.FilterIndex = File.StreamCompressed ? 3 : File.StreamFormat == SimisStreamFormat.Text ? 1 : 2;
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK) {
				Filename = saveFileDialog.FileName;
				File.StreamFormat = saveFileDialog.FilterIndex == 1 ? SimisStreamFormat.Text : SimisStreamFormat.Binary;
				File.StreamCompressed = saveFileDialog.FilterIndex == 3;
				SaveFile();
			}
		}

		void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!SaveFileIfModified()) {
				return;
			}
			Close();
		}

		#endregion

		#region Menu Events - Edit

		private void undoToolStripMenuItem_Click(object sender, EventArgs e) {
			File.Undo();
			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e) {
			File.Redo();
			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
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

		void Editor_DragEnter(object sender, DragEventArgs e) {
			e.Effect = DragDropEffects.None;
			if (!e.Data.GetDataPresent(DataFormats.FileDrop)) {
				return;
			}
			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length != 1) {
				return;
			}
			if (!WaitForSimisProvider()) return;
			if (!SimisProvider.Formats.Any<SimisFormat>((f) => files[0].EndsWith("." + f.Extension))) {
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

        void SimisTree_AfterSelect(object sender, TreeViewEventArgs e) {
            SelectNode(e.Node);
        }

		void SimisProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
			if (SelectedNode == null) return;

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
			if (child is SimisTreeNodeValueInteger) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueInteger(child.Type, child.Name, (long)value), child));
			} else if (child is SimisTreeNodeValueFloat) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueFloat(child.Type, child.Name, (float)value), child));
			} else if (child is SimisTreeNodeValueString) {
				File.Tree = File.Tree.Apply(blockPathList, n => n.ReplaceChild(new SimisTreeNodeValueString(child.Type, child.Name, (string)value), child));
			}

			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
		}

		#endregion

		void UpdateTitle() {
			Text = FilenameTitle + (Modified ? "*" : "") + " - " + Application.ProductName;
		}

		void UpdateMenu() {
			saveToolStripMenuItem.Enabled = Filename.Length > 0;
			saveAsToolStripMenuItem.Enabled = Filename.Length > 0;
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

			if (simisTreeNodes.All<SimisTreeNode>(n => n is SimisTreeNodeValue)) {
				simisTreeNodes = new SimisTreeNode[] { };
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

		void ResyncSimisNodes(TreeNode viewTreeNode, SimisTreeNode simisTreeNode) {
			viewTreeNode.Text = GetNodeText(simisTreeNode);
			viewTreeNode.Nodes.Clear();
			if (simisTreeNode.Any<SimisTreeNode>(b => !(b is SimisTreeNodeValue))) {
				ResyncSimisNodes(viewTreeNode.Nodes, simisTreeNode);
			}
		}

        void SelectNode(TreeNode treeNode) {
			if ((treeNode == null) || (treeNode.Tag == null)) {
				SelectedNode = null;
				SimisProperties.SelectedObject = null;
				return;
			}
			SelectedNode = treeNode;
			SimisProperties.SelectedObject = CreateEditObjectFor((SimisTreeNode)SelectedNode.Tag);
        }

		static string BlockToNameString(SimisTreeNode block) {
			if (block.Name.Length > 0) return block.Type + " \"" + block.Name + "\"";
			return block.Type;
		}

		static string BlockToNameOnlyString(SimisTreeNode block) {
			if (block.Name.Length > 0) return block.Name;
			return block.Type;
		}

		static string BlockToValueString(SimisTreeNodeValue block) {
			if (block is SimisTreeNodeValueString) {
				return "\"" + block.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
			}
			return block.ToString();
		}

		string GetNodeText(SimisTreeNode block) {
			// Non-trivial node, don't bother trying.
			if (block.Count<SimisTreeNode>(b => !(b is SimisTreeNodeValue)) > 0) {
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
			if (block is SimisTreeNodeValue) {
				return BlockToNameOnlyString(block) + "=" + BlockToValueString((SimisTreeNodeValue)block);
			}

			return BlockToNameString(block);
		}

		object CreateEditObjectFor(SimisTreeNode block) {
			if (block.Any<SimisTreeNode>(b => !(b is SimisTreeNodeValue))) return null;

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
				if (block.Count<SimisTreeNode>(b => (b.Name.Length > 0 ? b.Name : b.Type) == dPropertyName) > 1) {
					var index = 1;
					while (dBlockNames.Contains(dPropertyName + (index == 0 ? "" : "_" + index))) index++;
					dPropertyName += "_" + index;
					dBlockNames.Add(dPropertyName);
				}
				if (block.Count == 1) {
					dPropertyName = block.Type;
				}

				if (node is SimisTreeNodeValue) {
					// Store the SimisBlockValue in a way we can find later.
					var dSimisProperty = CreateEditObjectFor_AddProperty(dFields, dProperties, dPropertyName + "_SimisTreeNodeValue", new CodeTypeReference(typeof(SimisTreeNodeValue)));
					dSimisProperty.CustomAttributes.Add(new CodeAttributeDeclaration("BrowsableAttribute", new CodeAttributeArgument(new CodePrimitiveExpression(false))));
					dPropertyValues.Add(dSimisProperty.Name, (SimisTreeNodeValue)node);
				}

				CodeTypeReference type;
				if (node is SimisTreeNodeValueInteger) {
					type = new CodeTypeReference(typeof(long));
				} else if (node is SimisTreeNodeValueFloat) {
					type = new CodeTypeReference(typeof(double));
				} else if (node is SimisTreeNodeValueString) {
					type = new CodeTypeReference(typeof(string));
				} else {
					return;
				}

				var dProperty = CreateEditObjectFor_AddProperty(dFields, dProperties, dPropertyName, type);
				if (node is SimisTreeNodeValueString) {
					//dProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Editor", new CodeAttributeArgument(new CodePrimitiveExpression("System.ComponentModel.Design.ArrayEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")), new CodeAttributeArgument(new CodeTypeOfExpression("UITypeEditor"))));
					dProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Editor", new CodeAttributeArgument(new CodePrimitiveExpression("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")), new CodeAttributeArgument(new CodeTypeOfExpression("UITypeEditor"))));
				}

				if (node is SimisTreeNodeValue) {
					dPropertyValues.Add(dProperty.Name, ((SimisTreeNodeValue)node).Value);
				}
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
					MessageBox.Show("CreateEditObjectFor() created C# code which failed to compile. Please look at 'CreateEditObjectFor.cs' in the application directory for the code.\n\n"
						+ String.Join("\n", messages), "Create Property Editor Data - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
	}
}
