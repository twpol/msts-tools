//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Jgr.IO.Parser
{
	[Immutable]
	public class SimisTreeNode : ReadOnlyCollection<SimisTreeNode>
	{
		public string Type { get; private set; }
		public string Name { get; private set; }

		public SimisTreeNode(string type, string name)
			: this(type, name, new SimisTreeNode[0]) {
		}

		SimisTreeNode(string type, string name, IList<SimisTreeNode> children)
			: base(children) {
			Type = type;
			Name = name;
		}

		public override string ToString() {
			return "<" + Type + (Name.Length > 0 ? " \"" + Name + "\"" : "") + ">" + String.Join(", ", this.Select<SimisTreeNode, string>(n => n.ToString()).ToArray()) + "</" + Type + ">";
		}

		public bool EqualsByValue(object obj) {
			if ((obj == null) || (GetType() != obj.GetType())) return false;
			var stn = obj as SimisTreeNode;
			return (Type == stn.Type) && (Name == stn.Name);
		}

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

		public SimisTreeNode Rename(string name) {
			return new SimisTreeNode(Type, name, this);
		}

		public SimisTreeNode AppendChild(SimisTreeNode child) {
			return InsertChild(child, Count);
		}

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

		public virtual T ToValue<T>() {
			throw new NotImplementedException();
		}
	}

	[Immutable]
	public abstract class SimisTreeNodeValue : SimisTreeNode
	{
		public object Value { get; private set; }

		protected SimisTreeNodeValue(string type, string name, object value)
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

	[Immutable]
	public abstract class SimisTreeNodeValueInteger : SimisTreeNodeValue
	{
		protected SimisTreeNodeValueInteger(string type, string name, object value)
			: base(type, name, value) {
		}
	}

	[Immutable]
	public class SimisTreeNodeValueIntegerUnsigned : SimisTreeNodeValueInteger {
		public SimisTreeNodeValueIntegerUnsigned(string type, string name, uint value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((uint)Value).ToString();
		}
	}

	[Immutable]
	public class SimisTreeNodeValueIntegerSigned : SimisTreeNodeValueInteger {
		public SimisTreeNodeValueIntegerSigned(string type, string name, int value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((int)Value).ToString();
		}
	}

	[Immutable]
	public class SimisTreeNodeValueIntegerDWord : SimisTreeNodeValueInteger {
		public SimisTreeNodeValueIntegerDWord(string type, string name, uint value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((uint)Value).ToString();
		}
	}

	[Immutable]
	public class SimisTreeNodeValueFloat : SimisTreeNodeValue
	{
		public SimisTreeNodeValueFloat(string type, string name, float value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((float)Value).ToString("G6");
		}
	}

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
