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

        public StartUpDialog(ContextSettings settings) {
            Settings = settings;
            InitializeComponent();
            
        }

        private void StartUpDialog_Load(object sender, EventArgs e) {
            var d3d = new Direct3D();
            if (d3d.Adapters.Count == 0)
                return;

            //update the display listview
            var adapter = d3d.Adapters[0];
            int currentDisplayModeIndex = 0, selectedDisplayModeIndex = -1;
            int i = 0;
            float dpi = this.CreateGraphics().DpiX;
            DisplayModesView.Columns[0].Width = DisplayModesView.Width- (int)(25.0f * (dpi/96));
            Height += (int)(20*dpi/96.0);
            
            foreach (var dm in adapter.GetDisplayModes(Format.X8R8G8B8)) {
                var item = new ListViewItem(String.Format("{0} x {1}   {2} Hz",
                    dm.Width.ToString().PadLeft(4, 'x').Replace("x", "  "),
                    dm.Height.ToString().PadLeft(4, 'x').Replace("x", "  "),
                    dm.RefreshRate.ToString().PadLeft(3, 'x').Replace("x", "  ")));
                DisplayModesView.Items.Add(item);
                _displayModesMap.Add(item.Index, dm);
                if (dm.ToString() == adapter.CurrentDisplayMode.ToString())
                    currentDisplayModeIndex = i;
                if (dm.Width  == Settings.DisplayMode.Width
                &&  dm.Height == Settings.DisplayMode.Height
                &&  (selectedDisplayModeIndex < 1
                ||  dm.RefreshRate == Settings.DisplayMode.RefreshRate))
                    selectedDisplayModeIndex = i;
                ++i;
            }
            if (selectedDisplayModeIndex < 0)
                selectedDisplayModeIndex = currentDisplayModeIndex;
            DisplayModesView.Items[selectedDisplayModeIndex].Selected = true;
            DisplayModesView.EnsureVisible(selectedDisplayModeIndex);
            Settings.DisplayMode = _displayModesMap[selectedDisplayModeIndex];
            Settings.Validate();  // set aspect ratio (if not already done so)

            //update aspect ratio listview
            var itm = new ListViewItem("21:9");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 21.0 / 9.0);
            itm = new ListViewItem("16:9");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 16.0 / 9.0);
            itm = new ListViewItem("16:10");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 16.0 / 10.0);
            itm = new ListViewItem("3:2");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 3.0 / 2.0);
            itm = new ListViewItem("4:3");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 4.0 / 3.0);
            itm = new ListViewItem("5:4");
            AspectRatioView.Items.Add(itm);
            _aspectRatioMap.Add(itm.Index, 5.0 / 4.0);

            double minimalAspectDistance = 9999.0;
            int minimalAspectIndex = 0;
            i = 0;
            foreach (var el in _aspectRatioMap) {
                var currentAspectDistance = Math.Abs(el.Value - Settings.AspectRatio);
                if (currentAspectDistance < minimalAspectDistance) {
                    minimalAspectDistance = currentAspectDistance;
                    minimalAspectIndex = i;
                }
                ++i;
            }
            AspectRatioView.Items[minimalAspectIndex].Selected = true;
            AspectRatioView.EnsureVisible(minimalAspectIndex);

            //update sampling listview
            itm = new ListViewItem("Disabled");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 0);
            itm = new ListViewItem("  2x");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 2);
            itm = new ListViewItem("  4x");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 4);
            itm = new ListViewItem("  8x");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 8);
            itm = new ListViewItem("16x");
            SamplingView.Items.Add(itm);
            _samplingMap.Add(itm.Index, 16);

            int minimalSamplingDistance = 9999;
            int minimalSamplingIndex = 0;
            i = 0;
            foreach (var el in _samplingMap)
            {
                var currentSamplingDistance = Math.Abs(el.Value - Settings.Sampling);
                if (currentSamplingDistance < minimalSamplingDistance)
                {
                    minimalSamplingDistance = currentSamplingDistance;
                    minimalSamplingIndex = i;
                }
                ++i;
            }
            SamplingView.Items[minimalSamplingIndex].Selected = true;
            SamplingView.EnsureVisible(minimalSamplingIndex);

            FullScreenCheckBox.Checked = Settings.FullScreen;
            LoopedCheckBox.Checked = Settings.Looped;
            VSyncCheckBox.Checked = Settings.VSyncEnabled;
            PreCacheCheckBox.Checked = Settings.PreCacheEnabled;
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