using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jgr.IO.Parser;
using System.IO;
using Jgr.IO;

namespace Normalize
{
	class Program
	{
		static void Main(string[] args) {
			foreach (var arg in args) {
				try {
					var uc = new SimisTestableStream(File.OpenRead(arg));
					var newFile = arg.Substring(arg.LastIndexOf("\\") + 1);
					if (arg == newFile) newFile += ".uncompressed";
					using (var of = File.OpenWrite(newFile)) {
						using (var ofw = new BinaryWriter(of, new ByteEncoding())) {
							while (uc.Position < uc.Length) {
								ofw.Write((byte)uc.ReadByte());
							}
						}
					}
				} catch (Exception e) {
					Console.WriteLine(arg);
					Console.WriteLine(e.ToString());
				}
			}
		}
	}
}
