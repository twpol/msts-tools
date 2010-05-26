//------------------------------------------------------------------------------
// Jgr.Grammar library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;

namespace Jgr.Grammar {
	/// <summary>
	/// Contains the possible operations found in an expression.
	/// </summary>
	public enum OperatorType {
		/// <summary>
		/// A reference to something else, such as another expression.
		/// </summary>
		Reference,
		/// <summary>
		/// A string literal.
		/// </summary>
		String,
		/// <summary>
		/// A subexpression which may be used or skipped.
		/// </summary>
		Optional,
		/// <summary>
		/// A subexpression which may be used multiple times in a row.
		/// </summary>
		Repeat,
		/// <summary>
		/// Two subexpressions of which only one may be used at a time.
		/// </summary>
		LogicalOr,
		/// <summary>
		/// Two subexpressions that must both be used in order.
		/// </summary>
		LogicalAnd
	}

	/// <summary>
	/// Base class for all operators within an expression.
	/// </summary>
	[Immutable]
	public abstract class Operator : ICloneable {
		public OperatorType Op { get; private set; }

		/// <summary>
		/// Internal. Initializes a new instance of the <see cref="Operator"/> class with a given <see cref="OperatorType"/>.
		/// </summary>
		/// <param name="op">The <see cref="OperatorType"/> being constructed.</param>
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

		/// <summary>
		/// Internal. Initializes a new instance of the <see cref="ReferenceOperator"/> class with a given <see cref="OperatorType"/> and reference.
		/// </summary>
		/// <param name="op">The <see cref="OperatorType"/> being constructed.</param>
		/// <param name="reference">The reference for this operator.</param>
		protected ReferenceOperator(OperatorType op, string reference)
			: base(op) {
			Reference = reference;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceOperator"/> class with a given reference.
		/// </summary>
		/// <param name="reference">The reference for this operator.</param>
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

		/// <summary>
		/// Initializes a new instance of the <see cref="NamedReferenceOperator"/> class with a given name and reference. 
		/// </summary>
		/// <param name="name">The name for this reference.</param>
		/// <param name="reference">The reference for this operator.</param>
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

		/// <summary>
		/// Initializes a new instance of the <see cref="StringOperator"/> class with a given literal string. 
		/// </summary>
		/// <param name="value">The string for this operator.</param>
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
		
		/// <summary>
		/// Internal. Initializes a new instance of the <see cref="UnaryOperator"/> class with a given <see cref="OperatorType"/> and <see cref="Operator"/> subexpression. 
		/// </summary>
		/// <param name="op">The <see cref="OperatorType"/> being constructed.</param>
		/// <param name="right">The <see cref="Operator"/> subexpression.</param>
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
		/// <summary>
		/// Initializes a new instance of the <see cref="OptionalOperator"/> class with a given <see cref="Operator"/> subexpression.
		/// </summary>
		/// <param name="right">The <see cref="Operator"/> subexpression.</param>
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
		/// <summary>
		/// Initializes a new instance of the <see cref="RepeatOperator"/> class with a given <see cref="Operator"/> subexpression.
		/// </summary>
		/// <param name="right">The <see cref="Operator"/> subexpression.</param>
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

		/// <summary>
		/// Internal. Initializes a new instance of the <see cref="BinaryOperator"/> class with a given <see cref="OperatorType"/> and two <see cref="Operator"/> subexpressions.
		/// </summary>
		/// <param name="op">The <see cref="OperatorType"/> being constructed.</param>
		/// <param name="left">The left <see cref="Operator"/> subexpression.</param>
		/// <param name="right">The right <see cref="Operator"/> subexpression.</param>
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
		/// <summary>
		/// Initializes a new instance of the <see cref="LogicalAndOperator"/> class with two <see cref="Operator"/> subexpressions.
		/// </summary>
		/// <param name="left">The left <see cref="Operator"/> subexpression.</param>
		/// <param name="right">The right <see cref="Operator"/> subexpression.</param>
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
		/// <summary>
		/// Initializes a new instance of the <see cref="LogicalOrOperator"/> class with two <see cref="Operator"/> subexpressions.
		/// </summary>
		/// <param name="left">The left <see cref="Operator"/> subexpression.</param>
		/// <param name="right">The right <see cref="Operator"/> subexpression.</param>
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
