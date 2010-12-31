//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace Jgr.IO.Parser {
	/// <summary>
	/// A support class for using <see cref="SimisReader"/> and <see cref="SimisWriter"/> with on-disk files and editing capabilities.
	/// </summary>
	public class UndoRedoSimisFile : MutableSimisFile {
		Stack<SimisFileState> UndoBuffer { get; set; }
		Stack<SimisFileState> RedoBuffer { get; set; }

		struct SimisFileState {
			public SimisTreeNode Tree;
			public SimisAce Ace;

			public SimisFileState(SimisTreeNode tree, SimisAce ace) {
				Tree = tree;
				Ace = ace;
			}
		}

		public UndoRedoSimisFile(string fileName, SimisProvider provider)
			: base(fileName, provider) {
			UndoBuffer = new Stack<SimisFileState>();
			RedoBuffer = new Stack<SimisFileState>();
			ResetUndo(new SimisTreeNode("<root>", ""), new SimisAce(0, 0, 0, 0, 0, "", "", new byte[44], new SimisAceChannel[0], new SimisAceImage[0], new byte[0], new byte[0]));
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
				if (base.Tree != value) {
					UndoBuffer.Push(new SimisFileState(base.Tree, base.Ace));
					RedoBuffer.Clear();
					base.Tree = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the root <see cref="SimisAce"/> for the image read or written by this class.
		/// </summary>
		/// <remarks>
		/// <para>Setting the <see cref="Ace"/> will add to the available undo buffers and reset the redo buffers.</para>
		/// </remarks>
		public override SimisAce Ace {
			get {
				return base.Ace;
			}
			set {
				if (base.Ace != value) {
					UndoBuffer.Push(new SimisFileState(base.Tree, base.Ace));
					RedoBuffer.Clear();
					base.Ace = value;
				}
			}
		}

		void ResetUndo() {
			UndoBuffer.Clear();
			RedoBuffer.Clear();
		}

		void ResetUndo(SimisTreeNode newTree, SimisAce newAce) {
			ResetUndo();
			base.Tree = newTree;
			base.Ace = newAce;
		}

		/// <summary>
		/// Switches to the previous <see cref="SimisTreeNode"/>/<see cref="SimisAce"/> root.
		/// </summary>
		public void Undo() {
			RedoBuffer.Push(new SimisFileState(base.Tree, base.Ace));
			var state = UndoBuffer.Pop();
			base.Tree = state.Tree;
			base.Ace = state.Ace;
		}

		/// <summary>
		/// Switches to the next <see cref="SimisTreeNode"/>/<see cref="SimisAce"/> root.
		/// </summary>
		public void Redo() {
			UndoBuffer.Push(new SimisFileState(base.Tree, base.Ace));
			var state = RedoBuffer.Pop();
			base.Tree = state.Tree;
			base.Ace = state.Ace;
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