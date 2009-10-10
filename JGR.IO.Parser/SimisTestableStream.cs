//------------------------------------------------------------------------------
// JGR.IO library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Jgr.IO.Parser
{
	public class SimisTestableStream : Stream
	{
		Stream UncompressedStream;

		public SimisTestableStream(Stream baseStream) {
			UncompressedStream = new UnclosableStream(new MemoryStream());
			baseStream = new UnclosableStream(baseStream);
			baseStream.Seek(0, SeekOrigin.Begin);

			var start = baseStream.Position;
			var streamCompressed = false;
			var binaryReader = new BinaryReader(baseStream, new ByteEncoding());
			var binaryWriter = new BinaryWriter(UncompressedStream, new ByteEncoding());
			{
				var sr = new StreamReader(baseStream, true);
				sr.ReadLine();
				if (!(sr.CurrentEncoding is UTF8Encoding)) {
					binaryReader.Close();
					binaryWriter.Close();
					binaryReader = new BinaryReader(baseStream, sr.CurrentEncoding);
					binaryWriter = new BinaryWriter(UncompressedStream, sr.CurrentEncoding);
					start += sr.CurrentEncoding.GetPreamble().Length;
					binaryWriter.Write(sr.CurrentEncoding.GetPreamble());
				}
			}
			baseStream.Position = start;

			{
				var signature = String.Join("", binaryReader.ReadChars(8).Select<char, string>(c => c.ToString()).ToArray<string>());
				if ((signature != "SIMISA@F") && (signature != "SIMISA@@")) {
					throw new InvalidDataException("Signature '" + signature + "' is invalid.");
				}
				streamCompressed = (signature == "SIMISA@F");
				binaryWriter.Write("SIMISA@@".ToCharArray());
			}

			if (streamCompressed) {
				// This is a compressed stream. Read in the uncompressed size and DEFLATE the rest.
				var uncompressedSize = binaryReader.ReadUInt32();
				var streamLength = binaryReader.BaseStream.Position + uncompressedSize;
				{
					var signature = String.Join("", binaryReader.ReadChars(4).Select<char, string>(c => c.ToString()).ToArray<string>());
					if (signature != "@@@@") {
						throw new InvalidDataException("Signature '" + signature + "' is invalid.");
					}
				}
				// The stream is technically ZLIB, but we assume the selected ZLIB compression is DEFLATE (though we verify that here just in case). The ZLIB
				// header for DEFLATE is 0x78 0x9C (apparently).
				{
					var zlibHeader = binaryReader.ReadBytes(2);
					if ((zlibHeader[0] != 0x78) || (zlibHeader[1] != 0x9C)) {
						throw new InvalidDataException("ZLIB signature is invalid.");
					}
				}

				// BinaryReader -> BufferedInMemoryStream -> DeflateStream -> BinaryReader -> BaseStream.
				// The BufferedInMemoryStream is needed because DeflateStream only supports reading forwards - no seeking - and we'll potentially be jumping around.
				binaryReader.Close();
				binaryReader = new BinaryReader(new BufferedInMemoryStream(new DeflateStream(baseStream, CompressionMode.Decompress)), new ByteEncoding());
			} else {
				var signature = String.Join("", binaryReader.ReadChars(8).Select<char, string>(c => c.ToString()).ToArray<string>());
				if (signature != "@@@@@@@@") {
					throw new InvalidDataException("Signature '" + signature + "' is invalid.");
				}
			}
			binaryWriter.Write("@@@@@@@@".ToCharArray());

			var isText = false;
			{
				var signature = String.Join("", binaryReader.ReadChars(16).Select<char, string>(c => c.ToString()).ToArray<string>());
				if (signature.Substring(0, 5) != "JINX0") {
					throw new InvalidDataException("Signature '" + signature + "' is invalid.");
				}
				if (signature.Substring(8, 8) != "______\r\n") {
					throw new InvalidDataException("Signature '" + signature + "' is invalid.");
				}
				isText = (signature[7] == 't');
				binaryWriter.Write(signature.ToCharArray());
			}

			var inWhitespace = false;
			while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length) {
				var bite = binaryReader.ReadChar();
				if (isText) {
					if ((bite == '(') || (bite == ')')) {
						if (!inWhitespace) binaryWriter.Write(' ');
					}
				}
				var isWhitespace = (bite == '\t') || (bite == '\n') || (bite == '\r') || (bite == ' ');
				if (!isWhitespace || !inWhitespace || !isText) binaryWriter.Write(isWhitespace && isText ? ' ' : bite);
				inWhitespace = isWhitespace;
				if (isText) {
					if ((bite == '(') || (bite == ')')) {
						binaryWriter.Write(' ');
						inWhitespace = true;
					}
				}
			}

			binaryReader.Close();
			binaryWriter.Close();
			UncompressedStream.Seek(0, SeekOrigin.Begin);
		}

		public override void Close() {
			base.Close();
			UncompressedStream.Close();
		}

		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return true;
			}
		}

		public override bool CanWrite {
			get {
				return false;
			}
		}

		public override void Flush() {
			throw new NotImplementedException();
		}

		public override long Length {
			get {
				return UncompressedStream.Length;
			}
		}

		public override long Position {
			get {
				return UncompressedStream.Position;
			}
			set {
				UncompressedStream.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return UncompressedStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			return UncompressedStream.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			throw new NotImplementedException();
		}
	}
}
