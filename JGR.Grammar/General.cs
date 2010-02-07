//------------------------------------------------------------------------------
// Jgr.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;

namespace Jgr.Grammar {
	public enum OperatorType {
		Reference,
		String,
		Optional,
		Repeat,
		LogicalOr,
		LogicalAnd
	}

	/// <summary>
	/// Base class for all operators within an expression.
	/// </summary>
	[Immutable]
	public abstract class Operator : ICloneable {
		public OperatorType Op { get; private set; }

		protected Operator(OperatorType op) {
			Op = op;
		}

		#region ICloneable Members

		public virtual object Clone() {
			throw new NotImplementedException();
		}

		#endregion
	}

	/// <summary>
	/// Operator which represents a reference to an object, as defined by the caller.
	/// </summary>
	[Immutable]
	public class ReferenceOperator : Operator {
		public string Reference { get; private set; }

		protected ReferenceOperator(OperatorType op, string reference)
			: base(op) {
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

	/// <summary>
	/// Operator which represents a reference to an object, with its own name, as defined by the caller.
	/// </summary>
	[Immutable]
	public class NamedReferenceOperator : ReferenceOperator {
		public string Name { get; private set; }

		public NamedReferenceOperator(string name, string reference)
			: base(OperatorType.Reference, reference) {
			Name = name;
		}

		public override string ToString() {
			return base.ToString() + "," + Name;
		}

		public override object Clone() {
			return new NamedReferenceOperator(Name, Reference);
		}
	}

	/// <summary>
	/// Operator which represents a literal string.
	/// </summary>
	[Immutable]
	public class StringOperator : Operator {
		public string Value { get; private set; }

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

	/// <summary>
	/// Operator which represents all unary operations (operations on a single expression).
	/// </summary>
	[Immutable]
	public abstract class UnaryOperator : Operator {
		public Operator Right { get; private set; }

		protected UnaryOperator(OperatorType op, Operator right)
			: base(op) {
			Right = right;
		}

		public override object Clone() {
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Operator which represents an optional expression.
	/// </summary>
	[Immutable]
	public class OptionalOperator : UnaryOperator {
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

	/// <summary>
	/// Operator which represents a repeating (one or more times) expression.
	/// </summary>
	[Immutable]
	public class RepeatOperator : UnaryOperator {
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

	/// <summary>
	/// Operator which represents all binary operations (operations on two expressions).
	/// </summary>
	[Immutable]
	public abstract class BinaryOperator : Operator {
		public Operator Left { get; private set; }
		public Operator Right { get; private set; }

		protected BinaryOperator(OperatorType op, Operator left, Operator right)
			: base(op) {
			Left = left;
			Right = right;
		}

		public override object Clone() {
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Operator which represents a logical "and" operation.
	/// </summary>
	[Immutable]
	public class LogicalAndOperator : BinaryOperator {
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

	/// <summary>
	/// Operator which represents a logical "or" operation.
	/// </summary>
	[Immutable]
	public class LogicalOrOperator : BinaryOperator {
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
