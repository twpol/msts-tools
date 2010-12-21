namespace Tools
{
	partial class Tools
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.textBoxDescription = new System.Windows.Forms.TextBox();
			this.buttonLaunch = new System.Windows.Forms.Button();
			this.buttonExit = new System.Windows.Forms.Button();
			this.imageListIcons = new System.Windows.Forms.ImageList(this.components);
			this.listViewTools = new System.Windows.Forms.ListView();
			this.labelTool = new System.Windows.Forms.Label();
			this.buttonUpdate = new System.Windows.Forms.Button();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// textBoxDescription
			// 
			this.textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxDescription.BackColor = System.Drawing.SystemColors.Control;
			this.textBoxDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBoxDescription.Location = new System.Drawing.Point(218, 44);
			this.textBoxDescription.Multiline = true;
			this.textBoxDescription.Name = "textBoxDescription";
			this.textBoxDescription.ReadOnly = true;
			this.textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxDescription.Size = new System.Drawing.Size(354, 327);
			this.textBoxDescription.TabIndex = 2;
			// 
			// buttonLaunch
			// 
			this.buttonLaunch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonLaunch.Enabled = false;
			this.buttonLaunch.Location = new System.Drawing.Point(218, 377);
			this.buttonLaunch.Name = "buttonLaunch";
			this.buttonLaunch.Size = new System.Drawing.Size(200, 23);
			this.buttonLaunch.TabIndex = 3;
			this.buttonLaunch.Text = "Launch Tool";
			this.buttonLaunch.UseVisualStyleBackColor = true;
			this.buttonLaunch.Click += new System.EventHandler(this.buttonLaunch_Click);
			// 
			// buttonExit
			// 
			this.buttonExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonExit.Location = new System.Drawing.Point(497, 377);
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.Size = new System.Drawing.Size(75, 23);
			this.buttonExit.TabIndex = 4;
			this.buttonExit.Text = "Exit";
			this.buttonExit.UseVisualStyleBackColor = true;
			this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
			// 
			// imageListIcons
			// 
			this.imageListIcons.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.imageListIcons.ImageSize = new System.Drawing.Size(16, 16);
			this.imageListIcons.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// listViewTools
			// 
			this.listViewTools.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.listViewTools.Location = new System.Drawing.Point(12, 12);
			this.listViewTools.Name = "listViewTools";
			this.listViewTools.Size = new System.Drawing.Size(200, 359);
			this.listViewTools.SmallImageList = this.imageListIcons;
			this.listViewTools.TabIndex = 0;
			this.listViewTools.UseCompatibleStateImageBehavior = false;
			this.listViewTools.View = System.Windows.Forms.View.SmallIcon;
			this.listViewTools.SelectedIndexChanged += new System.EventHandler(this.listViewTools_SelectedIndexChanged);
			this.listViewTools.DoubleClick += new System.EventHandler(this.listViewTools_DoubleClick);
			// 
			// labelTool
			// 
			this.labelTool.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelTool.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelTool.Location = new System.Drawing.Point(218, 9);
			this.labelTool.Name = "labelTool";
			this.labelTool.Size = new System.Drawing.Size(354, 32);
			this.labelTool.TabIndex = 1;
			// 
			// buttonUpdate
			// 
			this.buttonUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonUpdate.Enabled = false;
			this.buttonUpdate.Location = new System.Drawing.Point(12, 377);
			this.buttonUpdate.Name = "buttonUpdate";
			this.buttonUpdate.Size = new System.Drawing.Size(200, 23);
			this.buttonUpdate.TabIndex = 5;
			this.buttonUpdate.UseVisualStyleBackColor = true;
			// 
			// Tools
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(584, 412);
			this.Controls.Add(this.buttonUpdate);
			this.Controls.Add(this.labelTool);
			this.Controls.Add(this.listViewTools);
			this.Controls.Add(this.buttonExit);
			this.Controls.Add(this.buttonLaunch);
			this.Controls.Add(this.textBoxDescription);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MinimumSize = new System.Drawing.Size(500, 300);
			this.Name = "Tools";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "JGR MSTS Editors & Tools";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBoxDescription;
		private System.Windows.Forms.Button buttonLaunch;
		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.ImageList imageListIcons;
		private System.Windows.Forms.ListView listViewTools;
		private System.Windows.Forms.Label labelTool;
		private System.Windows.Forms.Button buttonUpdate;
		private System.Windows.Forms.ToolTip toolTip;
	}
}

