namespace SimisEditor
{
	partial class Messages
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.ListOfMessages = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
			this.MessageText = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// ListOfMessages
			// 
			this.ListOfMessages.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ListOfMessages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
			this.ListOfMessages.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ListOfMessages.FullRowSelect = true;
			this.ListOfMessages.GridLines = true;
			this.ListOfMessages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.ListOfMessages.HideSelection = false;
			this.ListOfMessages.Location = new System.Drawing.Point(0, 0);
			this.ListOfMessages.Name = "ListOfMessages";
			this.ListOfMessages.Size = new System.Drawing.Size(624, 294);
			this.ListOfMessages.TabIndex = 0;
			this.ListOfMessages.UseCompatibleStateImageBehavior = false;
			this.ListOfMessages.View = System.Windows.Forms.View.Details;
			this.ListOfMessages.SelectedIndexChanged += new System.EventHandler(this.ListOfMessages_SelectedIndexChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Source";
			this.columnHeader1.Width = 70;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Level";
			this.columnHeader2.Width = 40;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Message";
			// 
			// MessageText
			// 
			this.MessageText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.MessageText.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.MessageText.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MessageText.Location = new System.Drawing.Point(0, 294);
			this.MessageText.Multiline = true;
			this.MessageText.Name = "MessageText";
			this.MessageText.ReadOnly = true;
			this.MessageText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.MessageText.Size = new System.Drawing.Size(624, 148);
			this.MessageText.TabIndex = 2;
			this.MessageText.Text = "1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\r\n10";
			// 
			// Messages
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 442);
			this.Controls.Add(this.ListOfMessages);
			this.Controls.Add(this.MessageText);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Messages";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Messages";
			this.Shown += new System.EventHandler(this.Messages_Shown);
			this.Resize += new System.EventHandler(this.Messages_Resize);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView ListOfMessages;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.TextBox MessageText;
	}
}