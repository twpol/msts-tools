//------------------------------------------------------------------------------
// Jgr library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Jgr {
	public static class LinqExtensions {
		public static IEnumerable<T> AsEnumerable<T>(this T self) {
			return new T[] { self };
		}

		public static IEnumerable<T> Prefix<T>(this IEnumerable<T> second, IEnumerable<T> first) {
			return first.Concat(second);
		}

		public static IEnumerable<T> Prefix<T>(this IEnumerable<T> source, T item) {
			return source.Prefix(new T[] { item });
		}

		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item) {
			return source.Concat(new T[] { item });
		}

		public static Stack<T> AsStack<T>(this IEnumerable<T> source) {
			return new Stack<T>(source.Reverse());
		}
	}
}
