//------------------------------------------------------------------------------
// JGR.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

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

	public class BNFStateException : DescriptiveException
	{
		private static string FormatMessage(BNFState state, string message) {
			if (message.Length == 0) return state.ToString();
			return message + "\r\n\r\n" + state;
		}

		public BNFStateException(BNFState state, string message)
			: base(FormatMessage(state, message))
		{
		}
	}

	public class BNFState : BufferedMessageSource
	{
		public BNF BNF { get; protected set; }
		private Stack<KeyValuePair<BNFProduction, FSMState>> Rules;

		public BNFState(BNF BNF) {
			this.BNF = BNF;
			Rules = new Stack<KeyValuePair<BNFProduction, FSMState>>();
			IsEnterBlockTime = false;
			UpdateStates();
		}

		public override string GetMessageSourceName() {
			return "BNF State";
		}

		public FSMState State {
			get {
				if (Rules.Count == 0) {
					return null;
				}
				return Rules.Peek().Value;
			}
		}

		private bool _validCache = false;

		public bool IsEnterBlockTime {
			get;
			protected set;
		}

		private bool _isEndBlockTime = false;
		public bool IsEndBlockTime {
			get {
				UpdateStates();
				return _isEndBlockTime;
			}
		}

		private List<FSMState> _validReferences = null;
		private List<FSMState> ValidReferences {
			get {
				UpdateStates();
				return _validReferences;
			}
		}

		private List<string> _validStates = null;
		public List<string> ValidStates {
			get {
				UpdateStates();
				return _validStates;
			}
		}

		private void UpdateStates() {
			if (_validCache) return;

			// Update valid references.
			_isEndBlockTime = false;
			_validReferences = new List<FSMState>();
			if (Rules.Count > 0) {
				if (Rules.Peek().Value == null) {
					if (Rules.Peek().Key.ExpressionFSM != null) {
						_validReferences.Add(Rules.Peek().Key.ExpressionFSM.Root);
					}
				} else {
					_validReferences.AddRange(Rules.Peek().Value.Next);
				}
				while (_validReferences.Any<FSMState>(s => !(s.Op is ReferenceOperator))) {
					for (var i = 0; i < _validReferences.Count; i++) {
						if (!(_validReferences[i].Op is ReferenceOperator)) {
							var items = _validReferences[i].Next;
							if (items.Count == 0) _isEndBlockTime = true;
							_validReferences.InsertRange(i + 1, items);
							_validReferences.RemoveAt(i);
							i += items.Count<FSMState>(s => true) - 1;
						}
					}
				}
				if (_validReferences.Count == 0) _isEndBlockTime = true;
			}

			// Update valid states.
			_validStates = new List<string>();
			if (IsEnterBlockTime) {
				_validStates.Add("<begin-block>");
			} else {
				if (Rules.Count == 0) {
					_validStates.AddRange(BNF.Productions.Keys);
				} else {
					_validStates.AddRange(_validReferences.Select<FSMState, string>(s => ((ReferenceOperator)s.Op).Reference));
				}
				if (_isEndBlockTime) {
					_validStates.Add("<end-block>");
				}
			}

			_validCache = true;
		}

		public void MoveTo(string reference) {
			MessageSend(LEVEL_DEBUG, "Moving BNF to state '" + reference + "'.");
			if (IsEnterBlockTime) throw new BNFStateException(this, "BNF expected begin-block; got reference '" + reference + "'.");
			if (Rules.Count == 0) {
				if (!BNF.Productions.ContainsKey(reference)) throw new BNFStateException(this, "BNF has no production for root reference '" + reference + "'.");
				Rules.Push(new KeyValuePair<BNFProduction, FSMState>(BNF.Productions[reference], null));
				IsEnterBlockTime = true;
			} else {
				var targets = ValidReferences;
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
			_validCache = false;
			//MessageSend(LEVEL_DEBUG, ToString());
		}

		public void EnterBlock() {
			MessageSend(LEVEL_DEBUG, "Moving BNF to state <begin-block>.");
			if (!IsEnterBlockTime) throw new BNFStateException(this, "BNF expected end-block, reference or literal; got begin-block.");
			IsEnterBlockTime = false;
			_validCache = false;
			//MessageSend(LEVEL_DEBUG, ToString());
		}

		public void LeaveBlock() {
			MessageSend(LEVEL_DEBUG, "Moving BNF to state <end-block>.");
			if (IsEnterBlockTime) throw new BNFStateException(this, "BNF expected begin-block; got end-block.");
			if (!IsEndBlockTime) throw new BNFStateException(this, "BNF expected begin-block, reference or literal; got end-block.");
			Rules.Pop();
			_validCache = false;
			//MessageSend(LEVEL_DEBUG, ToString());
		}

		public bool IsEmpty {
			get {
				return Rules.Count == 0;
			}
		}

		public override string ToString() {
			return "Available states: " + String.Join(", ", ValidStates.Select<string, string>(s => s.StartsWith("<") ? s : "'" + s + "'").ToArray<string>()) + ".\n"
				+ "Current rule: " + (Rules.Count == 0 ? "<none>." : "[" + Rules.Peek().Key.Symbol.Reference + "] " + Rules.Peek().Key.ExpressionFSM) + "\n"
				+ "Current state: " + String.Join(" // ", Rules.Select<KeyValuePair<BNFProduction, FSMState>, string>(kvp => "[" + kvp.Key.Symbol.Reference + "] " + kvp.Value).ToArray<string>());
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
