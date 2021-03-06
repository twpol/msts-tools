﻿//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Jgr.IO.Parser {
	/// <summary>
	/// Represents Simis data as a tree of immutable nodes with values.
	/// </summary>
	[Immutable]
	[DebuggerDisplay("{ToString()}")]
	public class SimisTreeNode : ReadOnlyCollection<SimisTreeNode>, IDataTreeNode {
		readonly string _type;
		readonly string _name;

		public string Type { get { return _type; } }
		public string Name { get { return _name; } } 

		/// <summary>
		/// Constructs a new node with a given <see cref="Type"/>, <see cref="Name"/> and no children.
		/// </summary>
		/// <param name="type">The string identifier for this node.</param>
		/// <param name="name">An optional name for this node.</param>
		public SimisTreeNode(string type, string name)
			: this(type, name, new SimisTreeNode[0]) {
		}

		/// <summary>
		/// Constructs a new node with a given <see cref="Type"/>, <see cref="Name"/> and collection of children.
		/// </summary>
		/// <param name="type">The string identifier for this node.</param>
		/// <param name="name">An optional name for this node.</param>
		/// <param name="children">A collection of children for this node.</param>
		public SimisTreeNode(string type, string name, IList<SimisTreeNode> children)
			: base(children) {
			_type = type;
			_name = name;
		}

		public override string ToString() {
			return "<" + Type + (_name.Length > 0 ? " \"" + _name + "\"" : "") + ">" + String.Join(", ", this.Select(n => n.ToString()).ToArray()) + "</" + _type + ">";
		}

		/// <summary>
		/// Compares two nodes using only their <see cref="Type"/> and <see cref="Name"/>.
		/// </summary>
		/// <param name="value">Another <see cref="SimisTreeNode"/> to compare with.</param>
		/// <returns><c>true</c> for matching nodes, <c>false</c> otherwise.</returns>
		public bool EqualsByValue(object value) {
			if ((value == null) || (GetType() != value.GetType())) return false;
			var stn = value as SimisTreeNode;
			return (_type == stn.Type) && (_name == stn._name);
		}

		/// <summary>
		/// Finds and returns the first child with a matching <see cref="Type"/> (for blocks) or <see cref="Name"/> (for values).
		/// </summary>
		/// <param name="type">The <see cref="Type"/> (for blocks) or <see cref="Name"/> (for values) to find in the children.</param>
		/// <returns>The first <see cref="SimisTreeNode"/> child with a matching <see cref="Type"/> (for blocks) or <see cref="Name"/> (for values).</returns>
		/// <exception cref="ArgumentException">If not children have a matching <see cref="Type"/> (for blocks) or <see cref="Name"/> (for values).</exception>
		public SimisTreeNode this[string type] {
			get {
				foreach (var child in this) {
					if (child is SimisTreeNodeValue) {
						if (child._name.Equals(type, StringComparison.InvariantCultureIgnoreCase)) return child;
					} else {
						if (child._type.Equals(type, StringComparison.InvariantCultureIgnoreCase)) return child;
					}
				}
				throw new ArgumentException("No children of the given type (or name for values) were found.", "type");
			}
		}

		public bool Contains(string type) {
			foreach (var child in this) {
				if (child is SimisTreeNodeValue) {
					if (child._name.Equals(type, StringComparison.InvariantCultureIgnoreCase)) return true;
				} else {
					if (child._type.Equals(type, StringComparison.InvariantCultureIgnoreCase)) return true;
				}
			}
			return false;
		}

		public SimisTreeNode GetNextSibling(SimisTreeNode child) {
			for (var i = 0; i < Count; i++) {
				if (this[i] == child) {
					return i == Count - 1 ? null : this[i + 1];
				}
			}
			throw new ArgumentException("child is not a child of this node.", "child");
		}

		public SimisTreeNode GetPreviousSibling(SimisTreeNode child) {
			for (var i = 0; i < Count; i++) {
				if (this[i] == child) {
					return i == 0 ? null : this[i - 1];
				}
			}
			throw new ArgumentException("child is not a child of this node.", "child");
		}

		public SimisTreeNode GetFirstChild() {
			return Count == 0 ? null : this[0];
		}

		public SimisTreeNode GetLastChild() {
			return Count == 0 ? null : this[Count - 1];
		}

		int LastIndexOf(SimisTreeNode node) {
			for (var i = Count - 1; i >= 0; i--) {
				if (this[i] == node) return i;
			}
			return -1;
		}

		/// <summary>
		/// Creates a new <see cref="SimisTreeNode"/> with a different <see cref="Name"/>.
		/// </summary>
		/// <param name="name">The new name for the node.</param>
		/// <returns>The new <see cref="SimisTreeNode"/>.</returns>
		public SimisTreeNode Rename(string name) {
			return new SimisTreeNode(_type, name, this);
		}

		/// <summary>
		/// Creates a new <see cref="SimisTreeNode"/> with an extra child appended.
		/// </summary>
		/// <param name="child">The child to append.</param>
		/// <returns>The new <see cref="SimisTreeNode"/>.</returns>
		public SimisTreeNode AppendChild(SimisTreeNode child) {
			return InsertChild(child, Count);
		}

		/// <summary>
		/// Creates a new <see cref="SimisTreeNode"/> with an extra child inserted.
		/// </summary>
		/// <param name="child">The child to insert.</param>
		/// <param name="before">The existing child to insert before.</param>
		/// <returns>The new <see cref="SimisTreeNode"/>.</returns>
		public SimisTreeNode InsertChild(SimisTreeNode child, SimisTreeNode before) {
			if (before == null) {
				return AppendChild(child);
			}
			var index = LastIndexOf(before);
			if (index == -1) throw new InvalidDataException("Cannot InsertChild before node which is not a child of this node.");
			return InsertChild(child, index);
		}

		SimisTreeNode InsertChild(SimisTreeNode child, int index) {
			var newChildren = new List<SimisTreeNode>(this);
			newChildren.Insert(index, child);
			return new SimisTreeNode(_type, _name, newChildren);
		}

		/// <summary>
		/// Creates a new <see cref="SimisTreeNode"/> with a child replaced by another.
		/// </summary>
		/// <param name="child">The new child to insert.</param>
		/// <param name="oldChild">The child to be replaced.</param>
		/// <returns>The new <see cref="SimisTreeNode"/>.</returns>
		public SimisTreeNode ReplaceChild(SimisTreeNode child, SimisTreeNode oldChild) {
			var index = LastIndexOf(oldChild);
			if (index == -1) throw new InvalidDataException("Cannot ReplaceChild a node which is not a child of this node.");
			return ReplaceChild(child, index);
		}

		SimisTreeNode ReplaceChild(SimisTreeNode child, int index) {
			var newChildren = new List<SimisTreeNode>(this);
			newChildren[index] = child;
			return new SimisTreeNode(_type, _name, newChildren);
		}

		/// <summary>
		/// Creates a new <see cref="SimisTreeNode"/> with a child removed.
		/// </summary>
		/// <param name="child">The child to remove.</param>
		/// <returns>The new <see cref="SimisTreeNode"/>.</returns>
		public SimisTreeNode RemoveChild(SimisTreeNode child) {
			var index = LastIndexOf(child);
			if (index == -1) throw new InvalidDataException("Cannot RemoveChild a node which is not a child of this node.");
			return RemoveChild(index);
		}

		SimisTreeNode RemoveChild(int index) {
			var newChildren = new List<SimisTreeNode>(this);
			newChildren.RemoveAt(index);
			return new SimisTreeNode(_type, _name, newChildren);
		}

		/// <summary>
		/// Gets the value from value nodes.
		/// </summary>
		/// <typeparam name="T">The expected <see cref="Type"/> (e.g. <c>uint</c>, <c>string</c>) of the value.</typeparam>
		/// <returns>The value of the node, cast to <see cref="T"/>.</returns>
		public virtual T ToValue<T>() {
			throw new NotImplementedException();
		}

		#region IDataTreeNode Members

		public bool HasChildNodes() {
			return !(this is SimisTreeNodeValue);
		}

		public IDataTreeNode GetChildNode(object name) {
			if (name is string) {
				return this[(string)name];
			}
			if (name is SimisTreeNode) {
				if (!Contains((SimisTreeNode)name)) throw new InvalidDataException("Cannot GetChildNode a node which is not a child of this node.");
				return (IDataTreeNode)name;
			}
			return this[(int)name];
		}

		public IDataTreeNode AppendChildNode(IDataTreeNode child) {
			return AppendChild((SimisTreeNode)child);
		}

		public IDataTreeNode InsertChildNode(IDataTreeNode child, IDataTreeNode before) {
			return InsertChild((SimisTreeNode)child, (SimisTreeNode)before);
		}

		public IDataTreeNode ReplaceChildNode(IDataTreeNode child, object name, IDataTreeNode oldChild) {
			return ReplaceChild((SimisTreeNode)child, (SimisTreeNode)oldChild);
		}

		public IDataTreeNode RemoveChildNode(IDataTreeNode child) {
			return RemoveChild((SimisTreeNode)child);
		}

		#endregion
	}

	/// <summary>
	/// Base class for all value nodes in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public abstract class SimisTreeNodeValue : SimisTreeNode {
		public static Dictionary<string, Type> NodeTypes = InitNodeTypes();
		static Dictionary<string, Type> InitNodeTypes() {
			var d = new Dictionary<string, Type>();
			d.Add("string", typeof(SimisTreeNodeValueString));
			d.Add("uint", typeof(SimisTreeNodeValueIntegerUnsigned));
			d.Add("sint", typeof(SimisTreeNodeValueIntegerSigned));
			d.Add("dword", typeof(SimisTreeNodeValueIntegerDWord));
			d.Add("word", typeof(SimisTreeNodeValueIntegerWord));
			d.Add("byte", typeof(SimisTreeNodeValueIntegerByte));
			d.Add("float", typeof(SimisTreeNodeValueFloat));
			return d;
		}

		public object Value { get; private set; }

		internal SimisTreeNodeValue(string type, string name, object value)
			: base(type, name) {
			Value = value;
		}

		public override string ToString() {
			return Value.ToString();
		}

		public override T ToValue<T>() {
			return (T)Value;
		}
	}

	/// <summary>
	/// Represents all integer values (<c>uint</c>, <c>sint</c>, etc.) in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public abstract class SimisTreeNodeValueInteger : SimisTreeNodeValue {
		internal SimisTreeNodeValueInteger(string type, string name, object value)
			: base(type, name, value) {
		}
	}

	/// <summary>
	/// Represents an unsigned integer (<see cref="uint"/>) value in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public class SimisTreeNodeValueIntegerUnsigned : SimisTreeNodeValueInteger {
		public SimisTreeNodeValueIntegerUnsigned(string type, string name)
			: this(type, name, 0) {
		}

		public SimisTreeNodeValueIntegerUnsigned(string type, string name, uint value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((uint)Value).ToString(CultureInfo.CurrentCulture);
		}
	}

	/// <summary>
	/// Represents a signed integer (<see cref="int"/>) value in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public class SimisTreeNodeValueIntegerSigned : SimisTreeNodeValueInteger {
		public SimisTreeNodeValueIntegerSigned(string type, string name)
			: this(type, name, 0) {
		}

		public SimisTreeNodeValueIntegerSigned(string type, string name, int value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((int)Value).ToString(CultureInfo.CurrentCulture);
		}
	}

	/// <summary>
	/// Represents an unsigned double-word integer (<see cref="uint"/>) value in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public class SimisTreeNodeValueIntegerDWord : SimisTreeNodeValueInteger {
		public SimisTreeNodeValueIntegerDWord(string type, string name)
			: this(type, name, 0) {
		}

		public SimisTreeNodeValueIntegerDWord(string type, string name, uint value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((uint)Value).ToString(CultureInfo.CurrentCulture);
		}
	}

	/// <summary>
	/// Represents an unsigned single-word integer (<see cref="ushort"/>) value in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public class SimisTreeNodeValueIntegerWord : SimisTreeNodeValueInteger {
		public SimisTreeNodeValueIntegerWord(string type, string name)
			: this(type, name, 0) {
		}

		public SimisTreeNodeValueIntegerWord(string type, string name, ushort value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((ushort)Value).ToString(CultureInfo.CurrentCulture);
		}
	}

	/// <summary>
	/// Represents an unsigned single-byte integer (<see cref="byte"/>) value in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public class SimisTreeNodeValueIntegerByte : SimisTreeNodeValueInteger {
		public SimisTreeNodeValueIntegerByte(string type, string name)
			: this(type, name, 0) {
		}

		public SimisTreeNodeValueIntegerByte(string type, string name, byte value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((byte)Value).ToString(CultureInfo.CurrentCulture);
		}
	}

	/// <summary>
	/// Represents a floating point (<see cref="float"/>) value in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public class SimisTreeNodeValueFloat : SimisTreeNodeValue {
		public SimisTreeNodeValueFloat(string type, string name)
			: this(type, name, 0) {
		}

		public SimisTreeNodeValueFloat(string type, string name, float value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((float)Value).ToString("G6", CultureInfo.CurrentCulture);
		}
	}

	/// <summary>
	/// Represents a string (<see cref="string"/>) value in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public class SimisTreeNodeValueString : SimisTreeNodeValue {
		public SimisTreeNodeValueString(string type, string name)
			: this(type, name, "") {
		}

		public SimisTreeNodeValueString(string type, string name, string value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return (string)Value;
		}
	}
}
