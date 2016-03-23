// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;


namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for CodeSectionTabControl.xaml
    /// </summary>
    //note: this assumes that a sectionid is unique within all code sections
    public partial class CodeSectionTabControl : UserControl
    {

        #region Properties
        public ObservableCollection<CodeSectionViewModel> JoinedCodeSections { get; set; }

        public List<CodeSectionManager> CodeSectionManagers {
            get { return _codeSectionManagers; } 
            set { 
                //remove old handlers
                if (_codeSectionManagers != null) {
                    foreach (var csm in _codeSectionManagers) {
                        csm.CodeSections.CollectionChanged -=CodeSections_CollectionChangedHandler;
                    }
                }

                _codeSectionManagers = value;
            
                foreach(var csm in _codeSectionManagers) {
                    csm.CodeSections.CollectionChanged += CodeSections_CollectionChangedHandler;
                }
                BuildJoinedCodeSectionList();
            } 
        }
        #endregion

        public CodeSectionTabControl() {
            InitializeComponent();
            JoinedCodeSections = new ObservableCollection<CodeSectionViewModel>();
        }

        public void SetActiveSectionId(string sectionId) {
            int foundIdx = -1;
            int idx = 0;
            foreach (var csvm in JoinedCodeSections) {
                if (csvm.Id == sectionId) {
                    foundIdx = idx;
                    break;
                }
                ++idx;
            }
            if (foundIdx >= 0) {
                //workaround to enforce triggering a changed signal even if the new index equals the old one
                XSectionTabControl.SelectedIndex = -1; 
                XSectionTabControl.SelectedIndex = foundIdx;
            }
        }

        #region defining change event
        public class SectionChangedEventArgs : EventArgs
        {
            public CodeSectionManager CodeSectionManager { get; set; }
            public CodeSectionViewModel CodeSection { get; set; }
        }
        public delegate void SectionClickedDelegate(object o, SectionChangedEventArgs e);
        public event SectionClickedDelegate SectionChangedEvent;
        #endregion

        #region event handlers
        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (App.Current != null) {
                var sectionBinding = new Binding() {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Source = this,
                    Path = new PropertyPath("JoinedCodeSections")
                };

                BindingOperations.SetBinding(XSectionTabControl, ItemsControl.ItemsSourceProperty, sectionBinding);
            }
        }

        void CodeSections_CollectionChangedHandler(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            BuildJoinedCodeSectionList();
        }

        private void TabControl_SelectionChangedHandler(object sender, SelectionChangedEventArgs e) {
            if (!_tabChangedEventsEnabled)
                return;

            var tc = sender as TabControl;
            if( tc == null)
                return;


            var index = tc.SelectedIndex;
            if (index == -1)
                return;
                
            var csvm = JoinedCodeSections[index];
        
            CodeSectionManager csm = null;
            foreach(var csmI in CodeSectionManagers) {
                foreach( var cs in csmI.CodeSections ) {
                    if( cs == csvm ) {
                        csm = csmI;
                        break;
                    }
                }
            }
            

            SectionChangedEvent(this, new SectionChangedEventArgs() { CodeSection= csvm, CodeSectionManager= csm });
        }
        #endregion

        #region private stuff

        private void BuildJoinedCodeSectionList() {
            _tabChangedEventsEnabled = false;
            JoinedCodeSections.Clear();
            foreach(var csm in _codeSectionManagers) {                
                foreach (var cs in csm.CodeSections) {
                    if (!cs.Id.StartsWith("_")) {
                        JoinedCodeSections.Add(cs);
                    }
                }
            }
            _tabChangedEventsEnabled = true;
        }
        private List<CodeSectionManager> _codeSectionManagers;
        private bool _tabChangedEventsEnabled = true;

        #endregion

    }
}
