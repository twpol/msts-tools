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
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label3;
			this.TestTableLayout = new System.Windows.Forms.TableLayoutPanel();
			this.TestResults = new System.Windows.Forms.Button();
			this.TestProgress = new System.Windows.Forms.ProgressBar();
			this.TestPath = new System.Windows.Forms.Label();
			this.TestPathBrowse = new System.Windows.Forms.Button();
			this.TestFileStatus = new System.Windows.Forms.Label();
			this.TestFormats = new System.Windows.Forms.ComboBox();
			this.TestTests = new System.Windows.Forms.ComboBox();
			this.TestPathBrowseDialog = new System.Windows.Forms.FolderBrowserDialog();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			this.TestTableLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Dock = System.Windows.Forms.DockStyle.Fill;
			label1.Location = new System.Drawing.Point(0, 0);
			label1.Margin = new System.Windows.Forms.Padding(0);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(58, 31);
			label1.TabIndex = 6;
			label1.Text = "Directory:";
			label1.UseMnemonic = false;
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Dock = System.Windows.Forms.DockStyle.Fill;
			label2.Location = new System.Drawing.Point(0, 31);
			label2.Margin = new System.Windows.Forms.Padding(0);
			label2.Name = "label2";
			label2.Size = new System.Drawing.Size(58, 29);
			label2.TabIndex = 11;
			label2.Text = "Formats:";
			label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			label2.UseMnemonic = false;
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Dock = System.Windows.Forms.DockStyle.Fill;
			label3.Location = new System.Drawing.Point(0, 60);
			label3.Margin = new System.Windows.Forms.Padding(0);
			label3.Name = "label3";
			label3.Size = new System.Drawing.Size(58, 29);
			label3.TabIndex = 12;
			label3.Text = "Tests:";
			label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			label3.UseMnemonic = false;
			// 
			// TestTableLayout
			// 
			this.TestTableLayout.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.TestTableLayout.ColumnCount = 3;
			this.TestTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.TestTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.TestTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.TestTableLayout.Controls.Add(this.TestResults, 2, 4);
			this.TestTableLayout.Controls.Add(label2, 0, 1);
			this.TestTableLayout.Controls.Add(this.TestProgress, 0, 5);
			this.TestTableLayout.Controls.Add(label1, 0, 0);
			this.TestTableLayout.Controls.Add(this.TestPath, 1, 0);
			this.TestTableLayout.Controls.Add(this.TestPathBrowse, 2, 0);
			this.TestTableLayout.Controls.Add(this.TestFileStatus, 0, 4);
			this.TestTableLayout.Controls.Add(label3, 0, 2);
			this.TestTableLayout.Controls.Add(this.TestFormats, 1, 1);
			this.TestTableLayout.Controls.Add(this.TestTests, 1, 2);
			this.TestTableLayout.Location = new System.Drawing.Point(12, 12);
			this.TestTableLayout.Name = "TestTableLayout";
			this.TestTableLayout.RowCount = 6;
			this.TestTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.TestTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.TestTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.TestTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.TestTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.TestTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.TestTableLayout.Size = new System.Drawing.Size(600, 123);
			this.TestTableLayout.TabIndex = 6;
			// 
			// TestResults
			// 
			this.TestResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestResults.Enabled = false;
			this.TestResults.Location = new System.Drawing.Point(522, 95);
			this.TestResults.Name = "TestResults";
			this.TestTableLayout.SetRowSpan(this.TestResults, 2);
			this.TestResults.Size = new System.Drawing.Size(75, 25);
			this.TestResults.TabIndex = 15;
			this.TestResults.Text = "Results";
			this.TestResults.UseVisualStyleBackColor = true;
			this.TestResults.Click += new System.EventHandler(this.TestResults_Click);
			// 
			// TestProgress
			// 
			this.TestTableLayout.SetColumnSpan(this.TestProgress, 2);
			this.TestProgress.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestProgress.Location = new System.Drawing.Point(3, 110);
			this.TestProgress.Name = "TestProgress";
			this.TestProgress.Size = new System.Drawing.Size(513, 10);
			this.TestProgress.TabIndex = 10;
			// 
			// TestPath
			// 
			this.TestPath.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestPath.Location = new System.Drawing.Point(58, 0);
			this.TestPath.Margin = new System.Windows.Forms.Padding(0);
			this.TestPath.Name = "TestPath";
			this.TestPath.Size = new System.Drawing.Size(461, 31);
			this.TestPath.TabIndex = 8;
			this.TestPath.Text = "<path here>";
			this.TestPath.UseMnemonic = false;
			// 
			// TestPathBrowse
			// 
			this.TestPathBrowse.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestPathBrowse.Location = new System.Drawing.Point(522, 3);
			this.TestPathBrowse.Name = "TestPathBrowse";
			this.TestPathBrowse.Size = new System.Drawing.Size(75, 25);
			this.TestPathBrowse.TabIndex = 7;
			this.TestPathBrowse.Text = "Browse...";
			this.TestPathBrowse.UseVisualStyleBackColor = true;
			this.TestPathBrowse.Click += new System.EventHandler(this.TestPathBrowse_Click);
			// 
			// TestFileStatus
			// 
			this.TestTableLayout.SetColumnSpan(this.TestFileStatus, 2);
			this.TestFileStatus.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestFileStatus.Location = new System.Drawing.Point(0, 92);
			this.TestFileStatus.Margin = new System.Windows.Forms.Padding(0);
			this.TestFileStatus.Name = "TestFileStatus";
			this.TestFileStatus.Size = new System.Drawing.Size(519, 15);
			this.TestFileStatus.TabIndex = 9;
			this.TestFileStatus.Text = "Found 0 files to test.";
			// 
			// TestFormats
			// 
			this.TestFormats.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestFormats.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.TestFormats.FormattingEnabled = true;
			this.TestFormats.Location = new System.Drawing.Point(61, 34);
			this.TestFormats.Name = "TestFormats";
			this.TestFormats.Size = new System.Drawing.Size(455, 23);
			this.TestFormats.TabIndex = 13;
			this.TestFormats.SelectedIndexChanged += new System.EventHandler(this.TestFormats_SelectedIndexChanged);
			// 
			// TestTests
			// 
			this.TestTests.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestTests.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.TestTests.FormattingEnabled = true;
			this.TestTests.Location = new System.Drawing.Point(61, 63);
			this.TestTests.Name = "TestTests";
			this.TestTests.Size = new System.Drawing.Size(455, 23);
			this.TestTests.TabIndex = 14;
			this.TestTests.SelectedIndexChanged += new System.EventHandler(this.TestTests_SelectedIndexChanged);
			// 
			// TestPathBrowseDialog
			// 
			this.TestPathBrowseDialog.ShowNewFolderButton = false;
			// 
			// Test
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 147);
			this.Controls.Add(this.TestTableLayout);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Test";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Test Simis Files";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Test_FormClosing);
			this.TestTableLayout.ResumeLayout(false);
			this.TestTableLayout.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label TestPath;
		private System.Windows.Forms.Button TestPathBrowse;
		private System.Windows.Forms.ProgressBar TestProgress;
		private System.Windows.Forms.Label TestFileStatus;
		private System.Windows.Forms.ComboBox TestFormats;
		private System.Windows.Forms.ComboBox TestTests;
		private System.Windows.Forms.FolderBrowserDialog TestPathBrowseDialog;
		private System.Windows.Forms.TableLayoutPanel TestTableLayout;
		private System.Windows.Forms.Button TestResults;
	}
}