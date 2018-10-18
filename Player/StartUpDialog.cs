// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Framefield.Core;
using SharpDX;
using SharpDX.Direct3D9;


namespace Framefield.Player
{
    public partial class StartUpDialog : Form
    {
        public ContextSettings Settings { get; private set; }
        public bool Accepted { get; private set; }

        public StartUpDialog() {
            Settings = new ContextSettings();
            InitializeComponent();
            
        }

        private void StartUpDialog_Load(object sender, EventArgs e) {
            var d3d = new Direct3D();
            if (d3d.Adapters.Count == 0)
                return;

            //update the display listview
            var adapter = d3d.Adapters[0];
            int currentDisplayModeIndex = 0;
            int i = 0;
            float dpi = this.CreateGraphics().DpiX;
            DisplayModesView.Columns[0].Width = DisplayModesView.Width- (int)(25.0f * (dpi/96));
            Height += (int)(20*dpi/96.0);
            
            foreach (var dm in adapter.GetDisplayModes(Format.X8R8G8B8)) {
                var item = new ListViewItem(String.Format("{0,4} x {1,4}, {2,3}Hz", dm.Width, dm.Height, dm.RefreshRate));
                DisplayModesView.Items.Add(item);
                _displayModesMap.Add(item.Index, dm);
                if (dm.ToString() == adapter.CurrentDisplayMode.ToString())
                    currentDisplayModeIndex = i;
                ++i;
            }
            DisplayModesView.Items[currentDisplayModeIndex].Selected = true;
            DisplayModesView.EnsureVisible(currentDisplayModeIndex);

            //update aspect ratio listview
            var itm = new ListViewItem("4:3");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 4.0/3.0);
            itm = new ListViewItem("5:4");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 5.0/4.0);
            itm = new ListViewItem("16:9");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 16.0/9.0);
            itm = new ListViewItem("16:10");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 16.0/10.0);

            double minimalAspectDistance = 9999.0;
            int minimalAspectIndex = -1;
            i = 0;
            foreach (var el in _aspectRatioMap) {
                var currentAspectDistance = Math.Abs(el.Value - adapter.CurrentDisplayMode.AspectRatio);
                if (currentAspectDistance < minimalAspectDistance) {
                    minimalAspectDistance = currentAspectDistance;
                    minimalAspectIndex = i;
                }
                ++i;
            }
            if (minimalAspectIndex < 0)
                minimalAspectIndex = 0;
            AspectRatioView.Items[minimalAspectIndex].Selected = true;
            AspectRatioView.EnsureVisible(minimalAspectIndex);

            //update sampling listview
            itm = new ListViewItem("Disabled");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 0);
            itm = new ListViewItem("2x");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 2);
            itm = new ListViewItem("4x");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 4);
            itm = new ListViewItem("8x");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 8);
            SamplingView.Items[1].Selected = true;
            SamplingView.EnsureVisible(1);

            //const int VENDOR_NVIDIA = 4318;
            //const int VENDOR_ATI_AMD = 4098;

            //bool isAtiAmdCard = adapter.Details.VendorId == VENDOR_ATI_AMD;

            FullScreenCheckBox.Checked = true;
            LoopedCheckBox.Checked = false;
            VSyncCheckBox.Checked = true;//!isAtiAmdCard;
            PreCacheCheckBox.Checked = true;
        }

        private void StartBtn_Click(object sender, EventArgs e) {
            Accepted = true;
        }

        private void CancelBtn_Click(object sender, EventArgs e) {
            Accepted = false;
        }

        private void ResolutionsView_SelectedIndexChanged(object sender, EventArgs e) {
            if (DisplayModesView.SelectedItems.Count > 0)
                Settings.DisplayMode = _displayModesMap[DisplayModesView.SelectedItems[0].Index];
        }

        private void SamplingView_SelectedIndexChanged(object sender, EventArgs e) {
            if (SamplingView.SelectedItems.Count > 0)
                Settings.Sampling = _samplingMap[SamplingView.SelectedItems[0].Index];
        }

        private void AspectRatioView_SelectedIndexChanged(object sender, EventArgs e) {
            if (AspectRatioView.SelectedItems.Count > 0)
                Settings.AspectRatio = _aspectRatioMap[AspectRatioView.SelectedItems[0].Index];
        }

        private void FullScreenCheckBox_CheckedChanged(object sender, EventArgs e) {
            Settings.FullScreen = FullScreenCheckBox.Checked;
        }

        private void LoopedCheckBox_CheckedChanged(object sender, EventArgs e) {
            Settings.Looped = LoopedCheckBox.Checked;
        }

        private void VSyncCheckBox_CheckedChanged(object sender, EventArgs e) {
            Settings.VSyncEnabled = VSyncCheckBox.Checked;
        }

        private void PreCacheCheckBox_CheckedChanged(object sender, EventArgs e) {
            Settings.PreCacheEnabled = PreCacheCheckBox.Checked;
        }

        private Dictionary<int, DisplayMode> _displayModesMap = new Dictionary<int, DisplayMode>();
        private Dictionary<int, double> _aspectRatioMap = new Dictionary<int, double>();
        private Dictionary<int, int> _samplingMap = new Dictionary<int, int>();
    }
}