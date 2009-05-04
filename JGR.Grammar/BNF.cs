using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JGR;

namespace JGR.Grammar
{
	public class BNF
	{
		public readonly string Filename;

		public BNF(string filename) {
			Filename = filename;
			Definitions = new Dictionary<string, BNFDefinition>();
			Productions = new Dictionary<string, BNFProduction>();
		}

		public Dictionary<string, BNFDefinition> Definitions { get; private set; }
		public Dictionary<string, BNFProduction> Productions { get; private set; }

		public override string ToString() {
			return String.Join("\n", Definitions.Values.OrderBy<BNFDefinition, string>(d => d.ToString()).Select<BNFDefinition, string>(d => d.ToString()).Union<string>(Productions.Values.OrderBy<BNFProduction, string>(p => p.ToString()).Select<BNFProduction, string>(p => p.ToString())).ToArray<string>());
		}
	}

	public class BNFStateException : FileException
	{
		private static string FormatMessage(BNFState state, string message) {
			return message + "\n\n" + state + "\n\n";
		}

		public BNFStateException(BNFState state, string message)
			: base(state.BNF.Filename, FormatMessage(state, message))
		{
		}
	}

	public class BNFState : BufferedMessageSource
	{
		public readonly BNF BNF;
		private Stack<KeyValuePair<BNFProduction, FSMState>> Rules;
		public bool IsEnterBlockTime { get; protected set; }
		public bool IsEndBlockTime { get; protected set; }

		public BNFState(BNF BNF) {
			this.BNF = BNF;
			Rules = new Stack<KeyValuePair<BNFProduction, FSMState>>();
			IsEnterBlockTime = false;
			IsEndBlockTime = false;
		}

		public override string GetMessageSourceName() {
			return "BNF State";
		}

		private List<FSMState> GetValidReferences() {
			if (Rules.Count == 0) {
				return new List<FSMState>();
			}

			var states = new List<FSMState>();
			if (Rules.Peek().Value == null) {
				if (Rules.Peek().Key.ExpressionFSM != null) {
					states.Add(Rules.Peek().Key.ExpressionFSM.Root);
				}
			} else {
				states.AddRange(Rules.Peek().Value.Next);
			}
			while (states.Any<FSMState>(s => !(s.Op is ReferenceOperator))) {
				for (var i = 0; i < states.Count; i++) {
					if (!(states[i].Op is ReferenceOperator)) {
						var items = states[i].Next;
						states.InsertRange(i + 1, items);
						states.RemoveAt(i);
						i += items.Count<FSMState>(s => true) - 1;
					}
				}
			}
			return states;
		}

		public List<string> GetValidStates() {
			var rv = new List<string>();

			if (IsEnterBlockTime) {
				// Begin-block is exclusive.
				rv.Add("<begin-block>");
				return rv;
			}
			{
				if (Rules.Count == 0) {
					rv.AddRange(BNF.Productions.Keys);
				} else {
					rv.AddRange(GetValidReferences().Select<FSMState, string>(s => ((ReferenceOperator)s.Op).Reference));
				}
			}
			if (IsEndBlockTime) {
				rv.Add("<end-block>");
			}
			return rv;
		}

		public FSMState State {
			get {
				if (Rules.Count == 0) {
					return null;
				}
				return Rules.Peek().Value;
			}
		}

		private void UpdateIsEndBlockTime() {
			IsEndBlockTime = false;
			if (Rules.Count == 0) return;

			//var rule = Rules.Peek();
			//if (rule.Value == null) {
			//    if (Rules.Count == 1) return;

			//    var oldRule = Rules.Pop();
			//    rule = Rules.Peek();
			//    Rules.Push(oldRule);
			//}

			//var states = new List<FSMState>();
			//states.AddRange(rule.Value.Next);

			var states = new List<FSMState>();
			if (Rules.Peek().Value == null) {
				if (Rules.Peek().Key.ExpressionFSM != null) {
					states.Add(Rules.Peek().Key.ExpressionFSM.Root);
				}
			} else {
				states.AddRange(Rules.Peek().Value.Next);
			}

			while (states.Any<FSMState>(s => !(s.Op is ReferenceOperator) && (s.Next.Count > 0))) {
				for (var i = 0; i < states.Count; i++) {
					if (!(states[i].Op is ReferenceOperator) && (states[i].Next.Count > 0)) {
						var items = states[i].Next;
						states.InsertRange(i + 1, items);
						states.RemoveAt(i);
						i += items.Count<FSMState>(s => true) - 1;
					}
				}
			}

			if (states.Count == 0) {
				IsEndBlockTime = true;
			} else {
				for (var i = 0; i < states.Count; i++) {
					if (!(states[i].Op is ReferenceOperator)) {
						if (states[i].Next.Count == 0) {
							IsEndBlockTime = true;
							break;
						}
					}
				}
			}
		}

