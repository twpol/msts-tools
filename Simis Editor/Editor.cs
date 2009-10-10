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

		void WaitForSimisProvider() {
			try {
				SimisProvider.Join();
			} catch (FileException ex) {
				this.Invoke((MethodInvoker)(() => {
					MessageBox.Show(ex.ToString(), "Load Resources - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}));
			}

			this.Invoke((MethodInvoker)(() => UpdateFromSimisProvider()));
		}

		void UpdateFromSimisProvider() {
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

		void NewFile() {
			Modified = false;
			Filename = "";
			File = new SimisFile("", SimisProvider);
			SelectNode(null);
			ResyncSimisNodes();
			UpdateTitle();
		}

		void OpenFile(string filename) {
			var newFile = new SimisFile(filename, SimisProvider);
			try {
				newFile.ReadFile();
			} catch (FileException ex) {
				MessageBox.Show(ex.ToString(), "Open File - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
		}

		void FileModified() {
			Modified = true;
			UpdateTitle();
		}

		void SaveFile() {
			if (File.FileName != Filename) {
				File.FileName = Filename;
			}
			try {
				File.WriteFile();
			} catch (FileException ex) {
				if (ex.InnerException is UnauthorizedAccessException) {
					MessageBox.Show(ex.InnerException.Message, "Save File - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				} else {
					MessageBox.Show(ex.ToString(), "Save File - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				return;
			}

			Modified = false;
			UpdateTitle();
		}

		bool SaveFileIfModified() {
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

		void UpdateTitle() {
			Text = FilenameTitle + (Modified ? "*" : "") + " - " + Application.ProductName;
		}

		void ResyncSimisNodes() {
			Debug.WriteLine(File.Tree.Root.ToString());
			ResyncSimisNodes(SimisTree.Nodes, File.Tree.Root.Children);
		}

		void ResyncSimisNodes(TreeNodeCollection viewTreeNodes, SimisTreeNode[] simisTreeNodes) {
			var viewTreeNodeIndex = 0;
			var simisTreeNodeIndex = 0;

			if (simisTreeNodes.All<SimisTreeNode>(n => n is SimisTreeNodeValue)) {
				simisTreeNodes = new SimisTreeNode[] { };
			}

			while ((viewTreeNodeIndex < viewTreeNodes.Count) && (simisTreeNodeIndex < simisTreeNodes.Length)) {
				if (viewTreeNodes[viewTreeNodeIndex].Tag == simisTreeNodes[simisTreeNodeIndex]) {
					// This view node is the same simis node, so it is unchanged.
					viewTreeNodeIndex++;
					simisTreeNodeIndex++;
					continue;
				}

				if (simisTreeNodes[simisTreeNodeIndex].EqualsByValue(viewTreeNodes[viewTreeNodeIndex].Tag)) {
					viewTreeNodes[viewTreeNodeIndex].Text = GetNodeText(simisTreeNodes[simisTreeNodeIndex]);
					viewTreeNodes[viewTreeNodeIndex].Tag = simisTreeNodes[simisTreeNodeIndex];
					Debug.WriteLine(viewTreeNodes[viewTreeNodeIndex].FullPath + " [VN:Updated]");
					ResyncSimisNodes(viewTreeNodes[viewTreeNodeIndex].Nodes, simisTreeNodes[simisTreeNodeIndex].Children);
					viewTreeNodeIndex++;
					simisTreeNodeIndex++;
					continue;
				}

				// View node may have been deleted or had a node inserted before in simis tree.
				for (var i = simisTreeNodeIndex + 1; i < simisTreeNodes.Length; i++) {
					if (viewTreeNodes[viewTreeNodeIndex].Tag == simisTreeNodes[i]) {
						// View node has had at least one node inserted before it in the simis tree.
						for (var j = simisTreeNodeIndex; j < i; j++) {
							var viewNode = viewTreeNodes.Insert(viewTreeNodeIndex++, GetNodeText(simisTreeNodes[j]));
							viewNode.Tag = simisTreeNodes[j];
							Debug.WriteLine(viewNode.FullPath + " [VN:Insert]");
							ResyncSimisNodes(viewNode.Nodes, simisTreeNodes[j].Children);
						}
						simisTreeNodeIndex = i;
						continue;
					}
				}
				// This view node wasn't found, remove it.
				Debug.WriteLine(viewTreeNodes[viewTreeNodeIndex].FullPath + " [VN:Remove]");
				viewTreeNodes.RemoveAt(viewTreeNodeIndex);
			}

			while (viewTreeNodeIndex < viewTreeNodes.Count) {
				viewTreeNodes.RemoveAt(viewTreeNodeIndex);
			}

			while (simisTreeNodeIndex < simisTreeNodes.Length) {
				var viewNode = viewTreeNodes.Add(GetNodeText(simisTreeNodes[simisTreeNodeIndex]));
				viewNode.Tag = simisTreeNodes[simisTreeNodeIndex];
				Debug.WriteLine(viewNode.FullPath + " [VN:Add]");
				ResyncSimisNodes(viewNode.Nodes, simisTreeNodes[simisTreeNodeIndex].Children);
				simisTreeNodeIndex++;
			}
		}

		void ResyncSimisNodes(TreeNode viewTreeNode, SimisTreeNode simisTreeNode) {
			viewTreeNode.Text = GetNodeText(simisTreeNode);
			viewTreeNode.Nodes.Clear();
			if (simisTreeNode.Children.Any<SimisTreeNode>(b => !(b is SimisTreeNodeValue))) {
				ResyncSimisNodes(viewTreeNode.Nodes, simisTreeNode.Children);
			}
		}

		//void InsertSimisBlock(TreeNodeCollection treeNodes, SimisTreeNode block) {
		//    var treeNode = treeNodes.Add(GetNodeText(block));
		//    treeNode.Tag = block;

		//    if (block.Children.Any<SimisTreeNode>(b => !(b is SimisTreeNodeValue))) {
		//        // If we have any non-value child blocks, add everything.
		//        foreach (var child in block.Children) {
		//            InsertSimisBlock(treeNode.Nodes, child);
		//        }
		//    }
		//}

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
			if (block.Children.Count<SimisTreeNode>(b => !(b is SimisTreeNodeValue)) > 0) {
				return BlockToNameString(block);
			}

			// Just a single value child, easy.
			if (block.Children.Length == 1) {
				return BlockToNameOnlyString(block) + "=" + BlockToValueString((SimisTreeNodeValue)block.Children[0]);
			}

			// Multiple value children, quite easy.
			if (block.Children.Length > 1) {
				var values = new List<string>();
				foreach (var child in block.Children) {
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

		static object CreateEditObjectFor(SimisTreeNode block) {
			if (block.Children.Any<SimisTreeNode>(b => !(b is SimisTreeNodeValue))) return null;

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
				if (block.Children.Count<SimisTreeNode>(b => (b.Name.Length > 0 ? b.Name : b.Type) == dPropertyName) > 1) {
					var index = 1;
					while (dBlockNames.Contains(dPropertyName + (index == 0 ? "" : "_" + index))) index++;
					dPropertyName += "_" + index;
					dBlockNames.Add(dPropertyName);
				}
				if (block.Children.Length == 1) {
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
				foreach (var child in block.Children) {
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
				MessageBox.Show("CreateEditObjectFor() created C# code which failed to compile. Please look at 'CreateEditObjectFor.cs' in the application directory for the code.\n\n"
					+ String.Join("\n", messages), "Create Property Editor Data - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
			if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
				OpenFile(openFileDialog.FileName);
			}
		}

		void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFile();
		}

		void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
			// FilterIndex is 1-based, SIGH. Filters: 1=Text, 2=Binary, 3=Compressed Binary.
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

        void SimisTree_AfterSelect(object sender, TreeViewEventArgs e) {
            SelectNode(e.Node);
        }

		void SimisProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
			if (SelectedNode == null) return;

			var node = SelectedNode;
			var block = (SimisTreeNode)node.Tag;
			var blockPath = new Stack<SimisTreeNode>();
			while (node != null) {
				blockPath.Push((SimisTreeNode)node.Tag);
				node = node.Parent;
			}
			blockPath.Push(File.Tree.Root);

			var child = (SimisTreeNodeValue)SimisProperties.SelectedObject.GetType().GetProperty(e.ChangedItem.Label + "_SimisTreeNodeValue").GetValue(SimisProperties.SelectedObject, null);
			var value = e.ChangedItem.Value;

			if (child is SimisTreeNodeValueInteger) {
				File.Tree.ReplaceChild(blockPath, new SimisTreeNodeValueInteger(child.Type, child.Name, (long)value), child);
			} else if (child is SimisTreeNodeValueFloat) {
				File.Tree.ReplaceChild(blockPath, new SimisTreeNodeValueFloat(child.Type, child.Name, (double)value), child);
			} else if (child is SimisTreeNodeValueString) {
				File.Tree.ReplaceChild(blockPath, new SimisTreeNodeValueString(child.Type, child.Name, (string)value), child);
			}

			ResyncSimisNodes();
			SelectNode(SimisTree.SelectedNode);
		}

		void reloadSimisResourcesToolStripMenuItem_Click(object sender, EventArgs e) {
			InitializeSimisProvider();
		}

		void testToolStripMenuItem_Click(object sender, EventArgs e) {
			folderBrowserDialog.Description = "Select a folder to test files from.";
			if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK) {
				using (var test = new Test()) {
					test.SetupTest(folderBrowserDialog.SelectedPath, SimisProvider);
					test.ShowDialog(this);
				}
			}
		}

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

		void Editor_DragEnter(object sender, DragEventArgs e) {
			e.Effect = DragDropEffects.None;
			if (!e.Data.GetDataPresent(DataFormats.FileDrop)) {
				return;
			}
			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length != 1) {
				return;
			}
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
	}
}
