//------------------------------------------------------------------------------
// JGR.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Jgr.Grammar
{
	public class Bnf
	{
		public static TraceSwitch TraceSwitch = new TraceSwitch("jgr.grammar.bnf", "Trace Bnf and BnfState");

		public string FileName { get; private set; }

		public Bnf(string fileName) {
			FileName = fileName;
			Definitions = new Dictionary<string, BnfDefinition>();
			Productions = new Dictionary<string, BnfProduction>();
		}

		public Dictionary<string, BnfDefinition> Definitions { get; private set; }
		public Dictionary<string, BnfProduction> Productions { get; private set; }

		public override string ToString() {
			return String.Join("\n", Definitions.Values.OrderBy<BnfDefinition, string>(d => d.ToString()).Select<BnfDefinition, string>(d => d.ToString()).Union<string>(Productions.Values.OrderBy<BnfProduction, string>(p => p.ToString()).Select<BnfProduction, string>(p => p.ToString())).ToArray<string>());
		}
	}

	public class BnfStateException : DescriptiveException
	{
		public BnfStateException()
			: base("") {
		}

		public BnfStateException(string message)
			: base(message) {
		}

		public BnfStateException(string message, Exception innerException)
			: base(message, innerException) {
		}

		static string FormatMessage(BnfState state, string message) {
			if (message.Length == 0) return state.ToString();
			return message + "\r\n\r\n" + state;
		}

		public BnfStateException(BnfState state, string message)
			: this(FormatMessage(state, message))
		{
		}
	}

	public class BnfState : BufferedMessageSource
	{
		public Bnf Bnf { get; private set; }
		Stack<KeyValuePair<BnfProduction, FsmState>> Rules;

		public BnfState(Bnf bnf) {
			this.Bnf = bnf;
			Rules = new Stack<KeyValuePair<BnfProduction, FsmState>>();
			IsEnterBlockTime = false;
		}

		public override string MessageSourceName {
			get {
				return "BNF State";
			}
		}

		public FsmState State {
			get {
				if (Rules.Count == 0) {
					return null;
				}
				return Rules.Peek().Value;
			}
		}

		public bool IsEnterBlockTime { get; private set;  }

		public bool IsEndBlockTime {
			get {
				if (Rules.Count == 0) return false;
				return Rules.Peek().Value.NextStateHasFinish;
			}
		}

		IEnumerable<FsmState> ValidReferences {
			get {
				if (Rules.Count == 0) return new List<FsmState>();
				return Rules.Peek().Value.NextReferences;
			}
		}

		public IEnumerable<string> ValidStates {
			get {
				if (IsEnterBlockTime) {
					return new List<string>() { "<begin-block>" };
				}
				var nextStates = new List<string>();
				if (Rules.Count == 0) {
					nextStates.AddRange(Bnf.Productions.Keys);
				} else {
					nextStates.AddRange(Rules.Peek().Value.NextReferenceNames);
				}
				if (IsEndBlockTime) {
					nextStates.Add("<end-block>");
				}
				return nextStates;
			}
		}

		public void MoveTo(string reference) {
			if (Bnf.TraceSwitch.TraceInfo) Trace.WriteLine("Moving BNF to state '" + reference + "'.");
			if (IsEnterBlockTime) throw new BnfStateException(this, "BNF expected begin-block; got reference '" + reference + "'.");
			if (Rules.Count == 0) {
				if (!Bnf.Productions.ContainsKey(reference)) throw new BnfStateException(this, "BNF has no production for root reference '" + reference + "'.");
				Rules.Push(new KeyValuePair<BnfProduction, FsmState>(Bnf.Productions[reference], Bnf.Productions[reference].ExpressionFsm.Root));
				IsEnterBlockTime = true;
			} else {
				var targets = ValidReferences;
				var target = targets.FirstOrDefault<FsmState>(s => ((ReferenceOperator)s.Op).Reference == reference);
				if (target == null) throw new BnfStateException(this, "BNF cannot move to reference '" + reference + "', no valid state transitions found.");

				var rop = (ReferenceOperator)target.Op;
				var old = Rules.Pop();
				Rules.Push(new KeyValuePair<BnfProduction, FsmState>(old.Key, target));
				if (Bnf.Productions.ContainsKey(rop.Reference)) {
					Rules.Push(new KeyValuePair<BnfProduction, FsmState>(Bnf.Productions[rop.Reference], Bnf.Productions[reference].ExpressionFsm.Root));
					IsEnterBlockTime = true;
				}
			}
			if (Bnf.TraceSwitch.TraceVerbose) Trace.WriteLine(ToString() + "\n");
		}

		public void EnterBlock() {
			if (Bnf.TraceSwitch.TraceInfo) Trace.WriteLine("Moving BNF to state <begin-block>.");
			if (!IsEnterBlockTime) throw new BnfStateException(this, "BNF expected end-block, reference or literal; got begin-block.");
			IsEnterBlockTime = false;
			if (Bnf.TraceSwitch.TraceVerbose) Trace.WriteLine(ToString() + "\n");
		}

		public void LeaveBlock() {
			if (Bnf.TraceSwitch.TraceInfo) Trace.WriteLine("Moving BNF to state <end-block>.");
			if (IsEnterBlockTime) throw new BnfStateException(this, "BNF expected begin-block; got end-block.");
			if (!IsEndBlockTime) throw new BnfStateException(this, "BNF expected begin-block, reference or literal; got end-block.");
			Rules.Pop();
			if (Bnf.TraceSwitch.TraceVerbose) Trace.WriteLine(ToString() + "\n");
		}

		public bool IsEmpty {
			get {
				return Rules.Count == 0;
			}
		}

		public override string ToString() {
			return "Available states: " + String.Join(", ", ValidStates.Select<string, string>(s => s.StartsWith("<") ? s : "'" + s + "'").ToArray<string>()) + ".\n"
				+ "Current rule: " + (Rules.Count == 0 ? "<none>." : "[" + Rules.Peek().Key.Symbol.Reference + "] " + Rules.Peek().Key.ExpressionFsm) + "\n"
				+ "Current state: " + String.Join(" // ", Rules.Select<KeyValuePair<BnfProduction, FsmState>, string>(kvp => "[" + kvp.Key.Symbol.Reference + "] " + kvp.Value).ToArray<string>());
		}
	}

	[Immutable]
	public class BnfRule
	{
		public Bnf Bnf { get; private set; }
		public ReferenceOperator Symbol { get; private set; }
		public Operator Expression { get; private set; }
		public Fsm ExpressionFsm { get; private set; }
		
		public BnfRule(Bnf bnf, ReferenceOperator symbol, Operator expression) {
			Bnf = bnf;
			Symbol = symbol;
			Expression = expression;
			ExpressionFsm = new Fsm(ExpandReferences(expression));
		}

		Operator ExpandReferences(Operator op) {
			if (op == null) {
				return op;
			}
			if (op is NamedReferenceOperator) {
				var nrop = (NamedReferenceOperator)op;
				if (Bnf.Definitions.ContainsKey(nrop.Reference)) {
					return ExpandReferences(Bnf.Definitions[nrop.Reference].Expression);
				}
				return new NamedReferenceOperator(nrop.Name, nrop.Reference);
			}
			if (op is ReferenceOperator) {
				var rop = (ReferenceOperator)op;
				if (Bnf.Definitions.ContainsKey(rop.Reference)) {
					return ExpandReferences(Bnf.Definitions[rop.Reference].Expression);
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

	[Immutable]
	public class BnfDefinition : BnfRule
	{
		public BnfDefinition(Bnf bnf, ReferenceOperator symbol, Operator expression) : base(bnf, symbol, expression) { }

		public override string ToString() {
			return Symbol.Reference + " = " + (Expression == null ? "" : Expression.ToString()) + " .";
		}
	}

	[Immutable]
	public class BnfProduction : BnfRule
	{
		public BnfProduction(Bnf bnf, ReferenceOperator symbol, Operator expression) : base(bnf, symbol, expression) { }

		public override string ToString() {
			return Symbol.Reference + " ==> " + (Expression == null ? "" : Expression.ToString()) + " .";
		}
	}
}
