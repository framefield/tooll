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
            this.SuspendLayout();
            // 
            // StartBtn
            // 
            this.StartBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.StartBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(57)))), ((int)(((byte)(57)))));
            this.StartBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.StartBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.StartBtn.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StartBtn.Location = new System.Drawing.Point(374, 126);
            this.StartBtn.Name = "StartBtn";
            this.StartBtn.Size = new System.Drawing.Size(87, 27);
            this.StartBtn.TabIndex = 0;
            this.StartBtn.Text = "Start";
            this.StartBtn.UseVisualStyleBackColor = false;
            this.StartBtn.Click += new System.EventHandler(this.StartBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(57)))), ((int)(((byte)(57)))));
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CancelBtn.Location = new System.Drawing.Point(374, 159);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(87, 27);
            this.CancelBtn.TabIndex = 1;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = false;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // FullScreenCheckBox
            // 
            this.FullScreenCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.FullScreenCheckBox.AutoSize = true;
            this.FullScreenCheckBox.Location = new System.Drawing.Point(374, 31);
            this.FullScreenCheckBox.Name = "FullScreenCheckBox";
            this.FullScreenCheckBox.Size = new System.Drawing.Size(79, 19);
            this.FullScreenCheckBox.TabIndex = 6;
            this.FullScreenCheckBox.Text = "Fullscreen";
            this.FullScreenCheckBox.UseVisualStyleBackColor = true;
            this.FullScreenCheckBox.CheckedChanged += new System.EventHandler(this.FullScreenCheckBox_CheckedChanged);
            // 
            // LoopedCheckBox
            // 
            this.LoopedCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LoopedCheckBox.AutoSize = true;
            this.LoopedCheckBox.Location = new System.Drawing.Point(374, 54);
            this.LoopedCheckBox.Name = "LoopedCheckBox";
            this.LoopedCheckBox.Size = new System.Drawing.Size(66, 19);
            this.LoopedCheckBox.TabIndex = 7;
            this.LoopedCheckBox.Text = "Looped";
            this.LoopedCheckBox.UseVisualStyleBackColor = true;
            this.LoopedCheckBox.CheckedChanged += new System.EventHandler(this.LoopedCheckBox_CheckedChanged);
            // 
            // DisplayModesView
            // 
            this.DisplayModesView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.DisplayModesView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(57)))), ((int)(((byte)(57)))));
            this.DisplayModesView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DisplayModesView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.DisplayModesView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.DisplayModesView.FullRowSelect = true;
            this.DisplayModesView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.DisplayModesView.HideSelection = false;
            this.DisplayModesView.Location = new System.Drawing.Point(14, 33);
            this.DisplayModesView.MultiSelect = false;
            this.DisplayModesView.Name = "DisplayModesView";
            this.DisplayModesView.Size = new System.Drawing.Size(170, 152);
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
            this.label1.Location = new System.Drawing.Point(14, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 15);
            this.label1.TabIndex = 6;
            this.label1.Text = "Resolution";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(194, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 15);
            this.label2.TabIndex = 8;
            this.label2.Text = "Aspect Ratio";
            // 
            // AspectRatioView
            // 
            this.AspectRatioView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.AspectRatioView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(57)))), ((int)(((byte)(57)))));
            this.AspectRatioView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.AspectRatioView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
            this.AspectRatioView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.AspectRatioView.FullRowSelect = true;
            this.AspectRatioView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.AspectRatioView.HideSelection = false;
            this.AspectRatioView.Location = new System.Drawing.Point(194, 33);
            this.AspectRatioView.MultiSelect = false;
            this.AspectRatioView.Name = "AspectRatioView";
            this.AspectRatioView.Scrollable = false;
            this.AspectRatioView.Size = new System.Drawing.Size(75, 152);
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
            this.label4.Location = new System.Drawing.Point(278, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(73, 15);
            this.label4.TabIndex = 12;
            this.label4.Text = "Multisample";
            // 
            // SamplingView
            // 
            this.SamplingView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.SamplingView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(57)))), ((int)(((byte)(57)))));
            this.SamplingView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SamplingView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader4});
            this.SamplingView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.SamplingView.FullRowSelect = true;
            this.SamplingView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.SamplingView.HideSelection = false;
            this.SamplingView.Location = new System.Drawing.Point(278, 33);
            this.SamplingView.MultiSelect = false;
            this.SamplingView.Name = "SamplingView";
            this.SamplingView.Scrollable = false;
            this.SamplingView.Size = new System.Drawing.Size(75, 152);
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
            this.VSyncCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.VSyncCheckBox.AutoSize = true;
            this.VSyncCheckBox.Location = new System.Drawing.Point(374, 77);
            this.VSyncCheckBox.Name = "VSyncCheckBox";
            this.VSyncCheckBox.Size = new System.Drawing.Size(58, 19);
            this.VSyncCheckBox.TabIndex = 13;
            this.VSyncCheckBox.Text = "VSync";
            this.VSyncCheckBox.UseVisualStyleBackColor = true;
            this.VSyncCheckBox.CheckedChanged += new System.EventHandler(this.VSyncCheckBox_CheckedChanged);
            // 
            // PreCacheCheckBox
            // 
            this.PreCacheCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PreCacheCheckBox.AutoSize = true;
            this.PreCacheCheckBox.Location = new System.Drawing.Point(374, 100);
            this.PreCacheCheckBox.Name = "PreCacheCheckBox";
            this.PreCacheCheckBox.Size = new System.Drawing.Size(74, 19);
            this.PreCacheCheckBox.TabIndex = 14;
            this.PreCacheCheckBox.Text = "Precache";
            this.PreCacheCheckBox.UseVisualStyleBackColor = true;
            this.PreCacheCheckBox.CheckedChanged += new System.EventHandler(this.PreCacheCheckBox_CheckedChanged);
            // 
            // StartUpDialog
            // 
            this.AcceptButton = this.StartBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(475, 198);
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
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.StartBtn);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(491, 4000);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(491, 237);
            this.Name = "StartUpDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Framefield T2 Player";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.StartUpDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button StartBtn;
        private System.Windows.Forms.Button CancelBtn;
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