using System;
using System.Collections.Generic;
using Jgr.IO.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for SimisTreeNodeTest and is intended
	///to contain all SimisTreeNodeTest Unit Tests
	///</summary>
	[TestClass()]
	public class SimisTreeNodeTest {
		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext {
			get {
				return testContextInstance;
			}
			set {
				testContextInstance = value;
			}
		}

		/// <summary>
		///A test for SimisTreeNode Constructor
		///</summary>
		[TestMethod]
		public void _ctorTest1() {
			SimisTreeNode target = new SimisTreeNode("type", "name", new SimisTreeNode[0]);
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("name", target.Name);
			Assert.AreEqual(0, target.Count);
		}

		/// <summary>
		///A test for SimisTreeNode Constructor
		///</summary>
		[TestMethod]
		public void _ctorTest() {
			SimisTreeNode target = new SimisTreeNode("type", "name");
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("name", target.Name);
			Assert.AreEqual(0, target.Count);
		}

		/// <summary>
		///A test for AppendChild
		///</summary>
		[TestMethod]
		public void AppendChildTest() {
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("type", "name");
			target = target.AppendChild(child1);
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("name", target.Name);
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual(child1.Type, target[0].Type);
			Assert.AreEqual(child1.Type, target[0].Name);
			Assert.AreEqual(child1.Count, target[0].Count);
			target = target.AppendChild(child2);
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("name", target.Name);
			Assert.AreEqual(2, target.Count);
			Assert.AreEqual(child1.Type, target[0].Type);
			Assert.AreEqual(child1.Type, target[0].Name);
			Assert.AreEqual(child1.Count, target[0].Count);
			Assert.AreEqual(child2.Type, target[1].Type);
			Assert.AreEqual(child2.Type, target[1].Name);
			Assert.AreEqual(child2.Count, target[1].Count);
		}

		/// <summary>
		///A test for Apply
		///</summary>
		[TestMethod]
		public void ApplyTest() {
			string type = string.Empty; // TODO: Initialize to an appropriate value
			string name = string.Empty; // TODO: Initialize to an appropriate value
			SimisTreeNode target = new SimisTreeNode(type, name); // TODO: Initialize to an appropriate value
			IList<SimisTreeNode> path = null; // TODO: Initialize to an appropriate value
			Func<SimisTreeNode, SimisTreeNode> action = null; // TODO: Initialize to an appropriate value
			SimisTreeNode expected = null; // TODO: Initialize to an appropriate value
			SimisTreeNode actual;
			actual = target.Apply(path, action);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for EqualsByValue
		///</summary>
		[TestMethod]
		public void EqualsByValueTest() {
			SimisTreeNode target = new SimisTreeNode("type", "name");
			SimisTreeNode other = new SimisTreeNode("type", "name");
			Assert.IsTrue(target.EqualsByValue(other));
			other = new SimisTreeNode("type", "name2");
			Assert.IsFalse(target.EqualsByValue(other));
			other = new SimisTreeNode("type2", "name");
			Assert.IsFalse(target.EqualsByValue(other));
		}

		/// <summary>
		///A test for InsertChild
		///</summary>
		[TestMethod]
		public void InsertChildTest() {
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("type", "name", new SimisTreeNode[] { child1 });
			target = target.InsertChild(child2, child1);
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("name", target.Name);
			Assert.AreEqual(2, target.Count);
			Assert.AreEqual(child2.Type, target[0].Type);
			Assert.AreEqual(child2.Type, target[0].Name);
			Assert.AreEqual(child2.Count, target[0].Count);
			Assert.AreEqual(child1.Type, target[1].Type);
			Assert.AreEqual(child1.Type, target[1].Name);
			Assert.AreEqual(child1.Count, target[1].Count);
		}

		/// <summary>
		///A test for RemoveChild
		///</summary>
		[TestMethod]
		public void RemoveChildTest() {
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("type", "name", new SimisTreeNode[] { child1, child2 });
			target = target.RemoveChild(child1);
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("name", target.Name);
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual(child2.Type, target[0].Type);
			Assert.AreEqual(child2.Type, target[0].Name);
			Assert.AreEqual(child2.Count, target[0].Count);
			target = new SimisTreeNode("type", "name", new SimisTreeNode[] { child1, child2 });
			target = target.RemoveChild(child2);
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("name", target.Name);
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual(child1.Type, target[0].Type);
			Assert.AreEqual(child1.Type, target[0].Name);
			Assert.AreEqual(child1.Count, target[0].Count);
		}

		/// <summary>
		///A test for Rename
		///</summary>
		[TestMethod]
		public void RenameTest() {
			SimisTreeNode target = new SimisTreeNode("type", "name", new SimisTreeNode[2]);
			target = target.Rename("newname");
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("newname", target.Name);
			Assert.AreEqual(2, target.Count);
		}

		/// <summary>
		///A test for ReplaceChild
		///</summary>
		[TestMethod]
		public void ReplaceChildTest() {
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("type", "name", new SimisTreeNode[] { child1 });
			target = target.ReplaceChild(child2, child1);
			Assert.AreEqual("type", target.Type);
			Assert.AreEqual("name", target.Name);
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual(child2.Type, target[0].Type);
			Assert.AreEqual(child2.Name, target[0].Name);
			Assert.AreEqual(child2.Count, target[0].Count);
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[TestMethod]
		public void ToStringTest() {
			string type = string.Empty; // TODO: Initialize to an appropriate value
			string name = string.Empty; // TODO: Initialize to an appropriate value
			SimisTreeNode target = new SimisTreeNode(type, name); // TODO: Initialize to an appropriate value
			string expected = string.Empty; // TODO: Initialize to an appropriate value
			string actual;
			actual = target.ToString();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ToValue
		///</summary>
		public void ToValueTestHelper<T>() {
			string type = string.Empty; // TODO: Initialize to an appropriate value
			string name = string.Empty; // TODO: Initialize to an appropriate value
			SimisTreeNode target = new SimisTreeNode(type, name); // TODO: Initialize to an appropriate value
			T expected = default(T); // TODO: Initialize to an appropriate value
			T actual;
			actual = target.ToValue<T>();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		[TestMethod]
		public void ToValueTest() {
			ToValueTestHelper<GenericParameterHelper>();
		}

		/// <summary>
		///A test for Item
		///</summary>
		[TestMethod]
		public void ItemTest() {
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("type", "name", new SimisTreeNode[] { child1, child2 });
			Assert.AreEqual(child1.Type, target["child1type"].Type);
			Assert.AreEqual(child1.Name, target["child1type"].Name);
			Assert.AreEqual(child1.Count, target["child1type"].Count);
			Assert.AreEqual(child2.Type, target["child2type"].Type);
			Assert.AreEqual(child2.Name, target["child2type"].Name);
			Assert.AreEqual(child2.Count, target["child2type"].Count);
		}
	}
}
