//------------------------------------------------------------------------------
// Jgr library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;

namespace Jgr {
	/// <summary>
	/// Indicates that the class is intended to be immutable.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ImmutableAttribute : Attribute {
	}
}