		public void MoveTo(string reference) {
			MessageSend(LEVEL_DEBUG, "Moving BNF to state '" + reference + "'.");
			if (IsEnterBlockTime) throw new BNFStateException(this, "BNF expected begin-block; got reference '" + reference + "'.");
			if (Rules.Count == 0) {
				if (!BNF.Productions.ContainsKey(reference)) throw new BNFStateException(this, "BNF has no production for root reference '" + reference + "'.");
				Rules.Push(new KeyValuePair<BNFProduction, FSMState>(BNF.Productions[reference], null));
				IsEnterBlockTime = true;
			} else {
				var targets = GetValidReferences();
				var target = targets.FirstOrDefault<FSMState>(s => ((ReferenceOperator)s.Op).Reference == reference);
				if (target == null) throw new BNFStateException(this, "BNF cannot move to reference '" + reference + "', no valid state transitions found.");

				var rop = (ReferenceOperator)target.Op;
				var old = Rules.Pop();
				Rules.Push(new KeyValuePair<BNFProduction, FSMState>(old.Key, target));
				if (BNF.Productions.ContainsKey(rop.Reference)) {
					Rules.Push(new KeyValuePair<BNFProduction, FSMState>(BNF.Productions[rop.Reference], null));
					IsEnterBlockTime = true;
				}
			}
			UpdateIsEndBlockTime();
			MessageSend(LEVEL_DEBUG, ToString());
		}

		public void EnterBlock() {
			MessageSend(LEVEL_DEBUG, "Moving BNF to state <begin-block>.");
			if (!IsEnterBlockTime) throw new BNFStateException(this, "BNF expected end-block, reference or literal; got begin-block.");
			IsEnterBlockTime = false;
			UpdateIsEndBlockTime();
			MessageSend(LEVEL_DEBUG, ToString());
		}

		public void LeaveBlock() {
			MessageSend(LEVEL_DEBUG, "Moving BNF to state <end-block>.");
			if (IsEnterBlockTime) throw new BNFStateException(this, "BNF expected begin-block; got end-block.");
			if (!IsEndBlockTime) throw new BNFStateException(this, "BNF expected begin-block, reference or literal; got end-block.");
			Rules.Pop();
			UpdateIsEndBlockTime();
			MessageSend(LEVEL_DEBUG, ToString());
		}

		public bool IsEmpty {
			get {
				return Rules.Count == 0;
			}
		}

		public override string ToString() {
			return "Available BNF states: " + String.Join(", ", GetValidStates().Select<string, string>(s => s.StartsWith("<") ? s : "'" + s + "'").ToArray<string>()) + ".\n"
				+ "Current BNF state: " + String.Join(" // ", Rules.Select<KeyValuePair<BNFProduction, FSMState>, string>(kvp => "[" + kvp.Key.Symbol.Reference + "] " + kvp.Value).ToArray<string>());
		}
	}

	public class BNFRule
	{
		public readonly BNF BNF;
		public readonly ReferenceOperator Symbol;
		public readonly Operator Expression;
		public readonly FSM ExpressionFSM;
		
		public BNFRule(BNF bnf, ReferenceOperator symbol, Operator expression) {
			BNF = bnf;
			Symbol = symbol;
			Expression = expression;
			ExpressionFSM = null;
			if (expression != null) {
				ExpressionFSM = new FSM(ExpandReferences(expression));
			}
		}

		private Operator ExpandReferences(Operator op) {
			if (op is NamedReferenceOperator) {
				var nrop = (NamedReferenceOperator)op;
				if (BNF.Definitions.ContainsKey(nrop.Reference)) {
					return ExpandReferences(BNF.Definitions[nrop.Reference].Expression);
				}
				return new NamedReferenceOperator(nrop.Name, nrop.Reference);
			}
			if (op is ReferenceOperator) {
				var rop = (ReferenceOperator)op;
				if (BNF.Definitions.ContainsKey(rop.Reference)) {
					return ExpandReferences(BNF.Definitions[rop.Reference].Expression);
				}
				return new ReferenceOperator(rop.Reference);
			}
			if (op is StringOperator) {
				var sop = (StringOperator)op;
				return new StringOperator(sop.Value);
			}
			if (op is OptionalOperator) {
				var uop = (UnaryOperator)op;
				return new OptionalOperator(ExpandReferences(uop.Right));
			}
			if (op is RepeatOperator) {
				var uop = (UnaryOperator)op;
				return new RepeatOperator(ExpandReferences(uop.Right));
			}
			if (op is LogicalOrOperator) {
				var lop = (LogicalOperator)op;
				return new LogicalOrOperator(ExpandReferences(lop.Left), ExpandReferences(lop.Right));
			}
			if (op is LogicalAndOperator) {
				var lop = (LogicalOperator)op;
				return new LogicalAndOperator(ExpandReferences(lop.Left), ExpandReferences(lop.Right));
			}
			throw new InvalidDataException("Unhandled Operator: " + op);
		}
	}

	public class BNFDefinition : BNFRule
	{
		public BNFDefinition(BNF bnf, ReferenceOperator symbol, Operator expression) : base(bnf, symbol, expression) { }

		public override string ToString() {
			return Symbol.Reference + " = " + (Expression == null ? "" : Expression.ToString()) + " .";
		}
	}

	public class BNFProduction : BNFRule
	{
		public BNFProduction(BNF bnf, ReferenceOperator symbol, Operator expression) : base(bnf, symbol, expression) { }

		public override string ToString() {
			return Symbol.Reference + " ==> " + (Expression == null ? "" : Expression.ToString()) + " .";
		}
	}
}
