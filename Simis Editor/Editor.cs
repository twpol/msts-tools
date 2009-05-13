using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using JGR;
using JGR.Grammar;
using JGR.IO.Parser;
using Microsoft.CSharp;

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
            var node = SimisTree.Nodes.Add("No file loaded.");
            node.NodeFont = new Font(SimisTree.Font, FontStyle.Italic);
			SimisTree.ExpandAll();
		}

		private void OpenFile(string filename) {
			var resourcesDirectory = Application.ExecutablePath;
			resourcesDirectory = resourcesDirectory.Substring(0, resourcesDirectory.LastIndexOf('\\')) + @"\Resources";

			// Load BNFs.
			var BNFs = new Dictionary<string, BNF>();
			foreach (var bnfFilename in Directory.GetFiles(resourcesDirectory, "*.bnf")) {
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

			// Load token names.
			var TokenNames = new Dictionary<uint, string>();
			foreach (var tokFilename in Directory.GetFiles(resourcesDirectory, "*.tok")) {
				var ffReader = new StreamReader(System.IO.File.OpenRead(tokFilename), Encoding.ASCII);
				var tokenType = (ushort)0x0000;
				var tokenIndex = (ushort)0x0000;
				for (var ffLine = ffReader.ReadLine(); !ffReader.EndOfStream; ffLine = ffReader.ReadLine()) {
					if (ffLine.StartsWith("SID_DEFINE_FIRST_ID(")) {
						var type = ffLine.Substring(ffLine.IndexOf('(') + 1, ffLine.LastIndexOf(')') - ffLine.IndexOf('(') - 1);
						tokenType = ushort.Parse(type.Substring(2), NumberStyles.HexNumber);
						tokenIndex = 0x0000;
					} else if (ffLine.StartsWith("SIDDEF(")) {
						var name = ffLine.Substring(ffLine.IndexOf('"') + 1, ffLine.LastIndexOf('"') - ffLine.IndexOf('"') - 1);
						TokenNames.Add(((uint)tokenType << 16) + ++tokenIndex, name);
					}
				}
			}

			var newFile = new SimisFile(filename, BNFs, TokenNames);
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
			foreach (var root in File.Roots) {
				InsertSimisBlock(SimisTree.Nodes, root);
			}
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
                treeNode.Name = bv.Name + "=\"" + bv.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
			} else if (simisBlock is SimisBlockValue) {
				var bv = (SimisBlockValue)simisBlock;
				treeNode.Name = bv.Name + "=" + bv;
			} else {
				treeNode.Name = simisBlock.Type;
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
				var sbv = (SimisBlockValue)simisBlock.Nodes[0];
				treeNode.Nodes[0].Name = treeNode.Nodes[0].Name.Substring(sbv.Name.Length + 1);
			}
			// If all children are values, don't expand the node.
			if (allChildrenAreValues) {
				treeNode.Text = GetNodeText(treeNode);
			} else {
				treeNode.Expand();
			}

			return (simisBlock is SimisBlockValue);
		}

        private void SelectNode(TreeNode treeNode) {
			SimisKey.Text = "<none>";
            if (treeNode.Tag == null) return;
			SimisBlock selectedBlock = (SimisBlock)treeNode.Tag;
			SimisKey.Text = selectedBlock.Key;
			SimisProperties.SelectedObject = CreateEditObjectFor(selectedBlock);
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

		private static object CreateEditObjectFor(SimisBlock block) {
			if (block.Nodes.Count<SimisBlock>(b => !(b is SimisBlockValue)) > 0) return null;

			var dClassName = block.Type.Replace(".", "_");

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
					while (dBlockNames.Contains(dPropertyName + (index == 0 ? "" : "#" + index))) index++;
					dPropertyName += "#" + index;
					dBlockNames.Add(dPropertyName);
				}

				CodeTypeReference type;
				if (child is SimisBlockValueInteger) {
					type = new CodeTypeReference(typeof(long));
				} else if (child is SimisBlockValueDouble) {
					type = new CodeTypeReference(typeof(double));
				} else if (child is SimisBlockValueString) {
					type = new CodeTypeReference(typeof(string));
				} else {
					// SetupDynamicTypesFor(doneTypes, dNamespace, child)
					type = new CodeTypeReference(typeof(string));
					//continue;
				}

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

				if (child is SimisBlockValue) {
					dPropertyValues.Add(dPropertyName, ((SimisBlockValue)child).Value);
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
			dNamespace.Types.Add(dClass);

			// Create compile unit, containing our dynamic namespace and classes.
			var dUnit = new CodeCompileUnit();
			dUnit.ReferencedAssemblies.Add("System.dll");
			dUnit.ReferencedAssemblies.Add("System.Windows.Forms.dll");
			dUnit.Namespaces.Add(dNamespace);

			// Set up compiler options.
			var compilerParams = new CompilerParameters();
			compilerParams.GenerateExecutable = false;
			compilerParams.GenerateInMemory = true;

			// Get the C# compiler and build a new assembly from the code we've just created.
			var compiler = new CSharpCodeProvider();
			using (var writer = new StreamWriter(Application.ExecutablePath + @"\..\dynamic_codedom.cs", false, Encoding.UTF8)) {
				compiler.GenerateCodeFromCompileUnit(dUnit, writer, new CodeGeneratorOptions());
			}
			var compileResults = compiler.CompileAssemblyFromDom(compilerParams, dUnit);

			// Did it work? Did it?
			if (compileResults.Errors.HasErrors) {
				var messages = new string[compileResults.Output.Count];
				compileResults.Output.CopyTo(messages, 0);
				MessageBox.Show(String.Join("\n", messages), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void SimisTree_AfterSelect(object sender, TreeViewEventArgs e) {
            SelectNode(e.Node);
        }

		private void SimisProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
			//
		}
	}
}
