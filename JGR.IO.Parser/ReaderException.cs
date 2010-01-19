//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Jgr.IO.Parser
{
	public class ReaderException : DescriptiveException
	{
		bool ExceptionBinary;
		long ExceptionAddress;
		long ExceptionAddressPrefix;
		long ExceptionAddressSuffix;
		long ExceptionLength;
		long ExceptionLengthPrefix;
		long ExceptionLengthSuffix;
		char[] ExceptionData;

		public ReaderException(BinaryReader reader, bool binary, int exceptionLength, string message)
			: this(reader, binary, -exceptionLength, exceptionLength, message, null) {
		}

		public ReaderException(BinaryReader reader, bool binary, int exceptionLength, string message, Exception innerException)
			: this(reader, binary, -exceptionLength, exceptionLength, message, innerException) {
		}

		ReaderException(BinaryReader reader, bool binary, int exceptionOffset, int exceptionLength, string message, Exception innerException)
			: base(message, innerException) {
			// Record the original address and exception address for later.
			var originalPosition = reader.BaseStream.Position;
			ExceptionAddress = originalPosition + exceptionOffset;
			ExceptionBinary = binary;
			// Read 1 character in so we can measure its length. Remember to reset the stream position.
			reader.BaseStream.Position = 0;
			reader.ReadChar();
			var charLength = reader.BaseStream.Position;
			reader.BaseStream.Position = ExceptionAddress;
			// The prefix and suffix should be 64 characters long.
			var ffixLength = 64 * charLength;
			// Position stream at the prefix start, or the start of the stream if it would underflow.
			if (reader.BaseStream.Position > ffixLength) {
				reader.BaseStream.Position -= ffixLength;
			} else {
				reader.BaseStream.Position = 0;
			}
			List<char> data = new List<char>();
			// Record the addresses of the prefix and suffix.
			ExceptionAddressPrefix = reader.BaseStream.Position;
			ExceptionAddressSuffix = ExceptionAddress + exceptionLength;
			// Read in prefix data until we reach the exception address, recording the prefix length as the number of characters.
			while (reader.BaseStream.Position < ExceptionAddress) {
				data.Add(reader.ReadChar());
				ExceptionLengthPrefix++;
			}
			// Reset position to exception address, in case we've got <1 character over.
			reader.BaseStream.Position = ExceptionAddress;
			// Read in exception data until we reach address + length, recording the exception length as the number of characters.
			while (reader.BaseStream.Position < ExceptionAddress + exceptionLength) {
				data.Add(reader.ReadChar());
				ExceptionLength++;
			}
			// Reset position to exception suffix address.
			reader.BaseStream.Position = ExceptionAddressSuffix;
			// Read in suffix data until we run out of stream or reach address + length + suffx length, recording the suffix length as the number of characters.
			while ((reader.PeekChar() != -1) && (reader.BaseStream.Position < ExceptionAddress + exceptionLength + ffixLength)) {
				data.Add(reader.ReadChar());
				ExceptionLengthSuffix++;
			}
			// Convert the character list into an array for use in ToString();
			ExceptionData = data.ToArray();
			// Reset reader in case anything else wants to play with it.
			reader.BaseStream.Position = originalPosition;
		}

		static string FormatExceptionData(char[] data, bool dataBinary, long dataBase, long offset, long length) {
			var formattedText = "";
			if (dataBinary) {
				var formattedHex = "";
				var formattedChar = "";
				for (var i = 0; i < length; i++) {
					formattedHex += (i % 4 == 0 ? "   " : " ");
					formattedHex += ((int)data[i + offset]).ToString("X2");
					formattedChar += (data[i + offset] < 32 || data[i + offset] >= 128 ? '.' : data[i + offset]);
					if (i % 16 == 15) {
						formattedText += formattedHex.Substring(1) + "  " + formattedChar + "\r\n";
						formattedHex = "";
						formattedChar = "";
					}
				}
				if (formattedHex.Length > 0) formattedText += (formattedHex.Substring(1) + "                                                       ").Substring(0, 55) + "  " + formattedChar + "\r\n";
			} else {
				formattedText = "  " + String.Join("", data.Select<char, string>(c => c < 32 && c != 9 && c != 10 && c != 13 ? "." : c.ToString()).ToArray<string>(), (int)offset, (int)length).Replace("\t", "    ").Replace("\n", "\n  ") + "\r\n";
			}
			return (formattedText.Length > 2 ? formattedText.Substring(0, formattedText.Length - 2) : formattedText);
		}

		public override string ToString() {
			return "From 0x" + ExceptionAddressPrefix.ToString("X8") + " - data preceding failure:\r\n" + FormatExceptionData(ExceptionData, ExceptionBinary, ExceptionAddressPrefix, 0, ExceptionLengthPrefix) + "\r\n\r\n"
				+ (ExceptionLength > 0 ?
					"From 0x" + ExceptionAddress.ToString("X8")      + " - data at failure:\r\n"        + FormatExceptionData(ExceptionData, ExceptionBinary, ExceptionAddress, ExceptionLengthPrefix, ExceptionLength) + "\r\n\r\n"
					: "")
				+ "From 0x" + ExceptionAddressSuffix.ToString("X8")  + " - data following failure:\r\n" + FormatExceptionData(ExceptionData, ExceptionBinary, ExceptionAddressSuffix, ExceptionLengthPrefix + ExceptionLength, ExceptionLengthSuffix) + "\r\n\r\n"
				+ base.ToString();
		}
	}
}
