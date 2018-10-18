// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

namespace Framefield.Player
{
    partial class StartUpDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StartUpDialog));
            this.StartBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.FullScreenCheckBox = new System.Windows.Forms.CheckBox();
            this.LoopedCheckBox = new System.Windows.Forms.CheckBox();
            this.DisplayModesView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.AspectRatioView = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label4 = new System.Windows.Forms.Label();
            this.SamplingView = new System.Windows.Forms.ListView();
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.VSyncCheckBox = new System.Windows.Forms.CheckBox();
            this.PreCacheCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // StartBtn
            // 
            this.StartBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.StartBtn.Location = new System.Drawing.Point(306, 134);
            this.StartBtn.Name = "StartBtn";
            this.StartBtn.Size = new System.Drawing.Size(75, 23);
            this.StartBtn.TabIndex = 0;
            this.StartBtn.Text = "Start";
            this.StartBtn.UseVisualStyleBackColor = true;
            this.StartBtn.Click += new System.EventHandler(this.StartBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(306, 163);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelBtn.TabIndex = 1;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(414, 25);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.WaitOnLoad = true;
            // 
            // FullScreenCheckBox
            // 
            this.FullScreenCheckBox.AutoSize = true;
            this.FullScreenCheckBox.Location = new System.Drawing.Point(306, 52);
            this.FullScreenCheckBox.Name = "FullScreenCheckBox";
            this.FullScreenCheckBox.Size = new System.Drawing.Size(74, 17);
            this.FullScreenCheckBox.TabIndex = 6;
            this.FullScreenCheckBox.Text = "Fullscreen";
            this.FullScreenCheckBox.UseVisualStyleBackColor = true;
            this.FullScreenCheckBox.CheckedChanged += new System.EventHandler(this.FullScreenCheckBox_CheckedChanged);
            // 
            // LoopedCheckBox
            // 
            this.LoopedCheckBox.AutoSize = true;
            this.LoopedCheckBox.Location = new System.Drawing.Point(306, 72);
            this.LoopedCheckBox.Name = "LoopedCheckBox";
            this.LoopedCheckBox.Size = new System.Drawing.Size(62, 17);
            this.LoopedCheckBox.TabIndex = 7;
            this.LoopedCheckBox.Text = "Looped";
            this.LoopedCheckBox.UseVisualStyleBackColor = true;
            this.LoopedCheckBox.CheckedChanged += new System.EventHandler(this.LoopedCheckBox_CheckedChanged);
            // 
            // DisplayModesView
            // 
            this.DisplayModesView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.DisplayModesView.FullRowSelect = true;
            this.DisplayModesView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.DisplayModesView.HideSelection = false;
            this.DisplayModesView.Location = new System.Drawing.Point(12, 52);
            this.DisplayModesView.MultiSelect = false;
            this.DisplayModesView.Name = "DisplayModesView";
            this.DisplayModesView.Size = new System.Drawing.Size(146, 134);
            this.DisplayModesView.TabIndex = 2;
            this.DisplayModesView.UseCompatibleStateImageBehavior = false;
            this.DisplayModesView.View = System.Windows.Forms.View.Details;
            this.DisplayModesView.SelectedIndexChanged += new System.EventHandler(this.ResolutionsView_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 103;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Resolution";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(166, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "AspectRatio";
            // 
            // AspectRatioView
            // 
            this.AspectRatioView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
            this.AspectRatioView.FullRowSelect = true;
            this.AspectRatioView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.AspectRatioView.HideSelection = false;
            this.AspectRatioView.Location = new System.Drawing.Point(166, 52);
            this.AspectRatioView.MultiSelect = false;
            this.AspectRatioView.Name = "AspectRatioView";
            this.AspectRatioView.Scrollable = false;
            this.AspectRatioView.Size = new System.Drawing.Size(65, 134);
            this.AspectRatioView.TabIndex = 3;
            this.AspectRatioView.UseCompatibleStateImageBehavior = false;
            this.AspectRatioView.View = System.Windows.Forms.View.Details;
            this.AspectRatioView.SelectedIndexChanged += new System.EventHandler(this.AspectRatioView_SelectedIndexChanged);
            // 
            // columnHeader2
            // 
            this.columnHeader2.Width = 103;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(238, 35);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Sampling";
            // 
            // SamplingView
            // 
            this.SamplingView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader4});
            this.SamplingView.FullRowSelect = true;
            this.SamplingView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.SamplingView.HideSelection = false;
            this.SamplingView.Location = new System.Drawing.Point(238, 52);
            this.SamplingView.MultiSelect = false;
            this.SamplingView.Name = "SamplingView";
            this.SamplingView.Scrollable = false;
            this.SamplingView.Size = new System.Drawing.Size(50, 134);
            this.SamplingView.TabIndex = 5;
            this.SamplingView.UseCompatibleStateImageBehavior = false;
            this.SamplingView.View = System.Windows.Forms.View.Details;
            this.SamplingView.SelectedIndexChanged += new System.EventHandler(this.SamplingView_SelectedIndexChanged);
            // 
            // columnHeader4
            // 
            this.columnHeader4.Width = 103;
            // 
            // VSyncCheckBox
            // 
            this.VSyncCheckBox.AutoSize = true;
            this.VSyncCheckBox.Location = new System.Drawing.Point(306, 92);
            this.VSyncCheckBox.Name = "VSyncCheckBox";
            this.VSyncCheckBox.Size = new System.Drawing.Size(57, 17);
            this.VSyncCheckBox.TabIndex = 13;
            this.VSyncCheckBox.Text = "VSync";
            this.VSyncCheckBox.UseVisualStyleBackColor = true;
            this.VSyncCheckBox.CheckedChanged += new System.EventHandler(this.VSyncCheckBox_CheckedChanged);
            // 
            // PreCacheCheckBox
            // 
            this.PreCacheCheckBox.AutoSize = true;
            this.PreCacheCheckBox.Location = new System.Drawing.Point(306, 112);
            this.PreCacheCheckBox.Name = "PreCacheCheckBox";
            this.PreCacheCheckBox.Size = new System.Drawing.Size(72, 17);
            this.PreCacheCheckBox.TabIndex = 14;
            this.PreCacheCheckBox.Text = "Precache";
            this.PreCacheCheckBox.UseVisualStyleBackColor = true;
            this.PreCacheCheckBox.CheckedChanged += new System.EventHandler(this.PreCacheCheckBox_CheckedChanged);
            // 
            // StartUpDialog
            // 
            this.AcceptButton = this.StartBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(393, 197);
            this.ControlBox = false;
            this.Controls.Add(this.PreCacheCheckBox);
            this.Controls.Add(this.VSyncCheckBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.SamplingView);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.AspectRatioView);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DisplayModesView);
            this.Controls.Add(this.LoopedCheckBox);
            this.Controls.Add(this.FullScreenCheckBox);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.StartBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartUpDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Framefield T2 Player";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.StartUpDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button StartBtn;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.CheckBox FullScreenCheckBox;
        private System.Windows.Forms.CheckBox LoopedCheckBox;
        private System.Windows.Forms.ListView DisplayModesView;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListView AspectRatioView;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListView SamplingView;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.CheckBox VSyncCheckBox;
        private System.Windows.Forms.CheckBox PreCacheCheckBox;
    }
}