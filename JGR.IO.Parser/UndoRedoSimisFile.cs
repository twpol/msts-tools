//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace Jgr.IO.Parser {
	/// <summary>
	/// A support class for using <see cref="SimisReader"/> and <see cref="SimisWriter"/> with on-disk files and editing capabilities.
	/// </summary>
	public class UndoRedoSimisFile : MutableSimisFile {
		Stack<SimisTreeNode> UndoBuffer { get; set; }
		Stack<SimisTreeNode> RedoBuffer { get; set; }

		public UndoRedoSimisFile(string fileName, SimisProvider provider)
			: base(fileName, provider) {
			UndoBuffer = new Stack<SimisTreeNode>();
			RedoBuffer = new Stack<SimisTreeNode>();
			ResetUndo(new SimisTreeNode("<root>", ""));
		}

		/// <summary>
		/// Gets or sets the root <see cref="SimisTreeNode"/> for the tree read or written by this class.
		/// </summary>
		/// <remarks>
		/// <para>Setting the <see cref="Tree"/> will add to the available undo buffers and reset the redo buffers.</para>
		/// </remarks>
		public override SimisTreeNode Tree {
			get {
				return base.Tree;
			}
			set {
				UndoBuffer.Push(base.Tree);
				RedoBuffer.Clear();
				base.Tree = value;
			}
		}

		void ResetUndo() {
			UndoBuffer.Clear();
			RedoBuffer.Clear();
		}

		void ResetUndo(SimisTreeNode newTree) {
			ResetUndo();
			base.Tree = newTree;
		}

		/// <summary>
		/// Switches to the previous <see cref="SimisTreeNode"/> root.
		/// </summary>
		public void Undo() {
			RedoBuffer.Push(base.Tree);
			base.Tree = UndoBuffer.Pop();
		}

		/// <summary>
		/// Switches to the next <see cref="SimisTreeNode"/> root.
		/// </summary>
		public void Redo() {
			UndoBuffer.Push(base.Tree);
			base.Tree = RedoBuffer.Pop();
		}

		/// <summary>
		/// Gets a <see cref="bool"/> indicating whether undo is available.
		/// </summary>
		public bool CanUndo {
			get {
				return UndoBuffer.Count > 0;
			}
		}

		/// <summary>
		/// Gets a <see cref="bool"/> indicating whether redo is available.
		/// </summary>
		public bool CanRedo {
			get {
				return RedoBuffer.Count > 0;
			}
		}

		public override void Read() {
			base.Read();
			ResetUndo();
		}

		public override void Read(Stream stream) {
			base.Read(stream);
			ResetUndo();
		}
	}
}