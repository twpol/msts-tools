//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Jgr.Grammar;

namespace Jgr.IO.Parser
{
	public class SimisFile
	{
		public string FileName { get; set; }
		public SimisStreamFormat StreamFormat { get; set; }
		public bool StreamCompressed { get; set; }
		public string SimisFormat { get; set; }
		Stack<SimisTreeNode> UndoBuffer { get; set; }
		Stack<SimisTreeNode> RedoBuffer { get; set; }
		SimisTreeNode _Tree { get; set; }
		SimisProvider SimisProvider;

		public SimisFile(string fileName, SimisProvider provider) {
			FileName = fileName;
			UndoBuffer = new Stack<SimisTreeNode>();
			RedoBuffer = new Stack<SimisTreeNode>();
			ResetUndo(new SimisTreeNode("<root>", ""));
			SimisProvider = provider;
		}

		public SimisTreeNode Tree {
			get {
				return _Tree;
			}
			set {
				UndoBuffer.Push(_Tree);
				RedoBuffer.Clear();
				_Tree = value;
			}
		}

		void ResetUndo(SimisTreeNode newTree) {
			UndoBuffer.Clear();
			RedoBuffer.Clear();
			_Tree = newTree;
		}

		public void Undo() {
			RedoBuffer.Push(_Tree);
			_Tree = UndoBuffer.Pop();
		}

		public void Redo() {
			UndoBuffer.Push(_Tree);
			_Tree = RedoBuffer.Pop();
		}

		public bool CanUndo {
			get {
				return UndoBuffer.Count > 0;
			}
		}

		public bool CanRedo {
			get {
				return RedoBuffer.Count > 0;
			}
		}

		public void ReadFile() {
			try {
				using (var fileStream = File.OpenRead(FileName)) {
					ReadStream(fileStream);
				}
			} catch (ReaderException e) {
				throw new FileException(FileName, e);
			}
		}

		public void ReadStream(Stream stream) {
			var reader = new SimisReader(new BufferedInMemoryStream(stream), SimisProvider);

			var tree = new SimisTreeNode("<root>", "");
			var blockStack = new List<SimisTreeNode>();
			while (!reader.EndOfStream) {
				var token = reader.ReadToken();
				var name = "";
				if ((reader.BnfState != null) && (reader.BnfState.State != null) && (reader.BnfState.State.Op is NamedReferenceOperator)) {
					name = ((NamedReferenceOperator)reader.BnfState.State.Op).Name;
				}

				switch (token.Kind) {
					case SimisTokenKind.Block:
						var block = new SimisTreeNode(token.Type, token.String);
						tree = tree.Apply(blockStack, node => node.AppendChild(block));
						blockStack.Add(block);
						break;
					case SimisTokenKind.BlockBegin:
						break;
					case SimisTokenKind.BlockEnd:
						blockStack.RemoveAt(blockStack.Count - 1);
						break;
					case SimisTokenKind.IntegerUnsigned:
						tree = tree.Apply(blockStack, node => node.AppendChild(new SimisTreeNodeValueIntegerUnsigned(token.Type, name, token.IntegerUnsigned)));
						break;
					case SimisTokenKind.IntegerSigned:
						tree = tree.Apply(blockStack, node => node.AppendChild(new SimisTreeNodeValueIntegerSigned(token.Type, name, token.IntegerSigned)));
						break;
					case SimisTokenKind.IntegerDWord:
						tree = tree.Apply(blockStack, node => node.AppendChild(new SimisTreeNodeValueIntegerDWord(token.Type, name, token.IntegerDWord)));
						break;
					case SimisTokenKind.Float:
						tree = tree.Apply(blockStack, node => node.AppendChild(new SimisTreeNodeValueFloat(token.Type, name, token.Float)));
						break;
					case SimisTokenKind.String:
						tree = tree.Apply(blockStack, node => node.AppendChild(new SimisTreeNodeValueString(token.Type, name, token.String)));
						break;
				}
			}

			// We need to do this AFTER reading the file, otherwise they'll not be correct.
			StreamFormat = reader.StreamFormat;
			StreamCompressed = reader.StreamCompressed;
			SimisFormat = reader.SimisFormat;
			ResetUndo(tree);
		}

		public void WriteFile() {
			try {
				using (var fileStream = File.Create(FileName)) {
					WriteStream(fileStream);
				}
			} catch (Exception e) {
				throw new FileException(FileName, e);
			}
		}

		public void WriteStream(Stream stream) {
			var writer = new SimisWriter(stream, SimisProvider, StreamFormat, StreamCompressed, SimisFormat);
			foreach (var child in Tree) {
				WriteBlock(writer, child);
			}
			writer.WriteEnd();
		}

		void WriteBlock(SimisWriter writer, SimisTreeNode block) {
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Block, Type = block.Type, String = block.Name });
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockBegin });
			foreach (var child in block) {
				if (child is SimisTreeNodeValueIntegerUnsigned) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.IntegerUnsigned, IntegerUnsigned = (uint)((SimisTreeNodeValue)child).Value });
				} else if (child is SimisTreeNodeValueIntegerSigned) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.IntegerSigned, IntegerSigned = (int)((SimisTreeNodeValue)child).Value });
				} else if (child is SimisTreeNodeValueIntegerDWord) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.IntegerDWord, IntegerDWord = (uint)((SimisTreeNodeValue)child).Value });
				} else if (child is SimisTreeNodeValueFloat) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Float, Float = (float)((SimisTreeNodeValue)child).Value });
				} else if (child is SimisTreeNodeValueString) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.String, String = (string)((SimisTreeNodeValue)child).Value });
				} else {
					WriteBlock(writer, child);
				}
			}
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockEnd });
		}
	}
}
