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
using JGR;
using JGR.IO.Parser;
using Microsoft.CSharp;
using SimisEditor.Properties;

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
		protected TreeNode SelectedNode;
		protected SimisProvider SimisProvider;

		public Editor() {
			InitializeComponent();
			NewFile();

			var resourcesDirectory = Application.ExecutablePath;
			resourcesDirectory = resourcesDirectory.Substring(0, resourcesDirectory.LastIndexOf('\\')) + @"\Resources";
			SimisProvider = new SimisProvider(resourcesDirectory);
			var thread = new Thread(() => WaitForSimisProvider());
			thread.Start();

			var versionCheck = new CodePlexVersionCheck(Settings.Default.UpdateCheckCodePlexProjectUrl, Settings.Default.UpdateCheckCodePlexProjectName, Settings.Default.UpdateCheckCodePlexReleaseDate);
			versionCheck.CheckComplete += new EventHandler((o, e) => this.Invoke((MethodInvoker)(() => {
				if (versionCheck.HasLatestVersion) {
					if (versionCheck.IsNewVersion) {
						var item = menuStrip.Items.Add("New Version: " + versionCheck.LatestVersionTitle);
						item.Alignment = ToolStripItemAlignment.Right;
						item.Click += new EventHandler((o2, e2) => Process.Start(versionCheck.LatestVersionUri.AbsoluteUri));
					//} else {
					//    var item = menuStrip.Items.Add("Current Version: " + versionCheck.LatestVersionTitle);
					//    item.Alignment = ToolStripItemAlignment.Right;
					//    item.Click += new EventHandler((o2, e2) => Process.Start(versionCheck.LatestVersionUri.AbsoluteUri));
					}
				} else {
					var item = menuStrip.Items.Add("Error Checking for New Version");
					item.Alignment = ToolStripItemAlignment.Right;
					item.Click += new EventHandler((o2, e2) => Process.Start(Settings.Default.AboutUpdatesUrl));
				}
			})));
			versionCheck.Check();
		}

		private void WaitForSimisProvider() {
			try {
				SimisProvider.Join();
			} catch (FileException ex) {
				//using (var messages = new Messages()) {
					//bnf.RegisterMessageSink(messages);
					//messages.MessageAccept("Editor", BufferedMessageSource.LEVEL_CRITIAL, ex.ToString());
					this.Invoke((MethodInvoker)(() => {
						//messages.ShowDialog(this);
						MessageBox.Show(ex.ToString(), "Load Resources - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}));
					//bnf.UnregisterMessageSink(messages);
				//}
			}

			this.Invoke((MethodInvoker)(() => UpdateFromSimisProvider()));
		}

		private void UpdateFromSimisProvider() {
			var generalName = "Train Simulator files";

			var simisFormats = new List<List<string>>();
			simisFormats.Add(new List<string>(new string[] { "All " + generalName }));
			foreach (var format in SimisProvider.FileFormats) {
				simisFormats[0].Add(format.Value);
				simisFormats.Add(new List<string>(new string[] { format.Key + " files", format.Value }));
			}
			simisFormats.Add(new List<string>(new string[] { "All files", "*.*" }));
			openFileDialog.Filter = String.Join("|", simisFormats.Select<List<string>, string>(l => l[0] + "|" + String.Join(";", l.ToArray(), 1, l.Count - 1)).ToArray());

			var streamFormats = new string[] { "Text", "Binary", "Compressed Binary" };
			saveFileDialog.Filter = String.Join("|", streamFormats.Select<string, string>(s => s + " " + generalName + "|" + String.Join(";", simisFormats[0].ToArray(), 1, simisFormats[0].Count - 1)).ToArray());
		}

		private void NewFile() {
			Modified = false;
			Filename = "";
			File = new SimisFile("", SimisProvider);
			SelectNode(null);
			SimisTree.Nodes.Clear();
            var node = SimisTree.Nodes.Add("No file loaded.");
            node.NodeFont = new Font(SimisTree.Font, FontStyle.Italic);
			SimisTree.ExpandAll();
			if (SimisTree.Nodes.Count > 0) {
				SimisTree.TopNode = SimisTree.Nodes[0];
			}
			UpdateTitle();
		}

		private void OpenFile(string filename) {
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
			SimisTree.Nodes.Clear();
			foreach (var root in File.Roots) {
				InsertSimisBlock(SimisTree.Nodes, root);
			}
			SimisTree.ExpandAll();
			if (SimisTree.Nodes.Count > 0) {
				SimisTree.TopNode = SimisTree.Nodes[0];
			}
			UpdateTitle();
		}

		private void FileModified() {
			Modified = true;
			UpdateTitle();
		}

		private void SaveFile() {
			if (File.Filename != Filename) {
				File.Filename = Filename;
			}
			try {
				File.WriteFile();
			} catch (FileException ex) {
				MessageBox.Show(ex.ToString(), "Save File - " + Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			Modified = false;
			UpdateTitle();
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

		private void UpdateTitle() {
			Text = FilenameTitle + (Modified ? "*" : "") + " - " + Application.ProductName;
		}

		private void InsertSimisBlock(TreeNodeCollection treeNodes, SimisBlock block) {
			var treeNode = treeNodes.Add(GetNodeText(block));
			treeNode.Tag = block;

			if (block.Nodes.Any<SimisBlock>(b => !(b is SimisBlockValue))) {
				// If we have any non-value child blocks, add everything.
				foreach (var child in block.Nodes) {
					InsertSimisBlock(treeNode.Nodes, child);
				}
			}
		}

        private void SelectNode(TreeNode treeNode) {
			if ((treeNode == null) || (treeNode.Tag == null)) {
				SelectedNode = null;
				SimisProperties.SelectedObject = null;
				return;
			}
			SelectedNode = treeNode;
			SimisProperties.SelectedObject = CreateEditObjectFor((SimisBlock)SelectedNode.Tag);
        }

		private static string BlockToNameString(SimisBlock block) {
			if (block.Name.Length > 0) return block.Type + " \"" + block.Name + "\"";
			return block.Type;
		}

		private static string BlockToValueString(SimisBlockValue block) {
			if (block is SimisBlockValueString) {
				return "\"" + block.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
			}
			return block.ToString();
		}

		private string GetNodeText(SimisBlock block) {
			// Non-trivial node, don't bother trying.
			if (block.Nodes.Count<SimisBlock>(b => !(b is SimisBlockValue)) > 0) {
				return BlockToNameString(block);
			}

			// Just a single value child, easy.
			if (block.Nodes.Count == 1) {
				return BlockToNameString(block) + "=" + BlockToValueString((SimisBlockValue)block.Nodes[0]);
			}

			// Multiple value children. Quite easy.
			if (block.Nodes.Count > 1) {
				var values = new List<string>();
				foreach (var child in block.Nodes) {
					values.Add(BlockToNameString(child) + "=" + BlockToValueString((SimisBlockValue)child));
				}
				return BlockToNameString(block) + " (" + String.Join(" ", values.ToArray()) + ")";
			}

			// No children.
			if (block is SimisBlockValue) {
				return BlockToNameString(block) + "=" + BlockToValueString((SimisBlockValue)block);
			}

			return BlockToNameString(block);
		}

		private static object CreateEditObjectFor(SimisBlock block) {
			if (block.Nodes.Count<SimisBlock>(b => !(b is SimisBlockValue)) > 0) return null;

			var dClassName = "block_" + block.Type.Replace(".", "_");

			// Create constructor.
			var dConstructor = new CodeConstructor();
			dConstructor.Attributes = MemberAttributes.Public;

			// Go through the Simis data to find suitable things for editing.
			var dFields = new List<CodeMemberField>();
			var dProperties = new List<CodeMemberProperty>();
			var dBlockNames = new List<string>();
			var dPropertyValues = new Dictionary<string, object>();
			foreach (var child in block.Nodes) {
				var dPropertyName = child.Name.Length > 0 ? child.Name : child.Type;
				if (block.Nodes.Count<SimisBlock>(b => (b.Name.Length > 0 ? b.Name : b.Type) == dPropertyName) > 1) {
					var index = 1;
					while (dBlockNames.Contains(dPropertyName + (index == 0 ? "" : "_" + index))) index++;
					dPropertyName += "_" + index;
					dBlockNames.Add(dPropertyName);
				}
				if (block.Nodes.Count == 1) {
					dPropertyName = block.Type;
				}

				if (child is SimisBlockValue) {
					// Store the SimisBlockValue in a way we can find later.
					var dSimisProperty = CreateEditObjectFor_AddProperty(dFields, dProperties, dPropertyName + "_SimisBlockValue", new CodeTypeReference(typeof(SimisBlockValue)));
					dSimisProperty.CustomAttributes.Add(new CodeAttributeDeclaration("BrowsableAttribute", new CodeAttributeArgument(new CodePrimitiveExpression(false))));
					dPropertyValues.Add(dSimisProperty.Name, (SimisBlockValue)child);
				}

				CodeTypeReference type;
				if (child is SimisBlockValueInteger) {
					type = new CodeTypeReference(typeof(long));
				} else if (child is SimisBlockValueFloat) {
					type = new CodeTypeReference(typeof(double));
				} else if (child is SimisBlockValueString) {
					type = new CodeTypeReference(typeof(string));
				} else {
					// SetupDynamicTypesFor(doneTypes, dNamespace, child)
					//type = new CodeTypeReference(typeof(string));
					continue;
				}

				var dProperty = CreateEditObjectFor_AddProperty(dFields, dProperties, dPropertyName, type);
				if (child is SimisBlockValueString) {
					//dProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Editor", new CodeAttributeArgument(new CodePrimitiveExpression("System.ComponentModel.Design.ArrayEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")), new CodeAttributeArgument(new CodeTypeOfExpression("UITypeEditor"))));
					dProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Editor", new CodeAttributeArgument(new CodePrimitiveExpression("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")), new CodeAttributeArgument(new CodeTypeOfExpression("UITypeEditor"))));
				}

				if (child is SimisBlockValue) {
					dPropertyValues.Add(dProperty.Name, ((SimisBlockValue)child).Value);
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
			dNamespace.Imports.Add(new CodeNamespaceImport("JGR.IO.Parser"));
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

		private static CodeMemberProperty CreateEditObjectFor_AddProperty(List<CodeMemberField> dFields, List<CodeMemberProperty> dProperties, string dPropertyName, CodeTypeReference type) {
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
			if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
				OpenFile(openFileDialog.FileName);
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFile();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK) {
				Filename = saveFileDialog.FileName;
				// FilterIndex is 1-based, SIGH. Filters: 1=Text, 2=Binary, 3=Compressed Binary.
				File.StreamFormat = saveFileDialog.FilterIndex == 1 ? SimisStreamFormat.Text : SimisStreamFormat.Binary;
				File.StreamCompressed = saveFileDialog.FilterIndex == 3;
				SaveFile();
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!SaveFileIfModified()) {
				return;
			}
			Close();
		}

        private void SimisTree_AfterSelect(object sender, TreeViewEventArgs e) {
            SelectNode(e.Node);
        }

		private void SimisProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
			if (SelectedNode == null) return;

			var block = (SimisBlock)SelectedNode.Tag;
			var child = (SimisBlockValue)SimisProperties.SelectedObject.GetType().GetProperty(e.ChangedItem.Label + "_SimisBlockValue").GetValue(SimisProperties.SelectedObject, null);
			var value = e.ChangedItem.Value;

			if (child is SimisBlockValueInteger) {
				child.Value = (long)value;
			} else if (child is SimisBlockValueFloat) {
				child.Value = (double)value;
			} else if (child is SimisBlockValueString) {
				child.Value = (string)value;
			}

			SelectedNode.Text = GetNodeText(block);
		}

		private void testToolStripMenuItem_Click(object sender, EventArgs e) {
			folderBrowserDialog.Description = "Select a folder to test files from.";
			if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK) {
				using (var test = new Test()) {
					test.SetupTest(folderBrowserDialog.SelectedPath, SimisProvider);
					test.ShowDialog(this);
				}
			}
		}

		private void homepageToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start(Settings.Default.AboutHomepageUrl);
		}

		private void updatesToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start(Settings.Default.AboutUpdatesUrl);
		}

		private void discussionsToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start(Settings.Default.AboutDiscussionsUrl);
		}

		private void issueTrackerToolStripMenuItem_Click(object sender, EventArgs e) {
			Process.Start(Settings.Default.AboutIssuesUrl);
		}
	}
}
