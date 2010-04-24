namespace Jgr.Gui {
	partial class FeedbackPrompt {
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
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.TextEmail = new System.Windows.Forms.TextBox();
			this.TextApplication = new System.Windows.Forms.TextBox();
			this.TextComments = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.ButtonSend = new System.Windows.Forms.Button();
			this.ButtonCancel = new System.Windows.Forms.Button();
			this.LinkViewAll = new System.Windows.Forms.LinkLabel();
			this.TextType = new System.Windows.Forms.TextBox();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label4, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.label5, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.TextEmail, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.TextApplication, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.TextComments, 1, 3);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.TextType, 1, 1);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(470, 219);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(3, 29);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(99, 26);
			this.label3.TabIndex = 2;
			this.label3.Text = "Type:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(3, 58);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(99, 26);
			this.label4.TabIndex = 4;
			this.label4.Text = "E-mail (optional):";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(3, 87);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(99, 26);
			this.label5.TabIndex = 6;
			this.label5.Text = "Comments:";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// TextEmail
			// 
			this.TextEmail.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TextEmail.Location = new System.Drawing.Point(108, 61);
			this.TextEmail.Name = "TextEmail";
			this.TextEmail.Size = new System.Drawing.Size(359, 23);
			this.TextEmail.TabIndex = 5;
			// 
			// TextApplication
			// 
			this.TextApplication.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TextApplication.Enabled = false;
			this.TextApplication.Location = new System.Drawing.Point(108, 3);
			this.TextApplication.Name = "TextApplication";
			this.TextApplication.Size = new System.Drawing.Size(359, 23);
			this.TextApplication.TabIndex = 1;
			this.TextApplication.Text = "FIXME";
			// 
			// TextComments
			// 
			this.TextComments.AcceptsReturn = true;
			this.TextComments.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TextComments.Location = new System.Drawing.Point(108, 90);
			this.TextComments.Multiline = true;
			this.TextComments.Name = "TextComments";
			this.TextComments.Size = new System.Drawing.Size(359, 126);
			this.TextComments.TabIndex = 7;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(99, 26);
			this.label1.TabIndex = 0;
			this.label1.Text = "Application:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// ButtonSend
			// 
			this.ButtonSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ButtonSend.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.ButtonSend.Location = new System.Drawing.Point(251, 237);
			this.ButtonSend.Name = "ButtonSend";
			this.ButtonSend.Size = new System.Drawing.Size(150, 23);
			this.ButtonSend.TabIndex = 1;
			this.ButtonSend.Text = "Send Feedback";
			this.ButtonSend.UseVisualStyleBackColor = true;
			// 
			// ButtonCancel
			// 
			this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.ButtonCancel.Location = new System.Drawing.Point(407, 237);
			this.ButtonCancel.Name = "ButtonCancel";
			this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
			this.ButtonCancel.TabIndex = 2;
			this.ButtonCancel.Text = "Cancel";
			this.ButtonCancel.UseVisualStyleBackColor = true;
			// 
			// LinkViewAll
			// 
			this.LinkViewAll.AutoSize = true;
			this.LinkViewAll.Location = new System.Drawing.Point(12, 241);
			this.LinkViewAll.Name = "LinkViewAll";
			this.LinkViewAll.Size = new System.Drawing.Size(124, 15);
			this.LinkViewAll.TabIndex = 0;
			this.LinkViewAll.TabStop = true;
			this.LinkViewAll.Text = "View all collected data";
			this.LinkViewAll.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkViewAll_LinkClicked);
			// 
			// TextType
			// 
			this.TextType.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TextType.Enabled = false;
			this.TextType.Location = new System.Drawing.Point(108, 32);
			this.TextType.Name = "TextType";
			this.TextType.Size = new System.Drawing.Size(359, 23);
			this.TextType.TabIndex = 8;
			this.TextType.Text = "FIXME";
			// 
			// FeedbackPrompt
			// 
			this.AcceptButton = this.ButtonSend;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.ButtonCancel;
			this.ClientSize = new System.Drawing.Size(494, 272);
			this.Controls.Add(this.LinkViewAll);
			this.Controls.Add(this.ButtonCancel);
			this.Controls.Add(this.ButtonSend);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FeedbackPrompt";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Send Feedback";
			this.Shown += new System.EventHandler(this.Feedback_Shown);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button ButtonSend;
		private System.Windows.Forms.Button ButtonCancel;
		private System.Windows.Forms.LinkLabel LinkViewAll;
		public System.Windows.Forms.TextBox TextEmail;
		public System.Windows.Forms.TextBox TextApplication;
		public System.Windows.Forms.TextBox TextComments;
		public System.Windows.Forms.TextBox TextType;
	}
}