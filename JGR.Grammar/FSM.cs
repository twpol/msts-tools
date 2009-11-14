//------------------------------------------------------------------------------
// JGR.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Jgr.Grammar
{
	public class Fsm
	{
		public static TraceSwitch TraceSwitch = new TraceSwitch("jgr.grammar.fsm", "Trace Fsm and FsmState");

		public FsmState Root { get; private set; }

		public Fsm(Operator expression) {
			if (Fsm.TraceSwitch.TraceVerbose) Trace.WriteLine("ORIGINAL: " + expression);
			Root = RemoveRedundantSteps(MakeStateForOp(expression));
			IndexUnlinks(Root);
		}

		public override string ToString() {
			return Root.ToString();
		}

		FsmState MakeStateForOp(Operator op) {
			var state = new FsmState(op);
			if (op is ReferenceOperator) {
			} else if (op is StringOperator) {
			} else if (op is OptionalOperator) {
				var oop = (OptionalOperator)op;
				var right = MakeStateForOp(oop.Right);
				var end = new FsmState(null);
				LinkEndsTo(right, new FsmStateUnlink(end));
				state.Next.Add(right);
				state.Next.Add(end);
			} else if (op is RepeatOperator) {
				var rop = (RepeatOperator)op;
				var right = MakeStateForOp(rop.Right);
				state.Next.Add(right);
				LinkEndsTo(right, new FsmState[] { new FsmStateUnlink(state), new FsmState(null) });
			} else if (op is LogicalOperator) {
				var lop = (LogicalOperator)op;
				var left = MakeStateForOp(lop.Left);
				var right = MakeStateForOp(lop.Right);
				if (op is LogicalOrOperator) {
					state.Next.Add(left);
					state.Next.Add(right);
				} else if (op is LogicalAndOperator) {
					state.Next.Add(left);
					LinkEndsTo(left, right);
				} else {
					Debug.Assert(false, "LogicalOperator is not Or or And.");
				}
			} else {
				Debug.Assert(false, "Operator is not known.");
			}
			return state;
		}

		void LinkEndsTo(FsmState state, FsmState end) {
			LinkEndsTo(state, new FsmState[] { end });
		}

		void LinkEndsTo(FsmState state, IEnumerable<FsmState> end) {
			if (state is FsmStateUnlink) return;
			if (state.Next.Count == 0) {
				state.Next.AddRange(end);
			} else {
				foreach (var next in state.Next) {
					if (end.Contains<FsmState>(next)) continue;
					LinkEndsTo(next, end);
				}
			}
		}

		void ReplaceLinksWith(FsmState state, FsmState old, FsmState end) {
			ReplaceLinksWith(state, old, new FsmState[] { end });
		}

		void ReplaceLinksWith(FsmState state, FsmState old, IEnumerable<FsmState> end) {
			for (var i = 0; i < state.Next.Count; i++) {
				if (state.Next[i] == old) {
					state.Next.InsertRange(i + 1, end);
					state.Next.RemoveAt(i);
					i += end.Count<FsmState>(s => true) - 1;
				}
			}
			if (state.HasNext) {
				foreach (var next in state.Next) {
					ReplaceLinksWith(next, old, end);
				}
			}
		}

		FsmState RemoveRedundantSteps(FsmState root) {
			IndexUnlinks(root); // FIXME
			if (Fsm.TraceSwitch.TraceVerbose) Trace.WriteLine("INITIAL:  " + root);
			var rv = RemoveRedundantSteps(ref root, root);
			IndexUnlinks(root); // FIXME
			if (Fsm.TraceSwitch.TraceVerbose) Trace.WriteLine("FINAL:    " + root);
			if (Fsm.TraceSwitch.TraceVerbose) Trace.WriteLine("");
			return rv;
		}

		FsmState RemoveRedundantSteps(ref FsmState root, FsmState state) {
			// Structure with exactly 1 next state can be removed.
			if (state.IsStructure && (state.Next.Count == 1)) {
				if (Fsm.TraceSwitch.TraceVerbose) Trace.WriteLine("  Removing unnecessary " + state.OpString() + ".");
				if (root == state) {
					root = state.Next[0];
				}
				ReplaceLinksWith(root, state, state.Next[0]);
				IndexUnlinks(root); // FIXME
				if (Fsm.TraceSwitch.TraceVerbose) Trace.WriteLine("  New:    " + root);
				return RemoveRedundantSteps(ref root, state.Next[0]);
			}
			if (state.IsReference && (state.Next.Count == 1) && state.Next[0].IsStructure && (state.Next[0].Next.Count > 0)) {
				if (Fsm.TraceSwitch.TraceVerbose) Trace.WriteLine("  Removing unnecessary " + state.Next[0].OpString() + ".");
			    ReplaceLinksWith(root, state.Next[0], state.Next[0].Next);
				IndexUnlinks(root); // FIXME
				if (Fsm.TraceSwitch.TraceVerbose) Trace.WriteLine("  New:    " + root);
				return RemoveRedundantSteps(ref root, state);
			}
			if (state.HasNext) {
				for (var i = 0; i < state.Next.Count; i++) {
					state.Next[i] = RemoveRedundantSteps(ref root, state.Next[i]);
				}
			}
			return state;
		}

		void IndexUnlinks(FsmState state) {
			var index = 0;
			IndexUnlinks(ref index, state);
		}

		void IndexUnlinks(ref int index, FsmState state) {
			if (state is FsmStateUnlink) {
				state.Next[0].Index = ++index;
			} else {
				foreach (var next in state.Next) {
					IndexUnlinks(ref index, next);
				}
			}
		}
	}

	public class FsmState
	{
		public Operator Op { get; private set; }
		public List<FsmState> Next { get; private set; }
		public bool IsReference { get; protected set; }
		public bool IsStructure { get; protected set; }
		public int Index { get; internal set; }

		internal FsmState(Operator op) {
			Op = op;
			Next = new List<FsmState>();
			IsReference = Op is ReferenceOperator;
			IsStructure = !IsReference;
		}

		internal virtual string OpString() {
			if (IsReference) {
				return (Index > 0 ? Index + ":" : "") + ((ReferenceOperator)Op).Reference;
			}
			return (Index > 0 ? Index + ":"  : "") + "(" + (Op != null ? Op.Op.ToString().ToLower() : "null") + ")";
		}

		public virtual bool HasNext {
			get {
				return Next.Count > 0;
			}
		}

		public override string ToString() {
			var rv = OpString();
			if (Next.Count == 1) {
				rv += " -> " + Next[0].ToString();
			} else if (Next.Count > 1) {
				rv += " -> {" + String.Join(", ", Next.Select<FsmState, string>(s => s.ToString()).ToArray<string>()) + "}";
			}
			return rv;
		}
	}

	public class FsmStateUnlink : FsmState
	{
		internal FsmStateUnlink(FsmState state)
			: base(null)
		{
			Next.Add(state);
			IsReference = false;
			IsStructure = false;
		}

		internal override string OpString() {
			return (Index > 0 ? Index + ":" : "") + (Next.Count == 0 ? "^???" : "^" + Next[0].Index);
		}

		public override bool HasNext {
			get {
				return false;
			}
		}

		public override string ToString() {
			return OpString();
		}
	}
}
