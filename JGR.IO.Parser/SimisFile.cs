﻿//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace Jgr.IO.Parser
{
	/// <summary>
	/// A support class for using <see cref="SimisReader"/> and <see cref="SimisWriter"/> with on-disk files and editing capabilities.
	/// </summary>
	public class SimisFile
	{
		public string FileName { get; set; }
		public SimisFormat SimisFormat { get; set; }
		public SimisStreamFormat StreamFormat { get; set; }
		public bool StreamCompressed { get; set; }
		Stack<SimisTreeNode> UndoBuffer { get; set; }
		Stack<SimisTreeNode> RedoBuffer { get; set; }
		SimisTreeNode _Tree { get; set; }
		SimisProvider SimisProvider;

		public SimisFile(string fileName, SimisProvider provider) {
			FileName = fileName;
			UndoBuffer = new Stack<SimisTreeNode>();
			RedoBuffer = new Stack<SimisTreeNode>();
			ResetUndo(new SimisTreeNode("<root>", ""));
			SimisFormat = null;
			SimisProvider = provider.GetForPath(fileName);
		}

		/// <summary>
		/// Gets or sets the root <see cref="SimisTreeNode"/> for the tree read or written by this class.
		/// </summary>
		/// <remarks>
		/// <para>Setting the <see cref="Tree"/> will add to the available undo buffers and reset the redo buffers.</para>
		/// </remarks>
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

		/// <summary>
		/// Switches to the previous <see cref="SimisTreeNode"/> root.
		/// </summary>
		public void Undo() {
			RedoBuffer.Push(_Tree);
			_Tree = UndoBuffer.Pop();
		}

		/// <summary>
		/// Switches to the next <see cref="SimisTreeNode"/> root.
		/// </summary>
		public void Redo() {
			UndoBuffer.Push(_Tree);
			_Tree = RedoBuffer.Pop();
		}

		/// <summary>
		/// Gets a <see cref="bool"/> indicating whether undo is available.
		/// </summary>
		public bool CanUndo {
			get {
				return UndoBuffer.Count > 0;
			}
		}

		/// <summary>
		/// Gets a <see cref="bool"/> indicating whether redo is available.
		/// </summary>
		public bool CanRedo {
			get {
				return RedoBuffer.Count > 0;
			}
		}

		/// <summary>
		/// Reads the file specified in <see cref="FileName"/> into the <see cref="Tree"/>.
		/// </summary>
		/// <remarks>
		/// <para>This operation will reset the undo and redo buffers if it succeeds.</para>
		/// </remarks>
		public void ReadFile() {
			try {
				using (var fileStream = File.OpenRead(FileName)) {
					ReadStream(fileStream);
				}
			} catch (ReaderException e) {
				throw new FileException(FileName, e);
			}
		}

		/// <summary>
		/// Reads any readable <see cref="Stream"/> into the <see cref="Tree"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to read from.</param>
		/// <remarks>
		/// <para>This operation will reset the undo and redo buffers if it succeeds.</para>
		/// </remarks>
		public void ReadStream(Stream stream) {
			var reader = new SimisReader(new BufferedInMemoryStream(stream), SimisProvider, SimisFormat);

			var blockStack = new Stack<KeyValuePair<SimisToken, List<SimisTreeNode>>>();
			blockStack.Push(new KeyValuePair<SimisToken, List<SimisTreeNode>>(null, new List<SimisTreeNode>()));
			while (!reader.EndOfStream) {
				var token = reader.ReadToken();

				switch (token.Kind) {
					case SimisTokenKind.Block:
						blockStack.Push(new KeyValuePair<SimisToken, List<SimisTreeNode>>(token, new List<SimisTreeNode>()));
						break;
					case SimisTokenKind.BlockBegin:
						break;
					case SimisTokenKind.BlockEnd:
						var block = blockStack.Pop();
						var node = new SimisTreeNode(block.Key.Type, block.Key.Name, block.Value);
						blockStack.Peek().Value.Add(node);
						break;
					case SimisTokenKind.IntegerUnsigned:
						blockStack.Peek().Value.Add(new SimisTreeNodeValueIntegerUnsigned(token.Type, token.Name, token.IntegerUnsigned));
						break;
					case SimisTokenKind.IntegerSigned:
						blockStack.Peek().Value.Add(new SimisTreeNodeValueIntegerSigned(token.Type, token.Name, token.IntegerSigned));
						break;
					case SimisTokenKind.IntegerDWord:
						blockStack.Peek().Value.Add(new SimisTreeNodeValueIntegerDWord(token.Type, token.Name, token.IntegerDWord));
						break;
					case SimisTokenKind.IntegerWord:
						blockStack.Peek().Value.Add(new SimisTreeNodeValueIntegerWord(token.Type, token.Name, (ushort)token.IntegerDWord));
						break;
					case SimisTokenKind.IntegerByte:
						blockStack.Peek().Value.Add(new SimisTreeNodeValueIntegerByte(token.Type, token.Name, (byte)token.IntegerDWord));
						break;
					case SimisTokenKind.Float:
						blockStack.Peek().Value.Add(new SimisTreeNodeValueFloat(token.Type, token.Name, token.Float));
						break;
					case SimisTokenKind.String:
						blockStack.Peek().Value.Add(new SimisTreeNodeValueString(token.Type, token.Name, token.String));
						break;
				}
			}

			var rootBlock = blockStack.Pop();
			var tree = new SimisTreeNode("<root>", "", rootBlock.Value);

			// We need to do this AFTER reading the file, otherwise they'll not be correct.
			SimisFormat = reader.SimisFormat;
			StreamFormat = reader.StreamFormat;
			StreamCompressed = reader.StreamCompressed;
			ResetUndo(tree);
		}

		/// <summary>
		/// Writes the <see cref="Tree"/> out to the file specified in <see cref="FileName"/>.
		/// </summary>
		public void WriteFile() {
			try {
				using (var fileStream = File.Create(FileName)) {
					WriteStream(fileStream);
				}
			} catch (Exception e) {
				throw new FileException(FileName, e);
			}
		}

		/// <summary>
		/// Writes the <see cref="Tree"/> to any writable <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to write to.</param>
		public void WriteStream(Stream stream) {
			var writer = new SimisWriter(stream, SimisProvider, SimisFormat, StreamFormat, StreamCompressed);
			foreach (var child in Tree) {
				WriteBlock(writer, child);
			}
			writer.WriteEnd();
		}

		void WriteBlock(SimisWriter writer, SimisTreeNode block) {
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Block, Type = block.Type, Name = block.Name });
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockBegin });
			foreach (var child in block) {
				var childValue = child as SimisTreeNodeValue;
				if (childValue != null) {
					if (child is SimisTreeNodeValueIntegerUnsigned) {
						writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.IntegerUnsigned, IntegerUnsigned = (uint)childValue.Value });
					} else if (child is SimisTreeNodeValueIntegerSigned) {
						writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.IntegerSigned, IntegerSigned = (int)childValue.Value });
					} else if (child is SimisTreeNodeValueIntegerDWord) {
						writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.IntegerDWord, IntegerDWord = (uint)childValue.Value });
					} else if (child is SimisTreeNodeValueIntegerWord) {
						writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.IntegerWord, IntegerDWord = (ushort)childValue.Value });
					} else if (child is SimisTreeNodeValueIntegerByte) {
						writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.IntegerByte, IntegerDWord = (byte)childValue.Value });
					} else if (child is SimisTreeNodeValueFloat) {
						writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Float, Float = (float)childValue.Value });
					} else if (child is SimisTreeNodeValueString) {
						writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.String, String = (string)childValue.Value });
					} else {
						throw new InvalidDataException("Simis tree node " + child + " is not a known SimisTreeNodeValue descendant.");
					}
				} else {
					WriteBlock(writer, child);
				}
			}
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockEnd });
		}
	}
}
