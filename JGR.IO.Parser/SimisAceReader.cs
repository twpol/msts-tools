//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jgr.IO.Parser {
	public class SimisAceReader : SimisReader {
		public SimisAceReader(SimisStreamReader reader)
			: base(reader) {
		}

		public SimisAce Read() {
			// ACE format flags:
			//   01 = has mipmap images.
			//   02 = ?
			//   04 = ?
			//   08 = ?
			//   10 = is DXT compressed.
			// 00 = typical bitmap.
			// 05 = typical uncompressed texture (mips).
			// 15 = typical compressed texture (DXT with mips).
			var format = Reader.ReadInt32();
			if (!new[] { 0x00, 0x01, 0x05, 0x10, 0x11, 0x15 }.Contains(format)) {
				throw new ReaderException(Reader, true, 4, "ACE format 0x" + format.ToString("X") + " is not supported.");
			}
			var width = Reader.ReadInt32();
			var height = Reader.ReadInt32();
			// TODO: Work out what this int is for. It seems to change with the format but not match it. Could be DirectX surface format?
			//   0E = D3DFMT_R5G6B5?
			//   10 = D3DFMT_A1R5G5B5?
			//   11 = D3DFMT_A4R4G4B4?
			//   12 = D3DFMT_DXT1?
			//   14 = D3DFMT_DXT3???
			//   16 = D3DFMT_DXT5???
			var unknown4 = Reader.ReadInt32();
			var channelCount = Reader.ReadInt32();
			// TODO: Work out what this int is for. Tends to be zero.
			var unknown6 = Reader.ReadInt32();
			var unknown7 = new String(Reader.ReadChars(16));
			if (unknown7.Contains('\0')) {
				unknown7 = unknown7.Substring(0, unknown7.IndexOf('\0'));
			}
			var creator = new String(Reader.ReadChars(64));
			if (creator.Contains('\0')) {
				creator = creator.Substring(0, creator.IndexOf('\0'));
			}

			// TODO: Work out what these 44 bytes are for; they are sometimes all zero, sometimes not.
			var unknown9 = Reader.ReadBytes(44);

			var channels = new List<SimisAceChannel>();
			for (var channel = 0; channel < channelCount; channel++) {
				channels.Add(new SimisAceChannel((byte)Reader.ReadUInt64(), (SimisAceChannelId)Reader.ReadUInt64()));
			}

			return new SimisAce(format, width, height, unknown4, channelCount, unknown6, unknown7, creator, unknown9, channels.ToArray(), new SimisAceImage[0]);
		}
	}
}
