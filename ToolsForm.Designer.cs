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
            this.components = new System.ComponentModel.Container();
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
            System.Windows.Forms.GroupBox groupBox4;
            System.Windows.Forms.Label label5;
            System.Windows.Forms.Label label11;
            this.upDownChainTurns = new System.Windows.Forms.NumericUpDown();
            this.textBoxUp = new System.Windows.Forms.TextBox();
            this.upDownLimit = new Rationals.Forms.PrimeUpDown();
            this.textBoxSubgroup = new System.Windows.Forms.TextBox();
            this.trackBarStickCommas = new System.Windows.Forms.TrackBar();
            this.textBoxStickCommas = new System.Windows.Forms.TextBox();
            this.comboBoxDistance = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBoxDistanceLimit = new System.Windows.Forms.TextBox();
            this.upDownCountLimit = new System.Windows.Forms.NumericUpDown();
            this.textBoxInfo = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuPreset = new System.Windows.Forms.ToolStripMenuItem();
            this.menuReset = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRecent = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSave = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuImage = new System.Windows.Forms.ToolStripMenuItem();
            this.menuImageShow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuImageSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.textBoxGrids = new System.Windows.Forms.TextBox();
            this.buttonApply = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.textBoxSelection = new System.Windows.Forms.TextBox();
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
            groupBox4 = new System.Windows.Forms.GroupBox();
            label5 = new System.Windows.Forms.Label();
            label11 = new System.Windows.Forms.Label();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownChainTurns)).BeginInit();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownLimit)).BeginInit();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarStickCommas)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownCountLimit)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(9, 114);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(49, 13);
            label1.TabIndex = 2;
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
            groupBox2.Location = new System.Drawing.Point(12, 222);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(174, 78);
            groupBox2.TabIndex = 5;
            groupBox2.TabStop = false;
            groupBox2.Text = "Chain slope";
            // 
            // upDownChainTurns
            // 
            this.upDownChainTurns.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownChainTurns.DecimalPlaces = 3;
            this.upDownChainTurns.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.upDownChainTurns.Location = new System.Drawing.Point(64, 45);
            this.upDownChainTurns.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.upDownChainTurns.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.upDownChainTurns.Name = "upDownChainTurns";
            this.upDownChainTurns.Size = new System.Drawing.Size(104, 20);
            this.upDownChainTurns.TabIndex = 3;
            this.upDownChainTurns.Tag = "Slope turns";
            this.upDownChainTurns.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownChainTurns.ValueChanged += new System.EventHandler(this.upDownChainTurns_ValueChanged);
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
            this.textBoxUp.Location = new System.Drawing.Point(64, 19);
            this.textBoxUp.Name = "textBoxUp";
            this.textBoxUp.Size = new System.Drawing.Size(104, 20);
            this.textBoxUp.TabIndex = 1;
            this.textBoxUp.Tag = "Slope origin";
            this.textBoxUp.TextChanged += new System.EventHandler(this.control_ValueChanged);
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(9, 420);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(44, 13);
            label4.TabIndex = 7;
            label4.Text = "ED Grid";
            // 
            // groupBox3
            // 
            groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            groupBox3.Controls.Add(this.upDownLimit);
            groupBox3.Controls.Add(label7);
            groupBox3.Controls.Add(this.textBoxSubgroup);
            groupBox3.Controls.Add(label6);
            groupBox3.Location = new System.Drawing.Point(12, 27);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(174, 78);
            groupBox3.TabIndex = 1;
            groupBox3.TabStop = false;
            groupBox3.Text = "Primes";
            // 
            // upDownLimit
            // 
            this.upDownLimit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownLimit.Location = new System.Drawing.Point(64, 19);
            this.upDownLimit.Name = "upDownLimit";
            this.upDownLimit.Size = new System.Drawing.Size(104, 20);
            this.upDownLimit.TabIndex = 1;
            this.upDownLimit.Tag = "Limit";
            this.upDownLimit.ValueChanged += new System.EventHandler(this.upDownLimit_ValueChanged);
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(6, 48);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(53, 13);
            label7.TabIndex = 2;
            label7.Text = "Subgroup";
            // 
            // textBoxSubgroup
            // 
            this.textBoxSubgroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSubgroup.Location = new System.Drawing.Point(64, 45);
            this.textBoxSubgroup.Name = "textBoxSubgroup";
            this.textBoxSubgroup.Size = new System.Drawing.Size(104, 20);
            this.textBoxSubgroup.TabIndex = 3;
            this.textBoxSubgroup.Tag = "Subgroup";
            this.textBoxSubgroup.TextChanged += new System.EventHandler(this.textBoxSubgroup_TextChanged);
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(6, 23);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(28, 13);
            label6.TabIndex = 0;
            label6.Text = "Limit";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(6, 21);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(35, 13);
            label8.TabIndex = 0;
            label8.Text = "Count";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(6, 48);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(49, 13);
            label9.TabIndex = 2;
            label9.Text = "Distance";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(9, 458);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(25, 13);
            label10.TabIndex = 10;
            label10.Text = "Info";
            // 
            // groupBox4
            // 
            groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            groupBox4.Controls.Add(this.trackBarStickCommas);
            groupBox4.Controls.Add(label5);
            groupBox4.Controls.Add(this.textBoxStickCommas);
            groupBox4.Location = new System.Drawing.Point(12, 306);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new System.Drawing.Size(174, 79);
            groupBox4.TabIndex = 6;
            groupBox4.TabStop = false;
            groupBox4.Text = "Stick commas";
            // 
            // trackBarStickCommas
            // 
            this.trackBarStickCommas.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBarStickCommas.AutoSize = false;
            this.trackBarStickCommas.LargeChange = 10;
            this.trackBarStickCommas.Location = new System.Drawing.Point(6, 45);
            this.trackBarStickCommas.Maximum = 100;
            this.trackBarStickCommas.Name = "trackBarStickCommas";
            this.trackBarStickCommas.Size = new System.Drawing.Size(163, 29);
            this.trackBarStickCommas.TabIndex = 2;
            this.trackBarStickCommas.TickFrequency = 10;
            this.trackBarStickCommas.ValueChanged += new System.EventHandler(this.trackBarStickCommas_ValueChanged);
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(6, 22);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(47, 13);
            label5.TabIndex = 0;
            label5.Text = "Commas";
            // 
            // textBoxStickCommas
            // 
            this.textBoxStickCommas.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxStickCommas.Location = new System.Drawing.Point(63, 19);
            this.textBoxStickCommas.Name = "textBoxStickCommas";
            this.textBoxStickCommas.Size = new System.Drawing.Size(105, 20);
            this.textBoxStickCommas.TabIndex = 1;
            this.textBoxStickCommas.Tag = "Commas";
            this.textBoxStickCommas.TextChanged += new System.EventHandler(this.textBoxStickCommas_TextChanged);
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(9, 394);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(51, 13);
            label11.TabIndex = 12;
            label11.Text = "Selection";
            // 
            // comboBoxDistance
            // 
            this.comboBoxDistance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxDistance.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDistance.FormattingEnabled = true;
            this.comboBoxDistance.Location = new System.Drawing.Point(67, 111);
            this.comboBoxDistance.Name = "comboBoxDistance";
            this.comboBoxDistance.Size = new System.Drawing.Size(119, 21);
            this.comboBoxDistance.TabIndex = 3;
            this.comboBoxDistance.Tag = "Distance";
            this.comboBoxDistance.TextChanged += new System.EventHandler(this.control_ValueChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(label9);
            this.groupBox1.Controls.Add(label8);
            this.groupBox1.Controls.Add(this.textBoxDistanceLimit);
            this.groupBox1.Controls.Add(this.upDownCountLimit);
            this.groupBox1.Location = new System.Drawing.Point(12, 138);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(174, 78);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Generation limits";
            // 
            // textBoxDistanceLimit
            // 
            this.textBoxDistanceLimit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDistanceLimit.Location = new System.Drawing.Point(64, 45);
            this.textBoxDistanceLimit.Name = "textBoxDistanceLimit";
            this.textBoxDistanceLimit.Size = new System.Drawing.Size(104, 20);
            this.textBoxDistanceLimit.TabIndex = 3;
            this.textBoxDistanceLimit.Tag = "Generation distance";
            this.textBoxDistanceLimit.TextChanged += new System.EventHandler(this.control_ValueChanged);
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
            this.upDownCountLimit.Location = new System.Drawing.Point(64, 19);
            this.upDownCountLimit.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.upDownCountLimit.Name = "upDownCountLimit";
            this.upDownCountLimit.Size = new System.Drawing.Size(104, 20);
            this.upDownCountLimit.TabIndex = 1;
            this.upDownCountLimit.Tag = "Generated item count";
            this.upDownCountLimit.ValueChanged += new System.EventHandler(this.control_ValueChanged);
            // 
            // textBoxInfo
            // 
            this.textBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInfo.Location = new System.Drawing.Point(12, 475);
            this.textBoxInfo.Multiline = true;
            this.textBoxInfo.Name = "textBoxInfo";
            this.textBoxInfo.Size = new System.Drawing.Size(174, 73);
            this.textBoxInfo.TabIndex = 11;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuPreset,
            this.menuImage,
            this.menuAbout});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(198, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuTools";
            // 
            // menuPreset
            // 
            this.menuPreset.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuReset,
            this.menuOpen,
            this.menuRecent,
            this.menuSave,
            this.menuSaveAs});
            this.menuPreset.Name = "menuPreset";
            this.menuPreset.Size = new System.Drawing.Size(51, 20);
            this.menuPreset.Text = "&Preset";
            // 
            // menuReset
            // 
            this.menuReset.Name = "menuReset";
            this.menuReset.Size = new System.Drawing.Size(123, 22);
            this.menuReset.Text = "&Reset";
            this.menuReset.Click += new System.EventHandler(this.menuReset_Click);
            // 
            // menuOpen
            // 
            this.menuOpen.Name = "menuOpen";
            this.menuOpen.Size = new System.Drawing.Size(123, 22);
            this.menuOpen.Text = "&Open...";
            this.menuOpen.Click += new System.EventHandler(this.menuOpen_Click);
            // 
            // menuRecent
            // 
            this.menuRecent.Name = "menuRecent";
            this.menuRecent.Size = new System.Drawing.Size(123, 22);
            this.menuRecent.Text = "Recent";
            // 
            // menuSave
            // 
            this.menuSave.Enabled = false;
            this.menuSave.Name = "menuSave";
            this.menuSave.Size = new System.Drawing.Size(123, 22);
            this.menuSave.Text = "&Save";
            this.menuSave.Click += new System.EventHandler(this.menuSave_Click);
            // 
            // menuSaveAs
            // 
            this.menuSaveAs.Name = "menuSaveAs";
            this.menuSaveAs.Size = new System.Drawing.Size(123, 22);
            this.menuSaveAs.Text = "Save &As...";
            this.menuSaveAs.Click += new System.EventHandler(this.menuSaveAs_Click);
            // 
            // menuImage
            // 
            this.menuImage.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuImageShow,
            this.menuImageSaveAs});
            this.menuImage.Name = "menuImage";
            this.menuImage.Size = new System.Drawing.Size(52, 20);
            this.menuImage.Text = "&Image";
            // 
            // menuImageShow
            // 
            this.menuImageShow.Name = "menuImageShow";
            this.menuImageShow.Size = new System.Drawing.Size(139, 22);
            this.menuImageShow.Text = "Open as Svg";
            this.menuImageShow.Click += new System.EventHandler(this.menuImageShow_Click);
            // 
            // menuImageSaveAs
            // 
            this.menuImageSaveAs.Name = "menuImageSaveAs";
            this.menuImageSaveAs.Size = new System.Drawing.Size(139, 22);
            this.menuImageSaveAs.Text = "Save As...";
            this.menuImageSaveAs.Click += new System.EventHandler(this.menuImageSaveAs_Click);
            // 
            // menuAbout
            // 
            this.menuAbout.Name = "menuAbout";
            this.menuAbout.Size = new System.Drawing.Size(52, 20);
            this.menuAbout.Text = "&About";
            // 
            // textBoxGrids
            // 
            this.textBoxGrids.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxGrids.Location = new System.Drawing.Point(67, 417);
            this.textBoxGrids.Name = "textBoxGrids";
            this.textBoxGrids.Size = new System.Drawing.Size(119, 20);
            this.textBoxGrids.TabIndex = 8;
            this.textBoxGrids.Tag = "ED grid";
            this.textBoxGrids.TextChanged += new System.EventHandler(this.textBoxGrids_TextChanged);
            // 
            // buttonApply
            // 
            this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonApply.Location = new System.Drawing.Point(111, 443);
            this.buttonApply.Name = "buttonApply";
            this.buttonApply.Size = new System.Drawing.Size(75, 23);
            this.buttonApply.TabIndex = 9;
            this.buttonApply.Text = "&Apply";
            this.buttonApply.UseVisualStyleBackColor = true;
            this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 100;
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 100;
            this.toolTip.ReshowDelay = 20;
            this.toolTip.ShowAlways = true;
            // 
            // textBoxSelection
            // 
            this.textBoxSelection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSelection.Location = new System.Drawing.Point(67, 391);
            this.textBoxSelection.Name = "textBoxSelection";
            this.textBoxSelection.Size = new System.Drawing.Size(119, 20);
            this.textBoxSelection.TabIndex = 13;
            this.textBoxSelection.Tag = "ED grid";
            this.textBoxSelection.TextChanged += new System.EventHandler(this.textBoxHighlight_TextChanged);
            // 
            // ToolsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(198, 560);
            this.Controls.Add(this.textBoxSelection);
            this.Controls.Add(label11);
            this.Controls.Add(groupBox4);
            this.Controls.Add(label10);
            this.Controls.Add(groupBox3);
            this.Controls.Add(this.buttonApply);
            this.Controls.Add(this.textBoxGrids);
            this.Controls.Add(label4);
            this.Controls.Add(groupBox2);
            this.Controls.Add(this.textBoxInfo);
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
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarStickCommas)).EndInit();
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
        private System.Windows.Forms.TextBox textBoxInfo;
        private System.Windows.Forms.TextBox textBoxUp;
        private System.Windows.Forms.NumericUpDown upDownChainTurns;
        private System.Windows.Forms.TextBox textBoxDistanceLimit;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuPreset;
        private System.Windows.Forms.ToolStripMenuItem menuReset;
        private System.Windows.Forms.ToolStripMenuItem menuSaveAs;
        private System.Windows.Forms.ToolStripMenuItem menuSave;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private System.Windows.Forms.ToolStripMenuItem menuOpen;
        private System.Windows.Forms.TextBox textBoxGrids;
        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.TextBox textBoxSubgroup;
        private PrimeUpDown upDownLimit;
        private System.Windows.Forms.TrackBar trackBarStickCommas;
        private System.Windows.Forms.TextBox textBoxStickCommas;
        private System.Windows.Forms.ToolStripMenuItem menuRecent;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStripMenuItem menuImage;
        private System.Windows.Forms.ToolStripMenuItem menuImageShow;
        private System.Windows.Forms.ToolStripMenuItem menuImageSaveAs;
        private System.Windows.Forms.TextBox textBoxSelection;
    }
}