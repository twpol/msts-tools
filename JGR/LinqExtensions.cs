//------------------------------------------------------------------------------
// Jgr library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Jgr {
	/// <summary>
	/// Provides a number of useful extensions to .NET 3.5's LINQ methods.
	/// </summary>
	public static class LinqExtensions {
		/// <summary>
		/// Returns an <see cref="IEnumerable{T}"/> containing only <paramref name="self"/>.
		/// </summary>
		/// <typeparam name="T">The type of the element of <paramref name="self"/>.</typeparam>
		/// <param name="self">The object to return an enumeration containing.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing only <paramref name="self"/>.</returns>
		public static IEnumerable<T> AsEnumerable<T>(this T self) {
			return new T[] { self };
		}

		/// <summary>
		/// Prefixes a sequence with another sequence.
		/// </summary>
		/// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
		/// <param name="second">The sequence to prefix.</param>
		/// <param name="first">The sequence being prefixed to the <paramref name="second"/> sequence.</param>
		/// <returns>As <see cref="IEnumerable{T}"/> containing both sequences in <paramref name="first"/>, <paramref name="second"/> order.</returns>
		public static IEnumerable<T> Prefix<T>(this IEnumerable<T> second, IEnumerable<T> first) {
			return first.Concat(second);
		}

		/// <summary>
		/// Prefixes a sequence with a single item.
		/// </summary>
		/// <typeparam name="T">The type of the elements of the input sequence.</typeparam>
		/// <param name="source">The sequence to prefix.</param>
		/// <param name="item">The element being prefixed to the <paramref name="source"/> sequence.</param>
		/// <returns>As <see cref="IEnumerable{T}"/> containing <paramref name="item"/> followed by the <paramref name="source"/> sequence.</returns>
		public static IEnumerable<T> Prefix<T>(this IEnumerable<T> source, T item) {
			return source.Prefix(new T[] { item });
		}

		/// <summary>
		/// Concatenates a sequence with a single item.
		/// </summary>
		/// <typeparam name="T">The type of the elements of the input sequence.</typeparam>
		/// <param name="source">The sequence to concatenate.</param>
		/// <param name="item">The element being concatenated to the <paramref name="source"/> sequence.</param>
		/// <returns>As <see cref="IEnumerable{T}"/> containing the <paramref name="source"/> sequence followed by <paramref name="item"/>.</returns>
		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item) {
			return source.Concat(new T[] { item });
		}

		/// <summary>
		/// Converts a sequence into a <see cref="Stack{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the elements of the input sequence.</typeparam>
		/// <param name="source">The sequence to convert.</param>
		/// <returns>A <see cref="Stack{T}"/> containing the elements in the <paramref name="source"/> sequence.</returns>
		public static Stack<T> AsStack<T>(this IEnumerable<T> source) {
			return new Stack<T>(source.Reverse());
		}
	}
}
