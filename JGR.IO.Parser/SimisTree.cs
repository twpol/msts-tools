//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Jgr.IO.Parser
{
	/// <summary>
	/// Represents Simis data as a tree of immutable nodes with values.
	/// </summary>
	[Immutable]
	public class SimisTreeNode : ReadOnlyCollection<SimisTreeNode>
	{
		public string Type { get; private set; }
		public string Name { get; private set; }

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
			Type = type;
			Name = name;
		}

		public override string ToString() {
			return "<" + Type + (Name.Length > 0 ? " \"" + Name + "\"" : "") + ">" + String.Join(", ", this.Select(n => n.ToString()).ToArray()) + "</" + Type + ">";
		}

		/// <summary>
		/// Compares two nodes using only their <see cref="Type"/> and <see cref="Name"/>.
		/// </summary>
		/// <param name="value">Another <see cref="SimisTreeNode"/> to compare with.</param>
		/// <returns><c>true</c> for matching nodes, <c>false</c> otherwise.</returns>
		public bool EqualsByValue(object value) {
			if ((value == null) || (GetType() != value.GetType())) return false;
			var stn = value as SimisTreeNode;
			return (Type == stn.Type) && (Name == stn.Name);
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
						if (child.Name == type) return child;
					} else {
						if (child.Type == type) return child;
					}
				}
				throw new ArgumentException("No children of the given type (or name for values) were found.", "type");
			}
		}

		public bool Contains(string type) {
			foreach (var child in this) {
				if (child is SimisTreeNodeValue) {
					if (child.Name == type) return true;
				} else {
					if (child.Type == type) return true;
				}
			}
			return false;
		}

		int LastIndexOf(SimisTreeNode node) {
			for (var i = Count - 1; i >= 0; i--) {
				if (this[i] == node) return i;
			}
			return -1;
		}

		public SimisTreeNode Apply(IList<SimisTreeNode> path, Func<SimisTreeNode, SimisTreeNode> action) {
			return Apply(path, 0, action);
		}

		SimisTreeNode Apply(IList<SimisTreeNode> path, int pathStart, Func<SimisTreeNode, SimisTreeNode> action) {
			if (pathStart >= path.Count()) {
				return action(this);
			}
			var childIndex = LastIndexOf(path[pathStart]);
			if (childIndex == -1) throw new InvalidDataException("Cannot find child node specified by path[" + pathStart + "].");
			var newChild = this[childIndex].Apply(path, pathStart + 1, action);
			path[pathStart] = newChild;
			return ReplaceChild(newChild, childIndex);
		}

		/// <summary>
		/// Creates a new <see cref="SimisTreeNode"/> with a different <see cref="Name"/>.
		/// </summary>
		/// <param name="name">The new name for the node.</param>
		/// <returns>The new <see cref="SimisTreeNode"/>.</returns>
		public SimisTreeNode Rename(string name) {
			return new SimisTreeNode(Type, name, this);
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
			var index = LastIndexOf(before);
			if (index == -1) throw new InvalidDataException("Cannot InsertChild before node which is not a child of this node.");
			return InsertChild(child, index);
		}

		SimisTreeNode InsertChild(SimisTreeNode child, int index) {
			var newChildren = new List<SimisTreeNode>(this);
			newChildren.Insert(index, child);
			return new SimisTreeNode(Type, Name, newChildren);
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
			return new SimisTreeNode(Type, Name, newChildren);
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
			return new SimisTreeNode(Type, Name, newChildren);
		}

		/// <summary>
		/// Gets the value from value nodes.
		/// </summary>
		/// <typeparam name="T">The expected <see cref="Type"/> (e.g. <c>uint</c>, <c>string</c>) of the value.</typeparam>
		/// <returns>The value of the node, cast to <see cref="T"/>.</returns>
		public virtual T ToValue<T>() {
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Base class for all value nodes in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public abstract class SimisTreeNodeValue : SimisTreeNode
	{
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
	public abstract class SimisTreeNodeValueInteger : SimisTreeNodeValue
	{
		internal SimisTreeNodeValueInteger(string type, string name, object value)
			: base(type, name, value) {
		}
	}

	/// <summary>
	/// Represents an unsigned integer (<see cref="uint"/>) value in a <see cref="SimisTreeNode"/> tree.
	/// </summary>
	[Immutable]
	public class SimisTreeNodeValueIntegerUnsigned : SimisTreeNodeValueInteger {
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
	public class SimisTreeNodeValueFloat : SimisTreeNodeValue
	{
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
	public class SimisTreeNodeValueString : SimisTreeNodeValue
	{
		public SimisTreeNodeValueString(string type, string name, string value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return (string)Value;
		}
	}
}
