//------------------------------------------------------------------------------
// Tests, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using Jgr.Grammar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	///This is a test class for FsmTest and is intended
	///to contain all FsmTest Unit Tests
	///</summary>
	[TestClass]
	public class FsmTest {
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
		///A test for Fsm
		///</summary>
		[TestMethod]
		public void FsmConstructionTest() {
			Assert.AreEqual("<start> -> <finish>", new Fsm(null).ToString());
			Assert.AreEqual("<start> -> foo -> <finish>", new Fsm(new ReferenceOperator("foo")).ToString());
			Assert.AreEqual("<start> -> {foo -> <finish>, <finish>}", new Fsm(new OptionalOperator(new ReferenceOperator("foo"))).ToString());
			Assert.AreEqual("<start> -> foo -> bar -> <finish>", new Fsm(new LogicalAndOperator(new ReferenceOperator("foo"), new ReferenceOperator("bar"))).ToString());
			Assert.AreEqual("<start> -> {foo -> <finish>, bar -> <finish>}", new Fsm(new LogicalOrOperator(new ReferenceOperator("foo"), new ReferenceOperator("bar"))).ToString());
			Assert.AreEqual("<start> -> {foo -> 1:baz -> <finish>, bar -> ^1}", new Fsm(new LogicalAndOperator(new LogicalOrOperator(new ReferenceOperator("foo"), new ReferenceOperator("bar")), new ReferenceOperator("baz"))).ToString());
			Assert.AreEqual("<start> -> 1:foo -> {^1, <finish>}", new Fsm(new RepeatOperator(new ReferenceOperator("foo"))).ToString());
			Assert.AreEqual("<start> -> 1:foo -> bar -> {^1, <finish>}", new Fsm(new RepeatOperator(new LogicalAndOperator(new ReferenceOperator("foo"), new ReferenceOperator("bar")))).ToString());
			Assert.AreEqual("<start> -> 1:foo -> {^1, bar -> {^1, <finish>}}", new Fsm(new RepeatOperator(new LogicalAndOperator(new RepeatOperator(new ReferenceOperator("foo")), new ReferenceOperator("bar")))).ToString());
			Assert.AreEqual("<start> -> 2:foo -> 1:bar -> {^1, ^2, <finish>}", new Fsm(new RepeatOperator(new LogicalAndOperator(new ReferenceOperator("foo"), new RepeatOperator(new ReferenceOperator("bar"))))).ToString());
			Assert.AreEqual("<start> -> {1:foo -> {^1, ^2, <finish>}, 2:bar -> {^1, ^2, <finish>}}", new Fsm(new RepeatOperator(new LogicalOrOperator(new ReferenceOperator("foo"), new ReferenceOperator("bar")))).ToString());
		}
	}
}