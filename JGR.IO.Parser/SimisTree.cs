//------------------------------------------------------------------------------
// JGR.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace Jgr.IO.Parser
{
	public class SimisTree
	{
		public SimisTreeNode Root { get; private set; }

		public SimisTree() {
			Root = new SimisTreeNode("", "");
		}

		[Conditional("DEBUG")]
		void VerifyRoute(IEnumerable<SimisTreeNode> route, SimisTreeNode extra) {
			VerifyRoute(route.Concat(new SimisTreeNode[] { extra }));
		}

		[Conditional("DEBUG")]
		void VerifyRoute(IEnumerable<SimisTreeNode> route) {
			Debug.Assert(route.Count() != 0, "Route is empty.");
			Debug.Assert(route.First() == Root, "Node is not the root node. Route is invalid.");
			SimisTreeNode last = null;
			foreach (var node in route) {
				if (null != last) {
					Debug.Assert(last.HasChild(node), "Node is not a child of previous node. Route is invalid.");
				}
				last = node;
			}
		}

		IEnumerable<SimisTreeNode> UpdateRoute(IEnumerable<SimisTreeNode> route, Func<SimisTreeNode, SimisTreeNode> update) {
			var newRoute = route.ToArray();
			var oldRoute = route.ToArray();
			newRoute[newRoute.Length - 1] = update(newRoute[newRoute.Length - 1]);
			for (var i = newRoute.Length - 1; i > 0; i--) {
				newRoute[i - 1] = newRoute[i - 1].ReplaceChild(newRoute[i], oldRoute[i]);
			}
			return newRoute;
		}

		public IEnumerable<SimisTreeNode> AppendChild(IEnumerable<SimisTreeNode> route, SimisTreeNode child) {
			VerifyRoute(route);
			//Debug.WriteLine(String.Join("\\", route.Select<SimisTreeNode, string>(n => n.Type).ToArray()) + " [AppendChild] " + child.ToString());
			route = UpdateRoute(route, node => node.AppendChild(child));
			Root = route.First();
			return route;
		}

		public IEnumerable<SimisTreeNode> InsertChild(IEnumerable<SimisTreeNode> route, SimisTreeNode child, SimisTreeNode before) {
			VerifyRoute(route, before);
			//Debug.WriteLine(String.Join("\\", route.Select<SimisTreeNode, string>(n => n.Type).ToArray()) + " [InsertChild] " + child.ToString() + " [before] " + before.ToString());
			route = UpdateRoute(route, node => node.InsertChild(child, before));
			Root = route.First();
			return route;
		}

		public IEnumerable<SimisTreeNode> ReplaceChild(IEnumerable<SimisTreeNode> route, SimisTreeNode child, SimisTreeNode oldChild) {
			VerifyRoute(route, oldChild);
			//Debug.WriteLine(String.Join("\\", route.Select<SimisTreeNode, string>(n => n.Type).ToArray()) + " [ReplaceChild] " + child.ToString() + " [from] " + oldChild.ToString());
			route = UpdateRoute(route, node => node.ReplaceChild(child, oldChild));
			Root = route.First();
			return route;
		}

		public IEnumerable<SimisTreeNode> RemoveChild(IEnumerable<SimisTreeNode> route, SimisTreeNode child) {
			VerifyRoute(route, child);
			//Debug.WriteLine(String.Join("\\", route.Select<SimisTreeNode, string>(n => n.Type).ToArray()) + " [RemoveChild] " + child.ToString());
			route = UpdateRoute(route, node => node.RemoveChild(child));
			Root = route.First();
			return route;
		}
	}

	public class SimisTreeNode
	{
		public string Type { get; private set; }
		public string Name { get; private set; }
		public SimisTreeNode[] Children { get; private set; }
		int Counter;
		static int GlobalCounter;

		public SimisTreeNode(string type, string name)
			: this(type, name, new SimisTreeNode[] { }) {
		}

		SimisTreeNode(string type, string name, SimisTreeNode[] children) {
			Type = type;
			Name = name;
			Children = children;
			Counter = ++GlobalCounter;
		}

		public override string ToString() {
			return "<" + Type + (Name.Length > 0 ? " \"" + Name + "\"" : "") + " #" + Counter + ">" + String.Join(", ", Children.Select<SimisTreeNode, string>(n => n.ToString()).ToArray()) + "</" + Type + ">";
		}

		public bool EqualsByValue(object obj) {
			if ((obj == null) || (GetType() != obj.GetType())) return false;
			var stn = obj as SimisTreeNode;
			return (Type == stn.Type) && (Name == stn.Name);
		}

		int FindChildIndex(SimisTreeNode child) {
			for (var i = 0; i < Children.Length; i++) {
				if (Children[i] == child) return i;
			}
			return -1;
		}

		public SimisTreeNode Rename(string name) {
			return new SimisTreeNode(Type, name, Children);
		}

		public bool HasChild(SimisTreeNode child) {
			return FindChildIndex(child) >= 0;
		}

		public SimisTreeNode AppendChild(SimisTreeNode child) {
			return InsertChild(child, Children.Length);
		}

		public SimisTreeNode InsertChild(SimisTreeNode child, SimisTreeNode before) {
			var index = FindChildIndex(before);
			if (index == -1) throw new InvalidDataException("Cannot InsertChild before node which is not a child of this node.");
			return InsertChild(child, index);
		}

		public SimisTreeNode InsertChild(SimisTreeNode child, int index) {
			var newChildren = new SimisTreeNode[Children.Length + 1];
			for (var i = 0; i < Children.Length; i++) {
				newChildren[i + (i >= index ? 1 : 0)] = Children[i];
			}
			newChildren[index] = child;
			return new SimisTreeNode(Type, Name, newChildren);
		}

		public SimisTreeNode ReplaceChild(SimisTreeNode child, SimisTreeNode oldChild) {
			var index = FindChildIndex(oldChild);
			if (index == -1) throw new InvalidDataException("Cannot ReplaceChild a node which is not a child of this node.");
			return ReplaceChild(child, index);
		}

		public SimisTreeNode ReplaceChild(SimisTreeNode child, int index) {
			var newChildren = new SimisTreeNode[Children.Length];
			for (var i = 0; i < Children.Length; i++) {
				newChildren[i] = (i == index ? child : Children[i]);
			}
			return new SimisTreeNode(Type, Name, newChildren);
		}

		public SimisTreeNode RemoveChild(SimisTreeNode child) {
			var index = FindChildIndex(child);
			if (index == -1) throw new InvalidDataException("Cannot RemoveChild a node which is not a child of this node.");
			return RemoveChild(index);
		}

		public SimisTreeNode RemoveChild(int index) {
			var newChildren = new SimisTreeNode[Children.Length - 1];
			for (var i = 0; i < Children.Length; i++) {
				if (i != index) newChildren[i - (i >= index ? 1 : 0)] = Children[i];
			}
			return new SimisTreeNode(Type, Name, newChildren);
		}
	}

	public class SimisTreeNodeValue : SimisTreeNode
	{
		public object Value { get; private set; }

		protected SimisTreeNodeValue(string type, string name, object value)
			: base(type, name) {
			Value = value;
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
