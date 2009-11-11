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
	public class SimisTreeNode : ReadOnlyCollection<SimisTreeNode>
	{
		public string Type { get; private set; }
		public string Name { get; private set; }
		int Counter;
		static int GlobalCounter;

		public SimisTreeNode(string type, string name)
			: this(type, name, new List<SimisTreeNode>()) {
		}

		SimisTreeNode(string type, string name, IList<SimisTreeNode> children)
			: base(children) {
			Type = type;
			Name = name;
			Counter = ++GlobalCounter;
		}

		public override string ToString() {
			return "<" + Type + (Name.Length > 0 ? " \"" + Name + "\"" : "") + " #" + Counter + ">" + String.Join(", ", this.Select<SimisTreeNode, string>(n => n.ToString()).ToArray()) + "</" + Type + ">";
		}

		public bool EqualsByValue(object obj) {
			if ((obj == null) || (GetType() != obj.GetType())) return false;
			var stn = obj as SimisTreeNode;
			return (Type == stn.Type) && (Name == stn.Name);
		}

		public SimisTreeNode this[string type] {
			get {
				for (var i = 0; i < Count; i++) {
					if (this[i] is SimisTreeNodeValue) {
						if (this[i].Name == type) return this[i];
					} else {
						if (this[i].Type == type) return this[i];
					}
				}
				throw new ArgumentException("No children of the given type (or name for values) were found.", "type");
			}
		}

		internal bool HasChild(SimisTreeNode child) {
			return FindChildIndex(child) >= 0;
		}

		int FindChildIndex(SimisTreeNode child) {
			if (child == null) {
				return -1;
			}
			for (var i = 0; i < Count; i++) {
				if (this[i] == child) return i;
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
			var childIndex = FindChildIndex(path[pathStart]);
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
			var index = FindChildIndex(before);
			if (index == -1) throw new InvalidDataException("Cannot InsertChild before node which is not a child of this node.");
			return InsertChild(child, index);
		}

		SimisTreeNode InsertChild(SimisTreeNode child, int index) {
			var newChildren = new SimisTreeNode[Count + 1];
			for (var i = 0; i < Count; i++) {
				newChildren[i + (i >= index ? 1 : 0)] = this[i];
			}
			newChildren[index] = child;
			return new SimisTreeNode(Type, Name, newChildren);
		}

		public SimisTreeNode ReplaceChild(SimisTreeNode child, SimisTreeNode oldChild) {
			var index = FindChildIndex(oldChild);
			if (index == -1) throw new InvalidDataException("Cannot ReplaceChild a node which is not a child of this node.");
			return ReplaceChild(child, index);
		}

		SimisTreeNode ReplaceChild(SimisTreeNode child, int index) {
			var newChildren = new SimisTreeNode[Count];
			for (var i = 0; i < Count; i++) {
				newChildren[i] = (i == index ? child : this[i]);
			}
			return new SimisTreeNode(Type, Name, newChildren);
		}

		public SimisTreeNode RemoveChild(SimisTreeNode child) {
			var index = FindChildIndex(child);
			if (index == -1) throw new InvalidDataException("Cannot RemoveChild a node which is not a child of this node.");
			return RemoveChild(index);
		}

		SimisTreeNode RemoveChild(int index) {
			var newChildren = new SimisTreeNode[Count - 1];
			for (var i = 0; i < Count; i++) {
				if (i != index) newChildren[i - (i >= index ? 1 : 0)] = this[i];
			}
			return new SimisTreeNode(Type, Name, newChildren);
		}

		public virtual T ToValue<T>() {
			throw new NotImplementedException();
		}
	}

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
			if (Value is T) return (T)Value;
			throw new InvalidCastException("This SimisTreeNodeValue is not a " + typeof(T));
		}
	}

	public class SimisTreeNodeValueInteger : SimisTreeNodeValue
	{
		public SimisTreeNodeValueInteger(string type, string name, long value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((long)Value).ToString();
		}
	}

	public class SimisTreeNodeValueFloat : SimisTreeNodeValue
	{
		public SimisTreeNodeValueFloat(string type, string name, double value)
			: base(type, name, value) {
		}

		public override string ToString() {
			return ((double)Value).ToString("G6");
		}
	}

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
