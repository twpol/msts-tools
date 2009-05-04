using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace JGR.Grammar
{
	public class FSM
	{
		public readonly FSMState Root;

		public FSM(Operator expression) {
			Root = RemoveRedundantSteps(MakeStateForOp(expression));
			IndexUnlinks(Root);
		}

		public override string ToString() {
			return Root.ToString();
		}

		private FSMState MakeStateForOp(Operator op) {
			var state = new FSMState(op);
			if (op is ReferenceOperator) {
			} else if (op is StringOperator) {
			} else if (op is OptionalOperator) {
				var oop = (OptionalOperator)op;
				var right = MakeStateForOp(oop.Right);
				var end = new FSMState(null);
				LinkEndsTo(right, new FiniteStateMachineStateUnlink(state.Op, end));
				state.Next.Add(right);
				state.Next.Add(end);
			} else if (op is RepeatOperator) {
				var rop = (RepeatOperator)op;
				var right = MakeStateForOp(rop.Right);
				state.Next.Add(right);
				LinkEndsTo(right, new FSMState[] { new FiniteStateMachineStateUnlink(state.Op, state), new FSMState(null) });
			} else if (op is LogicalOrOperator) {
				var lop = (LogicalOperator)op;
				var left = MakeStateForOp(lop.Left);
				var right = MakeStateForOp(lop.Right);
				state.Next.Add(left);
				state.Next.Add(right);
			} else if (op is LogicalAndOperator) {
				var lop = (LogicalOperator)op;
				var left = MakeStateForOp(lop.Left);
				var right = MakeStateForOp(lop.Right);
				state.Next.Add(left);
				LinkEndsTo(left, right);
			}
			return state;
		}

		private void LinkEndsTo(FSMState state, FSMState end) {
			LinkEndsTo(state, new FSMState[] { end });
		}

		private void LinkEndsTo(FSMState state, IEnumerable<FSMState> end) {
			if (state.Next.Count == 0) {
				state.Next.AddRange(end);
			} else {
				foreach (var next in state.Next) {
					if (!(next is FiniteStateMachineStateUnlink) && !end.Contains<FSMState>(next)) {
						LinkEndsTo(next, end);
					}
				}
			}
		}

		private void ReplaceLinksWith(FSMState state, FSMState old, FSMState end) {
			ReplaceLinksWith(state, old, new FSMState[] { end });
		}

		private void ReplaceLinksWith(FSMState state, FSMState old, IEnumerable<FSMState> end) {
			for (var i = 0; i < state.Next.Count; i++) {
				if (state.Next[i] == old) {
					state.Next.InsertRange(i + 1, end);
					state.Next.RemoveAt(i);
					i += end.Count<FSMState>(s => true) - 1;
				}
			}
			if (!(state is FiniteStateMachineStateUnlink)) {
				foreach (var next in state.Next) {
					ReplaceLinksWith(next, old, end);
				}
			}
		}

		private FSMState RemoveRedundantSteps(FSMState root) {
			return RemoveRedundantSteps(root, root);
		}

		private FSMState RemoveRedundantSteps(FSMState root, FSMState state) {
			if ((state.Next.Count == 1) && ((state.Op is RepeatOperator) || (state.Op is LogicalOperator) || (state.Op == null))) {
				//Debug.WriteLine("RemoveRedundantSteps: 1 next state, state = " + state.OpString() + ".");
				//Debug.WriteLine("BEFORE: " + root);
				ReplaceLinksWith(root, state, state.Next[0]);
				//Debug.WriteLine("AFTER:  " + root);
				//Debug.WriteLine("");
				return RemoveRedundantSteps(root, state.Next[0]);
			}
			if ((state.Op is LogicalOperator) && state.Next.All<FSMState>(s => (s.Op.GetType() == state.Op.GetType()) || (s.Op is ReferenceOperator))) {
				//Debug.WriteLine("RemoveRedundantSteps: nested operator(s) same as container, state = " + state.OpString() + ".");
				//Debug.WriteLine("BEFORE: " + root);
				while (state.Next.Any<FSMState>(s => s.Op.GetType() == state.Op.GetType())) {
					var next = state.Next.ToArray();
					state.Next.RemoveAll(n => true);
					foreach (var n in next) {
						if (n.Op.GetType() == state.Op.GetType()) {
							state.Next.AddRange(n.Next);
						} else {
							state.Next.Add(n);
						}
					}
				}
				//Debug.WriteLine("AFTER:  " + root);
				//Debug.WriteLine("");
			}
			if ((state.Next.Count == 1) && !(state.Next[0] is FiniteStateMachineStateUnlink) && !(state.Next[0].Op is ReferenceOperator) && (state.Next[0].Next.Count > 0)) {
				//Debug.WriteLine("RemoveRedundantSteps: 1 next state, at least 1 further state, state = " + state.OpString() + ", state.Next[0] = " +  state.Next[0].OpString() + ".");
				//Debug.WriteLine("BEFORE: " + root);
				ReplaceLinksWith(root, state.Next[0], state.Next[0].Next);
				//Debug.WriteLine("AFTER:  " + root);
				//Debug.WriteLine("");
			}
			for (var i = 0; i < state.Next.Count; i++) {
			    if (!(state.Next[i] is FiniteStateMachineStateUnlink)) {
			        state.Next[i] = RemoveRedundantSteps(root, state.Next[i]);
			    }
			}
			return state;
		}

		private void IndexUnlinks(FSMState state) {
			var index = 0;
			IndexUnlinks(ref index, state);
		}

		private void IndexUnlinks(ref int index, FSMState state) {
			if (state is FiniteStateMachineStateUnlink) {
				if (state.Next[0].Index == 0) {
					state.Next[0].Index = ++index;
				}
			} else {
				foreach (var next in state.Next) {
					IndexUnlinks(ref index, next);
				}
			}
		}
	}

	public class FSMState
	{
		public readonly Operator Op;
		public readonly List<FSMState> Next;
		public int Index = 0;

		internal FSMState(Operator op) {
			Op = op;
			Next = new List<FSMState>();
		}

		internal string OpString() {
			if (Op is ReferenceOperator) {
				return (Index > 0 ? Index + ":" : "") + ((ReferenceOperator)Op).Reference;
			}
			return (Index > 0 ? Index + ":"  : "") + "(" + (Op != null ? Op.Op.ToString().ToLower() : "null") + ")";
		}

		public override string ToString() {
			var rv = OpString();
			if (Next.Count == 1) {
				rv += "-> " + Next[0].ToString();
			} else if (Next.Count > 1) {
				rv += "-> {" + String.Join(", ", Next.Select<FSMState, string>(s => s.ToString()).ToArray<string>()) + "}";
			}
			return rv;
		}
	}

	public class FiniteStateMachineStateUnlink : FSMState
	{
		internal FiniteStateMachineStateUnlink(Operator op, FSMState state)
			: base(op)
		{
			Next.Add(state);
		}

		public override string ToString() {
			return (Next.Count == 0 ? "^???" : "^" + Next[0].Index);
		}
	}
}
