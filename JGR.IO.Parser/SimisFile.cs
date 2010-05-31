//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Jgr.IO.Parser {
	[Immutable]
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public class SimisFile : DataTreeNode<SimisFile> {
		public readonly string FileName;
		public readonly SimisFormat SimisFormat;
		public readonly SimisStreamFormat StreamFormat;
		public readonly bool StreamCompressed;
		public readonly SimisTreeNode Tree;
		readonly SimisProvider SimisProvider;

		/// <summary>
		/// Creates a <see cref="SimisFile"/> from a file.
		/// </summary>
		/// <param name="fileName">The file to read from.</param>
		/// <param name="simisProvider">A <see cref="SimisProvider"/> within which the appropriate <see cref="Bnf"/> for parsing can be found.</param>
		public SimisFile(string fileName, SimisProvider simisProvider) {
			FileName = fileName;
			SimisProvider = simisProvider.GetForPath(FileName);
			try {
				using (var fileStream = File.OpenRead(FileName)) {
					ReadStream(fileStream, out SimisFormat, out StreamFormat, out StreamCompressed, out Tree);
				}
			} catch (ReaderException e) {
				throw new FileException(FileName, e);
			}
		}

		/// <summary>
		/// Creates a <see cref="SimisFile"/> from a readable <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to read from.</param>
		/// <param name="simisProvider">A <see cref="SimisProvider"/> within which the appropriate <see cref="Bnf"/> for parsing can be found.</param>
		public SimisFile(Stream stream, SimisProvider simisProvider) {
			FileName = "";
			SimisProvider = simisProvider;
			ReadStream(stream, out SimisFormat, out StreamFormat, out StreamCompressed, out Tree);
		}

		/// <summary>
		/// Creates a <see cref="SimisFile"/> from the component parts.
		/// </summary>
		/// <param name="fileName">The file to read from.</param>
		/// <param name="simisFormat">The <see cref="SimisFormat"/> for this <see cref="SimisFile"/>.</param>
		/// <param name="streamFormat">The <see cref="SimisStreamFormat"/> for this <see cref="SimisFile"/>.</param>
		/// <param name="streamCompressed">The <see cref="bool"/> indicating whether the stream should be compressed when written from this <see cref="SimisFile"/>.</param>
		/// <param name="tree">The <see cref="SimisTreeNode"/> tree for this <see cref="SimisFile"/>.</param>
		/// <param name="simisProvider">A <see cref="SimisProvider"/> within which the appropriate <see cref="Bnf"/> for writing can be found.</param>
		public SimisFile(string fileName, SimisFormat simisFormat, SimisStreamFormat streamFormat, bool streamCompressed, SimisTreeNode tree, SimisProvider simisProvider) {
			FileName = fileName;
			SimisFormat = simisFormat;
			StreamFormat = streamFormat;
			StreamCompressed = streamCompressed;
			Tree = tree;
			SimisProvider = simisProvider.GetForPath(fileName);
		}

		void ReadStream(Stream stream, out SimisFormat simisFormat, out SimisStreamFormat streamFormat, out bool streamCompressed, out SimisTreeNode tree) {
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
			simisFormat = reader.SimisFormat;
			streamFormat = reader.StreamFormat;
			streamCompressed = reader.StreamCompressed;
			tree = new SimisTreeNode("<root>", "", rootBlock.Value);
		}

		/// <summary>
		/// Writes the <see cref="SimisTreeNode"/> tree to <see cref="FileName"/>.
		/// </summary>
		public void Write() {
			if (String.IsNullOrEmpty(FileName)) throw new InvalidOperationException("Cannot write to file without a filename.");
			try {
				using (var fileStream = File.Create(FileName)) {
					Write(fileStream);
				}
			} catch (Exception e) {
				throw new FileException(FileName, e);
			}
		}

		/// <summary>
		/// Writes the <see cref="SimisTreeNode"/> tree to any writable <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to write to.</param>
		public void Write(Stream stream) {
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
