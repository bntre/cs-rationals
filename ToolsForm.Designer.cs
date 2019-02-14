namespace Rationals.Forms {
    partial class ToolsForm {
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
            System.Windows.Forms.GroupBox groupBox2;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.GroupBox groupBox3;
            System.Windows.Forms.Label label7;
            System.Windows.Forms.Label label6;
            System.Windows.Forms.Label label8;
            System.Windows.Forms.Label label9;
            System.Windows.Forms.Label label10;
            this.upDownChainTurns = new System.Windows.Forms.NumericUpDown();
            this.textBoxUp = new System.Windows.Forms.TextBox();
            this.upDownLimit = new Rationals.Forms.PrimeUpDown();
            this.textBoxSubgroup = new System.Windows.Forms.TextBox();
            this.comboBoxDistance = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBoxDistanceLimit = new System.Windows.Forms.TextBox();
            this.upDownCountLimit = new System.Windows.Forms.NumericUpDown();
            this.textBoxSelection = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.presetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textBoxGrids = new System.Windows.Forms.TextBox();
            this.buttonApply = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            groupBox2 = new System.Windows.Forms.GroupBox();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            groupBox3 = new System.Windows.Forms.GroupBox();
            label7 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownChainTurns)).BeginInit();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownLimit)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownCountLimit)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 196);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(49, 13);
            label1.TabIndex = 0;
            label1.Text = "Distance";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(6, 22);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(34, 13);
            label2.TabIndex = 0;
            label2.Text = "Origin";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            groupBox2.Controls.Add(this.upDownChainTurns);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(this.textBoxUp);
            groupBox2.Controls.Add(label2);
            groupBox2.Location = new System.Drawing.Point(11, 112);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(159, 75);
            groupBox2.TabIndex = 4;
            groupBox2.TabStop = false;
            groupBox2.Text = "Chain slope";
            // 
            // upDownChainTurns
            // 
            this.upDownChainTurns.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownChainTurns.DecimalPlaces = 2;
            this.upDownChainTurns.Location = new System.Drawing.Point(77, 45);
            this.upDownChainTurns.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownChainTurns.Name = "upDownChainTurns";
            this.upDownChainTurns.Size = new System.Drawing.Size(76, 20);
            this.upDownChainTurns.TabIndex = 3;
            this.upDownChainTurns.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(6, 47);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(34, 13);
            label3.TabIndex = 2;
            label3.Text = "Turns";
            // 
            // textBoxUp
            // 
            this.textBoxUp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxUp.Location = new System.Drawing.Point(77, 19);
            this.textBoxUp.Name = "textBoxUp";
            this.textBoxUp.Size = new System.Drawing.Size(76, 20);
            this.textBoxUp.TabIndex = 1;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(12, 305);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(26, 13);
            label4.TabIndex = 6;
            label4.Text = "Grid";
            // 
            // groupBox3
            // 
            groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            groupBox3.Controls.Add(this.upDownLimit);
            groupBox3.Controls.Add(label7);
            groupBox3.Controls.Add(this.textBoxSubgroup);
            groupBox3.Controls.Add(label6);
            groupBox3.Location = new System.Drawing.Point(11, 27);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(159, 79);
            groupBox3.TabIndex = 9;
            groupBox3.TabStop = false;
            groupBox3.Text = "Primes";
            // 
            // upDownLimit
            // 
            this.upDownLimit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownLimit.Location = new System.Drawing.Point(77, 19);
            this.upDownLimit.Name = "upDownLimit";
            this.upDownLimit.Size = new System.Drawing.Size(76, 20);
            this.upDownLimit.TabIndex = 12;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(6, 48);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(65, 13);
            label7.TabIndex = 11;
            label7.Text = "or Subgroup";
            // 
            // textBoxSubgroup
            // 
            this.textBoxSubgroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSubgroup.Location = new System.Drawing.Point(77, 45);
            this.textBoxSubgroup.Name = "textBoxSubgroup";
            this.textBoxSubgroup.Size = new System.Drawing.Size(76, 20);
            this.textBoxSubgroup.TabIndex = 10;
            this.textBoxSubgroup.TextChanged += new System.EventHandler(this.textBoxSubgroup_TextChanged);
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(6, 23);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(28, 13);
            label6.TabIndex = 9;
            label6.Text = "Limit";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(6, 21);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(35, 13);
            label8.TabIndex = 8;
            label8.Text = "Count";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(6, 48);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(49, 13);
            label9.TabIndex = 9;
            label9.Text = "Distance";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(12, 353);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(51, 13);
            label10.TabIndex = 10;
            label10.Text = "Selection";
            // 
            // comboBoxDistance
            // 
            this.comboBoxDistance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxDistance.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDistance.FormattingEnabled = true;
            this.comboBoxDistance.Location = new System.Drawing.Point(67, 193);
            this.comboBoxDistance.Name = "comboBoxDistance";
            this.comboBoxDistance.Size = new System.Drawing.Size(103, 21);
            this.comboBoxDistance.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(label9);
            this.groupBox1.Controls.Add(label8);
            this.groupBox1.Controls.Add(this.textBoxDistanceLimit);
            this.groupBox1.Controls.Add(this.upDownCountLimit);
            this.groupBox1.Location = new System.Drawing.Point(11, 220);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(159, 76);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Draw limits";
            // 
            // textBoxDistanceLimit
            // 
            this.textBoxDistanceLimit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDistanceLimit.Location = new System.Drawing.Point(77, 45);
            this.textBoxDistanceLimit.Name = "textBoxDistanceLimit";
            this.textBoxDistanceLimit.Size = new System.Drawing.Size(76, 20);
            this.textBoxDistanceLimit.TabIndex = 7;
            // 
            // upDownCountLimit
            // 
            this.upDownCountLimit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownCountLimit.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.upDownCountLimit.Location = new System.Drawing.Point(77, 19);
            this.upDownCountLimit.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.upDownCountLimit.Name = "upDownCountLimit";
            this.upDownCountLimit.Size = new System.Drawing.Size(76, 20);
            this.upDownCountLimit.TabIndex = 6;
            // 
            // textBoxSelection
            // 
            this.textBoxSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSelection.Location = new System.Drawing.Point(11, 370);
            this.textBoxSelection.Multiline = true;
            this.textBoxSelection.Name = "textBoxSelection";
            this.textBoxSelection.Size = new System.Drawing.Size(159, 74);
            this.textBoxSelection.TabIndex = 3;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.presetToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(182, 24);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // presetToolStripMenuItem
            // 
            this.presetToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.deleteToolStripMenuItem});
            this.presetToolStripMenuItem.Name = "presetToolStripMenuItem";
            this.presetToolStripMenuItem.Size = new System.Drawing.Size(51, 20);
            this.presetToolStripMenuItem.Text = "&Preset";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.newToolStripMenuItem.Text = "Reset";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.openToolStripMenuItem.Text = "Open";
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.saveToolStripMenuItem.Text = "Save";
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // textBoxGrids
            // 
            this.textBoxGrids.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxGrids.Location = new System.Drawing.Point(67, 302);
            this.textBoxGrids.Name = "textBoxGrids";
            this.textBoxGrids.Size = new System.Drawing.Size(103, 20);
            this.textBoxGrids.TabIndex = 7;
            this.textBoxGrids.TextChanged += new System.EventHandler(this.textBoxGrids_TextChanged);
            // 
            // buttonApply
            // 
            this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonApply.Location = new System.Drawing.Point(95, 334);
            this.buttonApply.Name = "buttonApply";
            this.buttonApply.Size = new System.Drawing.Size(75, 23);
            this.buttonApply.TabIndex = 8;
            this.buttonApply.Text = "&Apply";
            this.buttonApply.UseVisualStyleBackColor = true;
            this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
            // 
            // ToolsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(182, 456);
            this.Controls.Add(label10);
            this.Controls.Add(groupBox3);
            this.Controls.Add(this.buttonApply);
            this.Controls.Add(this.textBoxGrids);
            this.Controls.Add(label4);
            this.Controls.Add(groupBox2);
            this.Controls.Add(this.textBoxSelection);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.comboBoxDistance);
            this.Controls.Add(label1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ToolsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Tools";
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownChainTurns)).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownLimit)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownCountLimit)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ComboBox comboBoxDistance;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.NumericUpDown upDownCountLimit;
        private System.Windows.Forms.TextBox textBoxSelection;
        private System.Windows.Forms.TextBox textBoxUp;
        private System.Windows.Forms.NumericUpDown upDownChainTurns;
        private System.Windows.Forms.TextBox textBoxDistanceLimit;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem presetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.TextBox textBoxGrids;
        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.TextBox textBoxSubgroup;
        private PrimeUpDown upDownLimit;
    }
}