//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using JGR.Grammar;

namespace JGR.IO.Parser
{
	public class SimisFile
	{
		public string Filename { get; set; }
		public List<SimisBlock> Roots;
		public SimisStreamFormat StreamFormat;
		public bool StreamCompressed;
		public string SimisFormat;
		protected SimisProvider SimisProvider;

		public SimisFile(string filename, SimisProvider provider) {
			Filename = filename;
			Roots = new List<SimisBlock>();
			SimisProvider = provider;
		}

		public void ReadFile() {
			try {
				using (var fileStream = File.OpenRead(Filename)) {
					var reader = new SimisReader(new BufferedInMemoryStream(fileStream), SimisProvider);

					var blockStack = new Stack<SimisBlock>();
					while (!reader.EndOfStream) {
						var token = reader.ReadToken();
						var key = (blockStack.Count > 0 ? blockStack.Peek().Key + "." : "");
						var name = "";
						if ((reader.BNFState != null) && (reader.BNFState.State != null) && (reader.BNFState.State.Op is NamedReferenceOperator)) {
							name = ((NamedReferenceOperator)reader.BNFState.State.Op).Name;
						}
						key += (token.Kind == SimisTokenKind.Block ? token.Type : name);

						switch (token.Kind) {
							case SimisTokenKind.Block:
								var block = new SimisBlock(this, key, token.Type, token.String);
								if (blockStack.Count == 0) {
									Roots.Add(block);
								} else {
									blockStack.Peek().Nodes.Add(block);
								}
								blockStack.Push(block);
								break;
							case SimisTokenKind.BlockBegin:
								break;
							case SimisTokenKind.BlockEnd:
								blockStack.Pop();
								break;
							case SimisTokenKind.String:
								blockStack.Peek().Nodes.Add(new SimisBlockValueString(this, key, token.Type, name, token.String));
								break;
							case SimisTokenKind.Integer:
								blockStack.Peek().Nodes.Add(new SimisBlockValueInteger(this, key, token.Type, name, token.Integer));
								break;
							case SimisTokenKind.Float:
								blockStack.Peek().Nodes.Add(new SimisBlockValueFloat(this, key, token.Type, name, token.Float));
								break;
						}
					}

					// We need to do this AFTER reading the file, otherwise they'll not be correct.
					StreamFormat = reader.StreamFormat;
					StreamCompressed = reader.StreamCompressed;
					SimisFormat = reader.SimisFormat;
				}
			} catch (ReaderException e) {
				throw new FileException(Filename, e);
			}
		}

		public void WriteFile() {
			try {
				using (var fileStream = File.Create(Filename)) {
					var writer = new SimisWriter(fileStream, SimisProvider, StreamFormat, StreamCompressed, SimisFormat);
					foreach (var root in Roots) {
						WriteBlock(writer, root);
					}
				}
			} catch (Exception e) {
				throw new FileException(Filename, e);
			}
		}

		private void WriteBlock(SimisWriter writer, SimisBlock block) {
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Block, Type = block.Type, String = block.Name });
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockBegin });
			foreach (var child in block.Nodes) {
				if (child is SimisBlockValueInteger) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Integer, Integer = (long)((SimisBlockValueInteger)child).Value });
				} else if (child is SimisBlockValueFloat) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.Float, Float = (double)((SimisBlockValueFloat)child).Value });
				} else if (child is SimisBlockValueString) {
					writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.String, String = (string)((SimisBlockValueString)child).Value });
				} else {
					WriteBlock(writer, child);
				}
			}
			writer.WriteToken(new SimisToken() { Kind = SimisTokenKind.BlockEnd });
		}
	}

	public class SimisBlock
	{
		public readonly SimisFile File;
		public readonly string Key;
		public string Type;
		public string Name;
		public readonly List<SimisBlock> Nodes;

		public SimisBlock(SimisFile file, string key, string type, string name) {
			File = file;
			Key = key;
			Type = type;
			Name = name;
			Nodes = new List<SimisBlock>();
		}
	}

	public class SimisBlockValue : SimisBlock
	{
		protected object _value;
		protected SimisBlockValue(SimisFile file, string key, string type, string name, object value)
			: base(file, key, type, name) {
			_value = value;
		}

		public object Value {
			get {
				return _value;
			}
			set {
				_value = value;
			}
		}
	}

	public class SimisBlockValueInteger : SimisBlockValue
	{
		public SimisBlockValueInteger(SimisFile file, string key, string type, string name, long value)
			: base(file, key, type, name, value) {
		}

		public override string ToString() {
			return ((long)_value).ToString();
		}
	}

	public class SimisBlockValueFloat : SimisBlockValue
	{
		public SimisBlockValueFloat(SimisFile file, string key, string type, string name, double value)
			: base(file, key, type, name, value) {
		}

		public override string ToString() {
			return ((double)_value).ToString("G6");
		}
	}

	public class SimisBlockValueString : SimisBlockValue
	{
		public SimisBlockValueString(SimisFile file, string key, string type, string name, string value)
			: base(file, key, type, name, value) {
		}

		public override string ToString() {
			return (string)_value;
		}
	}
}
