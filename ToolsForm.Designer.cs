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
            System.Windows.Forms.Label label10;
            System.Windows.Forms.GroupBox groupBox4;
            System.Windows.Forms.Label label11;
            System.Windows.Forms.Label label5;
            System.Windows.Forms.GroupBox groupBox5;
            System.Windows.Forms.Label label9;
            this.upDownChainTurns = new Rationals.Forms.ScrollableUpDown();
            this.textBoxUp = new System.Windows.Forms.TextBox();
            this.upDownLimit = new Rationals.Forms.PrimeUpDown();
            this.textBoxSubgroup = new System.Windows.Forms.TextBox();
            this.gridTemperament = new Rationals.Forms.GridView.TypedGridView();
            this.ColumnRational = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnCents = new Rationals.Forms.GridView.NumericColumn();
            this.sliderTemperament = new System.Windows.Forms.TrackBar();
            this.upDownStepSizeCountLimit = new Rationals.Forms.CustomUpDown();
            this.upDownMinimalStep = new Rationals.Forms.ScrollableUpDown();
            this.comboBoxDistance = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.upDownCountLimit = new Rationals.Forms.ScrollableUpDown();
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
            label10 = new System.Windows.Forms.Label();
            groupBox4 = new System.Windows.Forms.GroupBox();
            label11 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            groupBox5 = new System.Windows.Forms.GroupBox();
            label9 = new System.Windows.Forms.Label();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownChainTurns)).BeginInit();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownLimit)).BeginInit();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridTemperament)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderTemperament)).BeginInit();
            groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownStepSizeCountLimit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownMinimalStep)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownCountLimit)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 22);
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
            groupBox2.Location = new System.Drawing.Point(12, 328);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(174, 78);
            groupBox2.TabIndex = 4;
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
            this.upDownChainTurns.ScrollStep = new decimal(new int[] {
            1,
            0,
            0,
            196608});
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
            this.textBoxUp.TextChanged += new System.EventHandler(this.textBoxUp_TextChanged);
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(9, 497);
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
            label8.Location = new System.Drawing.Point(6, 48);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(35, 13);
            label8.TabIndex = 2;
            label8.Text = "Count";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(9, 548);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(25, 13);
            label10.TabIndex = 11;
            label10.Text = "Info";
            // 
            // groupBox4
            // 
            groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            groupBox4.Controls.Add(this.gridTemperament);
            groupBox4.Controls.Add(this.sliderTemperament);
            groupBox4.Location = new System.Drawing.Point(12, 195);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new System.Drawing.Size(174, 127);
            groupBox4.TabIndex = 3;
            groupBox4.TabStop = false;
            groupBox4.Text = "Temperament";
            // 
            // gridTemperament
            // 
            this.gridTemperament.AllowDrop = true;
            this.gridTemperament.AllowUserToResizeRows = false;
            this.gridTemperament.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridTemperament.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridTemperament.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridTemperament.ColumnHeadersVisible = false;
            this.gridTemperament.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnRational,
            this.ColumnCents});
            this.gridTemperament.Location = new System.Drawing.Point(5, 19);
            this.gridTemperament.MultiSelect = false;
            this.gridTemperament.Name = "gridTemperament";
            this.gridTemperament.RowHeadersVisible = false;
            this.gridTemperament.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridTemperament.Size = new System.Drawing.Size(163, 67);
            this.gridTemperament.TabIndex = 0;
            this.gridTemperament.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridTemperament_CellEndEdit);
            this.gridTemperament.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridTemperation_CellValueChanged);
            this.gridTemperament.UserAddedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.gridTemperament_UserAddedRow);
            this.gridTemperament.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.gridTemperament_UserDeletedRow);
            this.gridTemperament.DragDrop += new System.Windows.Forms.DragEventHandler(this.gridTemperament_DragDrop);
            // 
            // ColumnRational
            // 
            this.ColumnRational.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnRational.FillWeight = 50F;
            this.ColumnRational.HeaderText = "Rational";
            this.ColumnRational.Name = "ColumnRational";
            // 
            // ColumnCents
            // 
            this.ColumnCents.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnCents.CursorScrollStep = 0.1F;
            this.ColumnCents.FillWeight = 50F;
            this.ColumnCents.HeaderText = "Cents";
            this.ColumnCents.Name = "ColumnCents";
            this.ColumnCents.ScrollRoundDigits = 2;
            this.ColumnCents.WheelScrollStep = 0.5F;
            // 
            // sliderTemperament
            // 
            this.sliderTemperament.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sliderTemperament.AutoSize = false;
            this.sliderTemperament.LargeChange = 10;
            this.sliderTemperament.Location = new System.Drawing.Point(5, 92);
            this.sliderTemperament.Maximum = 100;
            this.sliderTemperament.Name = "sliderTemperament";
            this.sliderTemperament.Size = new System.Drawing.Size(163, 29);
            this.sliderTemperament.TabIndex = 1;
            this.sliderTemperament.TickFrequency = 10;
            this.sliderTemperament.ValueChanged += new System.EventHandler(this.sliderTemperament_ValueChanged);
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(9, 523);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(51, 13);
            label11.TabIndex = 9;
            label11.Text = "Selection";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(6, 21);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(47, 13);
            label5.TabIndex = 5;
            label5.Text = "Min step";
            // 
            // groupBox5
            // 
            groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            groupBox5.Controls.Add(label9);
            groupBox5.Controls.Add(this.upDownStepSizeCountLimit);
            groupBox5.Controls.Add(label5);
            groupBox5.Controls.Add(this.upDownMinimalStep);
            groupBox5.Location = new System.Drawing.Point(12, 412);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new System.Drawing.Size(174, 76);
            groupBox5.TabIndex = 13;
            groupBox5.TabStop = false;
            groupBox5.Text = "Degrees";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(6, 47);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(57, 13);
            label9.TabIndex = 7;
            label9.Text = "Size count";
            // 
            // upDownStepSizeCountLimit
            // 
            this.upDownStepSizeCountLimit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownStepSizeCountLimit.Location = new System.Drawing.Point(64, 45);
            this.upDownStepSizeCountLimit.Name = "upDownStepSizeCountLimit";
            this.upDownStepSizeCountLimit.Size = new System.Drawing.Size(104, 20);
            this.upDownStepSizeCountLimit.TabIndex = 8;
            this.upDownStepSizeCountLimit.Tag = "Step size count";
            this.upDownStepSizeCountLimit.ValueChanged += new System.EventHandler(this.upDownStepSizeCountLimit_ValueChanged);
            // 
            // upDownMinimalStep
            // 
            this.upDownMinimalStep.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownMinimalStep.DecimalPlaces = 2;
            this.upDownMinimalStep.Location = new System.Drawing.Point(64, 19);
            this.upDownMinimalStep.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.upDownMinimalStep.Name = "upDownMinimalStep";
            this.upDownMinimalStep.ScrollStep = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.upDownMinimalStep.Size = new System.Drawing.Size(104, 20);
            this.upDownMinimalStep.TabIndex = 6;
            this.upDownMinimalStep.Tag = "Minimal step";
            this.upDownMinimalStep.ValueChanged += new System.EventHandler(this.upDownMinimalStep_ValueChanged);
            // 
            // comboBoxDistance
            // 
            this.comboBoxDistance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxDistance.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDistance.FormattingEnabled = true;
            this.comboBoxDistance.Location = new System.Drawing.Point(64, 19);
            this.comboBoxDistance.Name = "comboBoxDistance";
            this.comboBoxDistance.Size = new System.Drawing.Size(104, 21);
            this.comboBoxDistance.TabIndex = 1;
            this.comboBoxDistance.Tag = "Distance";
            this.comboBoxDistance.TextChanged += new System.EventHandler(this.comboBoxDistance_TextChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(label8);
            this.groupBox1.Controls.Add(this.upDownCountLimit);
            this.groupBox1.Controls.Add(label1);
            this.groupBox1.Controls.Add(this.comboBoxDistance);
            this.groupBox1.Location = new System.Drawing.Point(12, 111);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(174, 78);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Generate";
            // 
            // upDownCountLimit
            // 
            this.upDownCountLimit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownCountLimit.Location = new System.Drawing.Point(64, 46);
            this.upDownCountLimit.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.upDownCountLimit.Name = "upDownCountLimit";
            this.upDownCountLimit.ScrollStep = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownCountLimit.Size = new System.Drawing.Size(104, 20);
            this.upDownCountLimit.TabIndex = 3;
            this.upDownCountLimit.Tag = "Generated item count";
            this.upDownCountLimit.ValueChanged += new System.EventHandler(this.upDownCountLimit_ValueChanged);
            // 
            // textBoxInfo
            // 
            this.textBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInfo.Location = new System.Drawing.Point(12, 564);
            this.textBoxInfo.Multiline = true;
            this.textBoxInfo.Name = "textBoxInfo";
            this.textBoxInfo.Size = new System.Drawing.Size(174, 78);
            this.textBoxInfo.TabIndex = 12;
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
            this.textBoxGrids.Location = new System.Drawing.Point(67, 494);
            this.textBoxGrids.Name = "textBoxGrids";
            this.textBoxGrids.Size = new System.Drawing.Size(119, 20);
            this.textBoxGrids.TabIndex = 8;
            this.textBoxGrids.Tag = "ED grid";
            this.textBoxGrids.TextChanged += new System.EventHandler(this.textBoxGrids_TextChanged);
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
            this.textBoxSelection.Location = new System.Drawing.Point(67, 520);
            this.textBoxSelection.Name = "textBoxSelection";
            this.textBoxSelection.Size = new System.Drawing.Size(119, 20);
            this.textBoxSelection.TabIndex = 10;
            this.textBoxSelection.Tag = "ED grid";
            this.textBoxSelection.TextChanged += new System.EventHandler(this.textBoxSelection_TextChanged);
            // 
            // ToolsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(198, 654);
            this.Controls.Add(groupBox5);
            this.Controls.Add(this.textBoxSelection);
            this.Controls.Add(label11);
            this.Controls.Add(groupBox4);
            this.Controls.Add(label10);
            this.Controls.Add(groupBox3);
            this.Controls.Add(this.textBoxGrids);
            this.Controls.Add(label4);
            this.Controls.Add(groupBox2);
            this.Controls.Add(this.textBoxInfo);
            this.Controls.Add(this.groupBox1);
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
            ((System.ComponentModel.ISupportInitialize)(this.gridTemperament)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sliderTemperament)).EndInit();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownStepSizeCountLimit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownMinimalStep)).EndInit();
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
        private Rationals.Forms.ScrollableUpDown upDownCountLimit;
        private System.Windows.Forms.TextBox textBoxInfo;
        private System.Windows.Forms.TextBox textBoxUp;
        private Rationals.Forms.ScrollableUpDown upDownChainTurns;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuPreset;
        private System.Windows.Forms.ToolStripMenuItem menuReset;
        private System.Windows.Forms.ToolStripMenuItem menuSaveAs;
        private System.Windows.Forms.ToolStripMenuItem menuSave;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private System.Windows.Forms.ToolStripMenuItem menuOpen;
        private System.Windows.Forms.TextBox textBoxGrids;
        private System.Windows.Forms.TextBox textBoxSubgroup;
        private Rationals.Forms.PrimeUpDown upDownLimit;
        private System.Windows.Forms.TrackBar sliderTemperament;
        private System.Windows.Forms.ToolStripMenuItem menuRecent;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStripMenuItem menuImage;
        private System.Windows.Forms.ToolStripMenuItem menuImageShow;
        private System.Windows.Forms.ToolStripMenuItem menuImageSaveAs;
        private System.Windows.Forms.TextBox textBoxSelection;
        private Rationals.Forms.GridView.TypedGridView gridTemperament;
        private Rationals.Forms.ScrollableUpDown upDownMinimalStep;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnRational;
        private GridView.NumericColumn ColumnCents;
        private Rationals.Forms.CustomUpDown upDownStepSizeCountLimit;
    }
}