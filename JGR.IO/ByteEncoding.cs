//------------------------------------------------------------------------------
// JGR.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JGR.IO
{
	/// <summary>
	/// A basic encoding which maps bytes 0-255 to Unicode characters 0-255.
	/// </summary>
	public class ByteEncoding : Encoding
	{
		public ByteEncoding() {
		}

		public override int GetByteCount(char[] chars, int index, int count) {
			return count;
		}

		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
			for (var i = 0; i < charCount; i++) {
				bytes[i + byteIndex] = (byte)chars[i + charIndex];
			}
			return charCount;
		}

		public override int GetCharCount(byte[] bytes, int index, int count) {
			return count;
		}

		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
			for (var i = 0; i < byteCount; i++) {
				chars[i + charIndex] = (char)bytes[i + byteIndex];
			}
			return byteCount;
		}

		public override int GetMaxByteCount(int charCount) {
			return charCount;
		}

		public override int GetMaxCharCount(int byteCount) {
			return byteCount;
		}
	}
}
