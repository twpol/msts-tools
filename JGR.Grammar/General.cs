using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JGR.Grammar
{
	public enum OperatorType
	{
		Reference,
		String,
		Optional,
		Repeat,
		LogicalOr,
		LogicalAnd
	}

	public abstract class Operator : ICloneable
	{
		public readonly OperatorType Op;

		public Operator(OperatorType op) {
			Op = op;
		}

		#region ICloneable Members

		public virtual object Clone() {
			throw new NotImplementedException();
		}

		#endregion
	}

	public class ReferenceOperator : Operator
	{
		public readonly string Reference;

		protected ReferenceOperator(OperatorType op, string reference)
			: base(op)
		{
			Reference = reference;
		}

		public ReferenceOperator(string reference)
			: this(OperatorType.Reference, reference) {
		}

		public override string ToString() {
			return ":" + Reference;
		}

		public override object Clone() {
			return new ReferenceOperator(Reference);
		}
	}

	public class NamedReferenceOperator : ReferenceOperator
	{
		public readonly string Name;

		public NamedReferenceOperator(string name, string reference)
			: base(OperatorType.Reference, reference)
		{
			Name = name;
		}

		public override string ToString() {
			return base.ToString() + "," + Name;
		}

		public override object Clone() {
			return new NamedReferenceOperator(Name, Reference);
		}
	}

	public class StringOperator : Operator
	{
		public readonly string Value;

		public StringOperator(string value)
			: base(OperatorType.String) {
			Value = value;
		}

		public override string ToString() {
			return "\"" + Value + "\"";
		}

		public override object Clone() {
			return new StringOperator(Value);
		}
	}

	public abstract class UnaryOperator : Operator
	{
		public readonly Operator Right;

		public UnaryOperator(OperatorType op, Operator right)
			: base(op)
		{
			Right = right;
		}

		public override object Clone() {
			throw new NotImplementedException();
		}
	}

	public class OptionalOperator : UnaryOperator
	{
		public OptionalOperator(Operator right)
			: base(OperatorType.Optional, right) {
		}

		public override string ToString() {
			return "[" + Right + "]";
		}

		public override object Clone() {
			return new OptionalOperator((Operator)Right.Clone());
		}
	}

	public class RepeatOperator : UnaryOperator
	{
		public RepeatOperator(Operator right)
			: base(OperatorType.Repeat, right) {
		}

		public override string ToString() {
			return "{" + Right + "}";
		}

		public override object Clone() {
			return new RepeatOperator((Operator)Right.Clone());
		}
	}

	public abstract class LogicalOperator : Operator
	{
		public readonly Operator Left;
		public readonly Operator Right;

		public LogicalOperator(OperatorType op, Operator left, Operator right)
			: base(op)
		{
			Left = left;
			Right = right;
		}

		public override object Clone() {
			throw new NotImplementedException();
		}
	}

	public class LogicalAndOperator : LogicalOperator
	{
		public LogicalAndOperator(Operator left, Operator right)
			: base(OperatorType.LogicalAnd, left, right) {
		}

		public override string ToString() {
			return "(" + Left + " " + Right + ")";
		}

		public override object Clone() {
			return new LogicalAndOperator((Operator)Left.Clone(), (Operator)Right.Clone());
		}
	}

	public class LogicalOrOperator : LogicalOperator
	{
		public LogicalOrOperator(Operator left, Operator right)
			: base(OperatorType.LogicalOr, left, right) {
		}

		public override string ToString() {
			return "(" + Left + " | " + Right + ")";
		}

		public override object Clone() {
			return new LogicalOrOperator((Operator)Left.Clone(), (Operator)Right.Clone());
		}
	}
}
