//------------------------------------------------------------------------------
// Jgr.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Jgr.Grammar {
	public class Fsm {
		public static TraceSwitch TraceSwitch = new TraceSwitch("jgr.grammar.fsm", "Trace Fsm and FsmState");

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
				return ((ReferenceOperator)Op).Reference;
			}
			return "(" + (Op != null ? Op.Op.ToString().ToLower() : "null") + ")";
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

		bool FilledCache = false;
		List<FsmState> _NextStates;
		bool _NextStateHasFinish = false;
		List<FsmState> _NextReferences;
		List<string> _NextReferenceNames;
		void FillCache() {
			if (FilledCache) return;
			_NextStates = new List<FsmState>();
			_NextReferences = new List<FsmState>();
			_NextReferenceNames = new List<string>();
			foreach (var next in Next) {
				if (next is FsmStateUnlink) {
					_NextStates.Add(next.Next[0]);
					if (next.Next[0] is FsmStateFinish) {
						_NextStateHasFinish = true;
					}
					if (next.Next[0].IsReference) {
						_NextReferences.Add(next.Next[0]);
						_NextReferenceNames.Add(((ReferenceOperator)(next.Next[0].Op)).Reference);
					}
				} else {
					_NextStates.Add(next);
					if (next is FsmStateFinish) {
						_NextStateHasFinish = true;
					}
					if (next.IsReference) {
						_NextReferences.Add(next);
						_NextReferenceNames.Add(((ReferenceOperator)(next.Op)).Reference);
					}
				}
			}
			FilledCache = true;
		}

		public IEnumerable<FsmState> NextStates {
			get {
				FillCache();
				return _NextStates;
			}
		}

		public bool NextStateHasFinish {
			get {
				FillCache();
				return _NextStateHasFinish;
			}
		}

		public IEnumerable<FsmState> NextReferences {
			get {
				FillCache();
				return _NextReferences;
			}
		}

		public IEnumerable<string> NextReferenceNames {
			get {
				FillCache();
				return _NextReferenceNames;
			}
		}
	}

	[Immutable]
	public class FsmStateStart : FsmState {
		internal FsmStateStart()
			: base(null) {
			IsReference = false;
			IsStructure = false;
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
			: base(null) {
			IsReference = false;
			IsStructure = false;
		}

		internal override string OpString() {
			return "<finish>";
		}
	}

	[Immutable]
	public class FsmStateUnlink : FsmState {
		internal FsmStateUnlink(FsmState state)
			: base(null) {
			Next.Add(state);
			IsReference = false;
			IsStructure = false;
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
