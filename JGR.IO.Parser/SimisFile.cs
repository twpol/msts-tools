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
		public SimisTreeNode Tree { get; set; }
		SimisProvider SimisProvider;

		public SimisFile(string fileName, SimisProvider provider) {
			FileName = fileName;
			Tree = new SimisTreeNode("<root>", "");
			SimisProvider = provider;
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

			Tree = new SimisTreeNode("<root>", "");
			var blockStack = new List<SimisTreeNode>();
			while (!reader.EndOfStream) {
				var token = reader.ReadToken();
				//var key = (blockStack.Count > 0 ? blockStack.Peek().Key + "." : "");
				var name = "";
				if ((reader.BnfState != null) && (reader.BnfState.State != null) && (reader.BnfState.State.Op is NamedReferenceOperator)) {
					name = ((NamedReferenceOperator)reader.BnfState.State.Op).Name;
				}
				//key += (token.Kind == SimisTokenKind.Block ? token.Type : name);

				switch (token.Kind) {
					case SimisTokenKind.Block:
						var block = new SimisTreeNode(token.Type, token.String);
						Tree = Tree.Apply(blockStack, node => node.AppendChild(block));
						blockStack.Add(block);
						break;
					case SimisTokenKind.BlockBegin:
						break;
					case SimisTokenKind.BlockEnd:
						blockStack.RemoveAt(blockStack.Count - 1);
						break;
					case SimisTokenKind.Integer:
						Tree = Tree.Apply(blockStack, node => node.AppendChild(new SimisTreeNodeValueInteger(token.Type, name, token.Integer)));
						break;
					case SimisTokenKind.Float:
						Tree = Tree.Apply(blockStack, node => node.AppendChild(new SimisTreeNodeValueFloat(token.Type, name, token.Float)));
						break;
					case SimisTokenKind.String:
						Tree = Tree.Apply(blockStack, node => node.AppendChild(new SimisTreeNodeValueString(token.Type, name, token.String)));
						break;
				}
			}

			// We need to do this AFTER reading the file, otherwise they'll not be correct.
			StreamFormat = reader.StreamFormat;
			StreamCompressed = reader.StreamCompressed;
			SimisFormat = reader.SimisFormat;
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
				if (child is SimisTreeNodeValueInteger) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Integer, Integer = (long)((SimisTreeNodeValueInteger)child).Value });
				} else if (child is SimisTreeNodeValueFloat) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Float, Float = (double)((SimisTreeNodeValueFloat)child).Value });
				} else if (child is SimisTreeNodeValueString) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.String, String = (string)((SimisTreeNodeValueString)child).Value });
				} else {
					WriteBlock(writer, child);
				}
			}
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockEnd });
		}
	}
}
