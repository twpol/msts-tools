//------------------------------------------------------------------------------
// Jgr.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Jgr.Grammar {
	public class Fsm {
		internal static TraceSwitch TraceSwitch = new TraceSwitch("jgr.grammar.fsm", "Trace Fsm and FsmState");

		public FsmState Root { get; private set; }

		public Fsm(Operator expression) {
			if (Fsm.TraceSwitch.TraceVerbose) {
				Trace.WriteLine("BNF: " + expression);
			}
			Root = new FsmStateStart(ExpressionToFsm(expression));
			IndexFsmUnlinks(Root);
			if (Fsm.TraceSwitch.TraceVerbose) {
				Trace.WriteLine("FSM: " + Root);
			}
		}

		public override string ToString() {
			return Root.ToString();
		}

		IList<FsmState> ExpressionToFsm(Operator op) {
			// Null operator -> <finish>
			if (op == null) {
				return new FsmState[] { new FsmStateFinish() };
			}
			// Optional operator -> {<op.right>, <finish>}
			var oop = op as OptionalOperator;
			if (oop != null) {
				var states = new List<FsmState>(ExpressionToFsm(oop.Right));
				states.Add(new FsmStateFinish());
				return states;
			}
			// Repeat operator -> {1:<op.right> -> {^1, <finish>}}
			var rop = op as RepeatOperator;
			if (rop != null) {
				var states = new List<FsmState>(ExpressionToFsm(rop.Right));
				return LinkFsmFinishes(states, states.Select<FsmState, FsmState>(s => new FsmStateUnlink(s)).Union(new FsmState[] { new FsmStateFinish() }).ToArray());
			}
			// Or operator -> {<op.left> -> <finish>, <op.right> -> <finish>}
			var loop = op as LogicalOrOperator;
			if (loop != null) {
			    var states = new List<FsmState>(ExpressionToFsm(loop.Left));
			    states.AddRange(ExpressionToFsm(loop.Right));
			    return states;
			}
			// And operator -> <op.left> -> <op.right> -> <finish>
			var laop = op as LogicalAndOperator;
			if (laop != null) {
				return LinkFsmFinishes(ExpressionToFsm(laop.Left), ExpressionToFsm(laop.Right));
			}
			// Any other operators -> op -> <finish>
			return new FsmState[] { new FsmState(op, new FsmState[] { new FsmStateFinish() }) };
		}

		IList<FsmState> LinkFsmFinishes(IList<FsmState> states, IList<FsmState> finishes) {
			var rv = new List<FsmState>();
			foreach (var state in states) {
				if (state is FsmStateFinish) {
					rv.AddRange(finishes);
					for (var i = 0; i < finishes.Count; i++) {
						if (!(finishes[i] is FsmStateUnlink) && !(finishes[i] is FsmStateFinish)) {
							finishes[i] = new FsmStateUnlink(finishes[i]);
						}
					}
				} else {
					if (!(state is FsmStateUnlink)) {
						// XXX Only place FsmState is not immutable, bah!
						state.Next = LinkFsmFinishes(state.Next, finishes);
						state.UpdateNextCache();
					}
					rv.Add(state);
				}
			}
			return rv;
		}

		void IndexFsmUnlinks(FsmState state) {
			var index = 0;
			IndexFsmUnlinks(ref index, state);
		}

		void IndexFsmUnlinks(ref int index, FsmState state) {
			if (state is FsmStateUnlink) {
				if (state.Next[0].Index == 0) {
					state.Next[0].Index = ++index;
				}
			} else {
				foreach (var next in state.Next) {
					IndexFsmUnlinks(ref index, next);
				}
			}
		}
	}

	//[Immutable]
	public class FsmState {
		public Operator Operator { get; private set; }
		// Uses ReadOnlyCollection<FsmState> so it's not mutable that way, but property can be assigned to.
		public IList<FsmState> Next { get; set; }
		public readonly bool IsReference;
		public readonly bool IsStructure;
		// Mutable by IndexFsmUnlinks for display linking.
		public int Index { get; internal set; }
		// Mutates when Next property assigned to.
		public bool NextStateHasFinish { get; private set; }
		// Mutates when Next property assigned to.
		public IEnumerable<FsmState> NextStates { get; private set; }
		// Mutates when Next property assigned to.
		public IEnumerable<FsmState> NextReferences { get; private set; }
		// Mutates when Next property assigned to.
		public IEnumerable<string> NextReferenceNames { get; private set; }

		public FsmState(Operator op)
			: this(op, new FsmState[0]) {
			Debug.Assert(op != null);
		}

		public FsmState(Operator op, IEnumerable<FsmState> nexts)
			: this(op, op is ReferenceOperator, !(op is ReferenceOperator), nexts) {
			Debug.Assert(op != null);
		}

		internal FsmState(Operator op, bool isReference, bool isStructure, IEnumerable<FsmState> nexts) {
			Operator = op;
			IsReference = isReference;
			IsStructure = isStructure;
			Next = new ReadOnlyCollection<FsmState>(new List<FsmState>(nexts));
			UpdateNextCache();
		}

		internal void UpdateNextCache() {
			var nextStates = new List<FsmState>();
			var nextReferences = new List<FsmState>();
			var nextReferenceNames = new List<string>();
			foreach (var next in Next) {
				if (next is FsmStateUnlink) {
					nextStates.Add(next.Next[0]);
					if (next.Next[0] is FsmStateFinish) {
						NextStateHasFinish = true;
					}
					if (next.Next[0].IsReference) {
						nextReferences.Add(next.Next[0]);
						nextReferenceNames.Add(((ReferenceOperator)(next.Next[0].Operator)).Reference);
					}
				} else {
					nextStates.Add(next);
					if (next is FsmStateFinish) {
						NextStateHasFinish = true;
					}
					if (next.IsReference) {
						nextReferences.Add(next);
						nextReferenceNames.Add(((ReferenceOperator)(next.Operator)).Reference);
					}
				}
			}
			NextStates = new ReadOnlyCollection<FsmState>(nextStates);
			NextReferences = new ReadOnlyCollection<FsmState>(nextReferences);
			NextReferenceNames = new ReadOnlyCollection<string>(nextReferenceNames);
		}

		internal virtual string OpString() {
			if (IsReference) {
				return ((ReferenceOperator)Operator).Reference;
			}
			return "(" + (Operator != null ? Operator.Op.ToString().ToUpperInvariant() : "null") + ")";
		}

		public virtual bool HasNext {
			get {
				return Next.Count > 0;
			}
		}

		public override string ToString() {
			var rv = (Index > 0 ? Index + ":" : "") + OpString();
			if (Next.Count == 1) {
				rv += " -> " + Next[0].ToString();
			} else if (Next.Count > 1) {
				rv += " -> {" + String.Join(", ", Next.Select(s => s.ToString()).ToArray()) + "}";
			}
			return rv;
		}
	}

	[Immutable]
	public class FsmStateStart : FsmState {
		internal FsmStateStart(IEnumerable<FsmState> nexts)
			: base(null, false, false, nexts) {
		}

		internal override string OpString() {
			return "<start>";
		}
	}

	[Immutable]
	public class FsmStateFinish : FsmState {
		internal FsmStateFinish()
			: base(null, false, false, new FsmState[0]) {
		}

		internal override string OpString() {
			return "<finish>";
		}
	}

	[Immutable]
	public class FsmStateUnlink : FsmState {
		internal FsmStateUnlink(FsmState state)
			: base(null, false, false, new FsmState[] { state }) {
		}

		internal override string OpString() {
			return "^" + Next[0].Index;
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
