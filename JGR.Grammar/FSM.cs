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
			Root = new FsmStateStart(CreateState(expression));
			IndexUnlinks(Root);
			if (Fsm.TraceSwitch.TraceVerbose) {
				Trace.WriteLine("FSM: " + Root);
			}
		}

		public override string ToString() {
			return Root.ToString();
		}

		List<FsmState> CreateState(Operator op) {
			var states = new List<FsmState>();
			if (op == null) {
				states.Add(new FsmStateFinish());
				return states;
			}
			var oop = op as OptionalOperator;
			var rop = op as RepeatOperator;
			var loop = op as LogicalOrOperator;
			var laop = op as LogicalAndOperator;
			if (oop != null) {
				states.AddRange(CreateState(oop.Right));
				states.Add(new FsmStateFinish());
			} else if (rop != null) {
				states.AddRange(CreateState(rop.Right));
				LinkFinishes(states, () => {
					var l = new List<FsmState>(states.Select<FsmState, FsmState>(s => new FsmStateUnlink(s)));
					l.Add(new FsmStateFinish());
					return l;
				});
			} else if (loop != null) {
				states.AddRange(CreateState(loop.Left));
				states.AddRange(CreateState(loop.Right));
			} else if (laop != null) {
				states.AddRange(CreateState(laop.Left));
				LinkFinishes(states, () => CreateState(laop.Right));
				// TODO: This can duplicate some branches of the FSM. We should CreateState for one branch and link the rest.
			} else {
				var state = new FsmState(op);
				states.Add(state);
				state.Next.Add(new FsmStateFinish());
			}
			return states;
		}

		void LinkFinishes(List<FsmState> states, Func<List<FsmState>> makeNewStates) {
			for (var i = 0; i < states.Count; i++) {
				if (states[i] is FsmStateFinish) {
					var newStates = makeNewStates();
					states.RemoveAt(i);
					states.InsertRange(i, newStates);
					i += newStates.Count - 1;
				} else if (states[i].HasNext) {
					LinkFinishes(states[i].Next, makeNewStates);
				}
			}
		}

		void IndexUnlinks(FsmState state) {
			var index = 0;
			IndexUnlinks(ref index, state);
		}

		void IndexUnlinks(ref int index, FsmState state) {
			if (state is FsmStateUnlink) {
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

	[Immutable]
	public class FsmState {
		public Operator Operator { get; private set; }
		public Collection<FsmState> Next { get; private set; }
		public readonly bool IsReference;
		public readonly bool IsStructure;
		public int Index { get; internal set; }
		public readonly bool NextStateHasFinish = false;
		public ReadOnlyCollection<FsmState> NextStates { get; private set; }
		public ReadOnlyCollection<FsmState> NextReferences { get; private set; }
		public ReadOnlyCollection<string> NextReferenceNames { get; private set; }

		internal FsmState(Operator op)
			: this(op, op is ReferenceOperator, !(op is ReferenceOperator)) {
		}

		internal FsmState(Operator op, bool isReference, bool isStructure) {
			Operator = op;
			Next = new Collection<FsmState>();
			IsReference = isReference;
			IsStructure = isStructure;

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
				rv += " -> {" + String.Join(", ", Next.Select<FsmState, string>(s => s.ToString()).ToArray<string>()) + "}";
			}
			return rv;
		}
	}

	[Immutable]
	public class FsmStateStart : FsmState {
		internal FsmStateStart()
			: base(null, false, false) {
		}

		internal FsmStateStart(IEnumerable<FsmState> state)
			: this() {
			Next.AddRange(state);
		}

		internal override string OpString() {
			return "<start>";
		}
	}

	[Immutable]
	public class FsmStateFinish : FsmState {
		internal FsmStateFinish()
			: base(null, false, false) {
		}

		internal override string OpString() {
			return "<finish>";
		}
	}

	[Immutable]
	public class FsmStateUnlink : FsmState {
		internal FsmStateUnlink(FsmState state)
			: base(null, false, false) {
			Next.Add(state);
		}

		internal override string OpString() {
			return (Next.Count == 0 ? "^???" : "^" + Next[0].Index);
		}

		public override bool HasNext {
			get {
				return false;
			}
		}

		public override string ToString() {
			return (Index > 0 ? Index + ":" : "") + OpString();
		}
	}
}
