//------------------------------------------------------------------------------
// Jgr.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
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
		static string FormatMessage(BnfState state, string message) {
			if (message.Length == 0) return state.Bnf.FileName + "\r\n\r\n" + state.ToString().Replace("\n", "\r\n\r\n");
			return state.Bnf.FileName + "\r\n\r\n" + message + "\r\n\r\n" + state.ToString().Replace("\n", "\r\n\r\n");
		}

		public BnfStateException(BnfState state, string message)
			: base(FormatMessage(state, message))
		{
		}
	}

	public class BnfState : BufferedMessageSource
	{
		public Bnf Bnf { get; private set; }
		Stack<KeyValuePair<BnfRule, FsmState>> Rules;

		public BnfState(Bnf bnf)
			: this(bnf, "FILE") {
		}

		public BnfState(Bnf bnf, string root) {
			this.Bnf = bnf;
			Rules = new Stack<KeyValuePair<BnfRule, FsmState>>();
			IsEnterBlockTime = false;
			if (Bnf.Definitions.ContainsKey(root)) {
				Rules.Push(new KeyValuePair<BnfRule, FsmState>(Bnf.Definitions[root], Bnf.Definitions[root].ExpressionFsm.Root));
			} else {
				Rules.Push(new KeyValuePair<BnfRule, FsmState>(Bnf.Productions[root], Bnf.Productions[root].ExpressionFsm.Root));
			}
		}

		public override string MessageSourceName {
			get {
				return "BNF State";
			}
		}

		public FsmState State {
			get {
				if (IsCompleted) {
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
				if (IsCompleted) {
					return false;
				}
				return Rules.Peek().Value.NextStateHasFinish;
			}
		}

		IEnumerable<FsmState> ValidReferences {
			get {
				if (IsCompleted) {
					return new FsmState[0];
				}
				return Rules.Peek().Value.NextReferences;
			}
		}

		public IEnumerable<string> ValidStates {
			get {
				if (IsCompleted) {
					return new string[0];
				}
				if (IsEnterBlockTime) {
					return new List<string>() { "<begin-block>" };
				}
				var nextStates = new List<string>(Rules.Peek().Value.NextReferenceNames);
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
			if (IsCompleted) throw new BnfStateException(this, "BNF has completed; got '" + reference + "'.");
			var target = ValidReferences.FirstOrDefault(s => ((ReferenceOperator)s.Operator).Reference == reference);
			if (IsEnterBlockTime || (target == null)) throw new BnfStateException(this, "BNF expected " + String.Join(", ", ValidStates.Select(s => s.StartsWith("<", StringComparison.Ordinal) ? s : "'" + s + "'").ToArray()) + "; got '" + reference + "'.");
			var rop = (ReferenceOperator)target.Operator;
			var old = Rules.Pop();
			Rules.Push(new KeyValuePair<BnfRule, FsmState>(old.Key, target));
			if (Bnf.Productions.ContainsKey(rop.Reference)) {
				Rules.Push(new KeyValuePair<BnfRule, FsmState>(Bnf.Productions[rop.Reference], Bnf.Productions[reference].ExpressionFsm.Root));
				IsEnterBlockTime = true;
			}
			if (Bnf.TraceSwitch.TraceVerbose) Trace.WriteLine(ToString() + "\n");
		}

		/// <summary>
		/// Completes a move to a reference which represents a separate subexpression.
		/// </summary>
		public void EnterBlock() {
			if (Bnf.TraceSwitch.TraceInfo) Trace.WriteLine("Moving BNF to state <begin-block>.");
			if (IsCompleted) throw new BnfStateException(this, "BNF has completed; got <begin-block>.");
			if (!IsEnterBlockTime) throw new BnfStateException(this, "BNF expected " + String.Join(", ", ValidStates.Select(s => s.StartsWith("<", StringComparison.Ordinal) ? s : "'" + s + "'").ToArray()) + "; got <begin-block>.");
			IsEnterBlockTime = false;
			if (Bnf.TraceSwitch.TraceVerbose) Trace.WriteLine(ToString() + "\n");
		}

		/// <summary>
		/// Leaves the existing subexpression and returns to the parent.
		/// </summary>
		public void LeaveBlock() {
			if (Bnf.TraceSwitch.TraceInfo) Trace.WriteLine("Moving BNF to state <end-block>.");
			if (IsCompleted) throw new BnfStateException(this, "BNF has completed; got <end-block>.");
			if (IsEnterBlockTime || !IsEndBlockTime) throw new BnfStateException(this, "BNF expected " + String.Join(", ", ValidStates.Select(s => s.StartsWith("<", StringComparison.Ordinal) ? s : "'" + s + "'").ToArray()) + "; got <end-block>.");
			Rules.Pop();
			if (Bnf.TraceSwitch.TraceVerbose) Trace.WriteLine(ToString() + "\n");
		}

		public bool IsCompleted {
			get {
				return Rules.Count <= 0;
			}
		}

		public override string ToString() {
			return "Current state: " + String.Join(" // ", Rules.Select(kvp => "[" + kvp.Key.Symbol.Reference + "] " + kvp.Value).ToArray()) + "\n"
				+ "Current rule: " + (Rules.Count == 0 ? "<none>." : "[" + Rules.Peek().Key.Symbol.Reference + "] " + Rules.Peek().Key.ExpressionFsm);
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
