namespace ClimateAnalysis {
	partial class ForcingFile {
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxVICstart = new System.Windows.Forms.TextBox();
            this.lblVICstart = new System.Windows.Forms.Label();
            this.btnAdjust = new System.Windows.Forms.Button();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkPisces = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.comboBoxFormat = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Forcing File to Adjust";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(15, 26);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(386, 20);
            this.textBox1.TabIndex = 1;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "All Files|*.*";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(407, 25);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Browse...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 94);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Format";
            // 
            // textBoxVICstart
            // 
            this.textBoxVICstart.Location = new System.Drawing.Point(168, 110);
            this.textBoxVICstart.Name = "textBoxVICstart";
            this.textBoxVICstart.Size = new System.Drawing.Size(77, 20);
            this.textBoxVICstart.TabIndex = 6;
            this.textBoxVICstart.Text = "01/01/1915";
            // 
            // lblVICstart
            // 
            this.lblVICstart.AutoSize = true;
            this.lblVICstart.Location = new System.Drawing.Point(165, 94);
            this.lblVICstart.Name = "lblVICstart";
            this.lblVICstart.Size = new System.Drawing.Size(159, 13);
            this.lblVICstart.TabIndex = 7;
            this.lblVICstart.Text = "VIC Start Date (MM/DD/YYYY):";
            // 
            // btnAdjust
            // 
            this.btnAdjust.Location = new System.Drawing.Point(168, 199);
            this.btnAdjust.Name = "btnAdjust";
            this.btnAdjust.Size = new System.Drawing.Size(75, 23);
            this.btnAdjust.TabIndex = 8;
            this.btnAdjust.Text = "Adjust";
            this.btnAdjust.UseVisualStyleBackColor = true;
            this.btnAdjust.Click += new System.EventHandler(this.btnAdjust_Click);
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(15, 173);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(386, 20);
            this.textBox3.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 157);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Output Folder";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(407, 172);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 11;
            this.button3.Text = "Browse...";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(249, 199);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // chkPisces
            // 
            this.chkPisces.AutoSize = true;
            this.chkPisces.Location = new System.Drawing.Point(15, 203);
            this.chkPisces.Name = "chkPisces";
            this.chkPisces.Size = new System.Drawing.Size(140, 17);
            this.chkPisces.TabIndex = 13;
            this.chkPisces.Text = "Create Pisces Database";
            this.chkPisces.UseVisualStyleBackColor = true;
            this.chkPisces.Visible = false;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(15, 53);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(153, 17);
            this.checkBox2.TabIndex = 14;
            this.checkBox2.Text = "Adjust all files in the folder. ";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // comboBoxFormat
            // 
            this.comboBoxFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFormat.FormattingEnabled = true;
            this.comboBoxFormat.Location = new System.Drawing.Point(15, 110);
            this.comboBoxFormat.Name = "comboBoxFormat";
            this.comboBoxFormat.Size = new System.Drawing.Size(121, 21);
            this.comboBoxFormat.TabIndex = 15;
            this.comboBoxFormat.SelectionChangeCommitted += new System.EventHandler(this.comboBoxFormat_SelectionChangeCommitted);
            // 
            // ForcingFile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(493, 227);
            this.Controls.Add(this.comboBoxFormat);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.chkPisces);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.btnAdjust);
            this.Controls.Add(this.lblVICstart);
            this.Controls.Add(this.textBoxVICstart);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Name = "ForcingFile";
            this.Text = "Adjust Forcing File";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxVICstart;
		private System.Windows.Forms.Label lblVICstart;
		private System.Windows.Forms.Button btnAdjust;
		private System.Windows.Forms.TextBox textBox3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.CheckBox chkPisces;
		private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.ComboBox comboBoxFormat;
	}
}