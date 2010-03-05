//------------------------------------------------------------------------------
// Jgr.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
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
		internal static TraceSwitch TraceSwitch = new TraceSwitch("jgr.grammar.bnf", "Trace Bnf and BnfState");

		public string FileName { get; private set; }

		public Bnf(string fileName) {
			FileName = fileName;
			Definitions = new Dictionary<string, BnfDefinition>();
			Productions = new Dictionary<string, BnfProduction>();
		}

		public Dictionary<string, BnfDefinition> Definitions { get; private set; }
		public Dictionary<string, BnfProduction> Productions { get; private set; }

		public override string ToString() {
			return String.Join("\n", Definitions.Values.OrderBy(d => d.ToString()).Select(d => d.ToString()).Union(Productions.Values.OrderBy(p => p.ToString()).Select(p => p.ToString())).ToArray());
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

		/// <summary>
		/// Returns a <see cref="bool"/> indicating whether the <see cref="BnfState"/> instance is expecting a call to <see cref="EnterBlock"/> next.
		/// </summary>
		public bool IsEnterBlockTime { get; private set;  }

		/// <summary>
		/// Returns a <see cref="bool"/> indicating whether the <see cref="BnfState"/> instance will accept a call to <see cref="LeaveBlock"/> next.
		/// </summary>
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

		/// <summary>
		/// Moves the <see cref="BnfState"/> instance on to the given reference or throws a <see cref="BnfStateException"/> if it is not valid.
		/// </summary>
		/// <param name="reference">The reference to move to.</param>
		/// <exception cref="BnfStateException">Thrown if the given reference is not found or moving to a new reference is not valid at this time.</exception>
		public void MoveTo(string reference) {
			if (Bnf.TraceSwitch.TraceInfo) Trace.WriteLine("Moving BNF to state '" + reference + "'.");
			if (IsEnterBlockTime) throw new BnfStateException(this, "BNF expected begin-block; got reference '" + reference + "'.");
			if (Rules.Count == 0) {
				if (!Bnf.Productions.ContainsKey(reference)) throw new BnfStateException(this, "BNF has no production for root reference '" + reference + "'.");
				Rules.Push(new KeyValuePair<BnfProduction, FsmState>(Bnf.Productions[reference], Bnf.Productions[reference].ExpressionFsm.Root));
				IsEnterBlockTime = true;
			} else {
				var targets = ValidReferences;
				var target = targets.FirstOrDefault(s => ((ReferenceOperator)s.Operator).Reference == reference);
				if (target == null) throw new BnfStateException(this, "BNF cannot move to reference '" + reference + "', no valid state transitions found.");

				var rop = (ReferenceOperator)target.Operator;
				var old = Rules.Pop();
				Rules.Push(new KeyValuePair<BnfProduction, FsmState>(old.Key, target));
				if (Bnf.Productions.ContainsKey(rop.Reference)) {
					Rules.Push(new KeyValuePair<BnfProduction, FsmState>(Bnf.Productions[rop.Reference], Bnf.Productions[reference].ExpressionFsm.Root));
					IsEnterBlockTime = true;
				}
			}
			if (Bnf.TraceSwitch.TraceVerbose) Trace.WriteLine(ToString() + "\n");
		}

		/// <summary>
		/// Completes a move to a reference which represents a separate subexpression.
		/// </summary>
		public void EnterBlock() {
			if (Bnf.TraceSwitch.TraceInfo) Trace.WriteLine("Moving BNF to state <begin-block>.");
			if (!IsEnterBlockTime) throw new BnfStateException(this, "BNF expected end-block, reference or literal; got begin-block.");
			IsEnterBlockTime = false;
			if (Bnf.TraceSwitch.TraceVerbose) Trace.WriteLine(ToString() + "\n");
		}

		/// <summary>
		/// Leaves the existing subexpression and returns to the parent.
		/// </summary>
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
			return "Available states: " + String.Join(", ", ValidStates.Select(s => s.StartsWith("<", StringComparison.Ordinal) ? s : "'" + s + "'").ToArray()) + ".\n"
				+ "Current rule: " + (Rules.Count == 0 ? "<none>." : "[" + Rules.Peek().Key.Symbol.Reference + "] " + Rules.Peek().Key.ExpressionFsm) + "\n"
				+ "Current state: " + String.Join(" // ", Rules.Select(kvp => "[" + kvp.Key.Symbol.Reference + "] " + kvp.Value).ToArray());
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
			var nrop = op as NamedReferenceOperator;
			if (nrop != null) {
				if (Bnf.Definitions.ContainsKey(nrop.Reference)) {
					return ExpandReferences(Bnf.Definitions[nrop.Reference].Expression);
				}
				return new NamedReferenceOperator(nrop.Name, nrop.Reference);
			}
			var rop = op as ReferenceOperator;
			if (rop != null) {
				if (Bnf.Definitions.ContainsKey(rop.Reference)) {
					return ExpandReferences(Bnf.Definitions[rop.Reference].Expression);
				}
				return new ReferenceOperator(rop.Reference);
			}
			var sop = op as StringOperator;
			if (sop != null) {
				return new StringOperator(sop.Value);
			}
			var uop = op as UnaryOperator;
			if (op is OptionalOperator) {
				return new OptionalOperator(ExpandReferences(uop.Right));
			}
			if (op is RepeatOperator) {
				return new RepeatOperator(ExpandReferences(uop.Right));
			}
			var lop = op as BinaryOperator;
			if (op is LogicalOrOperator) {
				return new LogicalOrOperator(ExpandReferences(lop.Left), ExpandReferences(lop.Right));
			}
			if (op is LogicalAndOperator) {
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
