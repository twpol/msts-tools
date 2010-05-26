//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Jgr.IO.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for SimisTreeNodeTest and is intended
	///to contain all SimisTreeNodeTest Unit Tests
	///</summary>
	[TestClass]
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
		public void ConstructorTest1() {
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname", new SimisTreeNode[0]);
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
			Assert.AreEqual(0, target.Count);
		}

		/// <summary>
		///A test for SimisTreeNode Constructor
		///</summary>
		[TestMethod]
		public void ConstructorTest() {
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname");
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
			Assert.AreEqual(0, target.Count);
		}

		/// <summary>
		///A test for AppendChild
		///</summary>
		[TestMethod]
		public void AppendChildTest() {
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname");
			target = target.AppendChild(child1);
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual(child1.Type, target[0].Type);
			Assert.AreEqual(child1.Name, target[0].Name);
			Assert.AreEqual(child1.Count, target[0].Count);
			target = target.AppendChild(child2);
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
			Assert.AreEqual(2, target.Count);
			Assert.AreEqual(child1.Type, target[0].Type);
			Assert.AreEqual(child1.Name, target[0].Name);
			Assert.AreEqual(child1.Count, target[0].Count);
			Assert.AreEqual(child2.Type, target[1].Type);
			Assert.AreEqual(child2.Name, target[1].Name);
			Assert.AreEqual(child2.Count, target[1].Count);
		}

		/// <summary>
		///A test for Apply
		///</summary>
		[TestMethod]
		public void ApplyTest() {
			SimisTreeNode child3 = new SimisTreeNode("child3type", "child3name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name", new[] { child2 });
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname", new[] { child1 });
			target = target.Apply(new[] { child1, child2 }, n => n.AppendChild(child3));
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual(child1.Type, target[0].Type);
			Assert.AreEqual(child1.Name, target[0].Name);
			Assert.AreEqual(1, target[0].Count);
			Assert.AreEqual(child2.Type, target[0][0].Type);
			Assert.AreEqual(child2.Name, target[0][0].Name);
			Assert.AreEqual(1, target[0][0].Count);
			Assert.AreEqual(child3.Type, target[0][0][0].Type);
			Assert.AreEqual(child3.Name, target[0][0][0].Name);
			Assert.AreEqual(0, target[0][0][0].Count);
		}

		/// <summary>
		///A test for EqualsByValue
		///</summary>
		[TestMethod]
		public void EqualsByValueTest() {
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname");
			SimisTreeNode other = new SimisTreeNode("targettype", "targetname");
			Assert.IsTrue(target.EqualsByValue(other));
			other = new SimisTreeNode("targettype", "targetname2");
			Assert.IsFalse(target.EqualsByValue(other));
			other = new SimisTreeNode("targettype2", "targetname");
			Assert.IsFalse(target.EqualsByValue(other));
		}

		/// <summary>
		///A test for InsertChild
		///</summary>
		[TestMethod]
		public void InsertChildTest() {
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname", new SimisTreeNode[] { child1 });
			target = target.InsertChild(child2, child1);
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
			Assert.AreEqual(2, target.Count);
			Assert.AreEqual(child2.Type, target[0].Type);
			Assert.AreEqual(child2.Name, target[0].Name);
			Assert.AreEqual(child2.Count, target[0].Count);
			Assert.AreEqual(child1.Type, target[1].Type);
			Assert.AreEqual(child1.Name, target[1].Name);
			Assert.AreEqual(child1.Count, target[1].Count);
		}

		/// <summary>
		///A test for RemoveChild
		///</summary>
		[TestMethod]
		public void RemoveChildTest() {
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname", new SimisTreeNode[] { child1, child2 });
			target = target.RemoveChild(child1);
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual(child2.Type, target[0].Type);
			Assert.AreEqual(child2.Name, target[0].Name);
			Assert.AreEqual(child2.Count, target[0].Count);
			target = new SimisTreeNode("targettype", "targetname", new SimisTreeNode[] { child1, child2 });
			target = target.RemoveChild(child2);
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
			Assert.AreEqual(1, target.Count);
			Assert.AreEqual(child1.Type, target[0].Type);
			Assert.AreEqual(child1.Name, target[0].Name);
			Assert.AreEqual(child1.Count, target[0].Count);
		}

		/// <summary>
		///A test for Rename
		///</summary>
		[TestMethod]
		public void RenameTest() {
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname", new SimisTreeNode[2]);
			target = target.Rename("newname");
			Assert.AreEqual("targettype", target.Type);
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
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname", new SimisTreeNode[] { child1 });
			target = target.ReplaceChild(child2, child1);
			Assert.AreEqual("targettype", target.Type);
			Assert.AreEqual("targetname", target.Name);
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
			SimisTreeNode child1 = new SimisTreeNode("child1type", "child1name");
			SimisTreeNode child2 = new SimisTreeNode("child2type", "child2name");
			SimisTreeNode target = new SimisTreeNode("targettype", "targetname");
			Assert.AreEqual("<targettype \"targetname\"></targettype>", target.ToString());
			target = target.AppendChild(child1);
			Assert.AreEqual("<targettype \"targetname\"><child1type \"child1name\"></child1type></targettype>", target.ToString());
			target = target.AppendChild(child2);
			Assert.AreEqual("<targettype \"targetname\"><child1type \"child1name\"></child1type>, <child2type \"child2name\"></child2type></targettype>", target.ToString());
		}

		/// <summary>
		///A test for ToValue&lt;string&gt;
		///</summary>
		[TestMethod]
		public void ToValueStringTest() {
			string expected = "0x12345678";
			SimisTreeNode target = new SimisTreeNodeValueString("targettype", "targetname", expected);
			string actual = target.ToValue<string>();
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for ToValue&lt;uint&gt;
		///</summary>
		[TestMethod]
		public void ToValueUIntTest() {
			uint expected = 0x12345678;
			SimisTreeNode target = new SimisTreeNodeValueIntegerDWord("targettype", "targetname", expected);
			uint actual = target.ToValue<uint>();
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for ToValue&lt;int&gt;
		///</summary>
		[TestMethod]
		public void ToValueIntTest() {
			int expected = 0x1234;
			SimisTreeNode target = new SimisTreeNodeValueIntegerSigned("targettype", "targetname", expected);
			int actual = target.ToValue<int>();
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for ToValue&lt;float&gt;
		///</summary>
		[TestMethod]
		public void ToValueFloatTest() {
			float expected = 1234.5678f;
			SimisTreeNode target = new SimisTreeNodeValueFloat("targettype", "targetname", expected);
			float actual = target.ToValue<float>();
			Assert.AreEqual(expected, actual);
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
