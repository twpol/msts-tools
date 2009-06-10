namespace SimisEditor
{
	partial class Test
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
			this.TestPath = new System.Windows.Forms.Label();
			this.TestProgress = new System.Windows.Forms.ProgressBar();
			this.TestFileStatus = new System.Windows.Forms.Label();
			this.TestRun = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// TestPath
			// 
			this.TestPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.TestPath.Location = new System.Drawing.Point(12, 9);
			this.TestPath.Name = "TestPath";
			this.TestPath.Size = new System.Drawing.Size(600, 30);
			this.TestPath.TabIndex = 0;
			this.TestPath.Text = "<path here>";
			this.TestPath.UseMnemonic = false;
			// 
			// TestProgress
			// 
			this.TestProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.TestProgress.Location = new System.Drawing.Point(12, 57);
			this.TestProgress.Name = "TestProgress";
			this.TestProgress.Size = new System.Drawing.Size(519, 10);
			this.TestProgress.TabIndex = 1;
			// 
			// TestFileStatus
			// 
			this.TestFileStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.TestFileStatus.Location = new System.Drawing.Point(12, 39);
			this.TestFileStatus.Name = "TestFileStatus";
			this.TestFileStatus.Size = new System.Drawing.Size(519, 15);
			this.TestFileStatus.TabIndex = 2;
			this.TestFileStatus.Text = "Found 0 files to test.";
			// 
			// TestRun
			// 
			this.TestRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.TestRun.Enabled = false;
			this.TestRun.Location = new System.Drawing.Point(537, 42);
			this.TestRun.Name = "TestRun";
			this.TestRun.Size = new System.Drawing.Size(75, 25);
			this.TestRun.TabIndex = 3;
			this.TestRun.Text = "Run Test";
			this.TestRun.UseVisualStyleBackColor = true;
			this.TestRun.Click += new System.EventHandler(this.TestRun_Click);
			// 
			// Test
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 79);
			this.Controls.Add(this.TestRun);
			this.Controls.Add(this.TestFileStatus);
			this.Controls.Add(this.TestProgress);
			this.Controls.Add(this.TestPath);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Test";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Test Simis Files";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Test_FormClosing);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label TestPath;
		private System.Windows.Forms.ProgressBar TestProgress;
		private System.Windows.Forms.Label TestFileStatus;
		private System.Windows.Forms.Button TestRun;
	}
}