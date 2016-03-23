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
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for BreadCrumbs.xaml
    /// You find further functionality in CompositionGraphView.AddNestingStep
    /// 
    /// The Breadscrumbs consist of a series of buttons to jump and of the current level.
    /// For each Crumb we als store the settings on how to jump up from this Crumb. The
    /// Root Crumb (Main) has jumpUp settings of null.
    /// 
    /// </summary>
    public partial class BreadCrumbsView : DockPanel
    {
        public BreadCrumbsView()
        {
            InitializeComponent();
        }



        public void Clear() {
            Children.Clear();
            _nestingHierachy.Clear();
        }


        public class Level
        {
            public Operator Operator { get; set; }
            public Operator SubOperator { get; set; }
            public TimeView.State TimeViewState { get; set; }            
            public Button Button { get; set; }
        }
        private List<Level> _nestingHierachy = new List<Level>();
        public event EventHandler<System.EventArgs> JumpOutEvent = (o, a) => { };

        public void Push(Operator op) 
        {
            if( _nestingHierachy.Count > 0) {
                _nestingHierachy.Last().TimeViewState= CV.XTimeView.CreateState();
                _nestingHierachy.Last().SubOperator= op;
            }

            var newButton = new Button();
            newButton.Content = op.Name == string.Empty
                ? op.Definition.Name
                : op.Name;
            Children.Add(newButton);
            newButton.Click +=new RoutedEventHandler(newButton_Click);

            _nestingHierachy.Add( new Level(){ Operator= op, Button= newButton } );
            
            UpdateHighlight();
            return;
        }

        public void Push(IEnumerable<Operator> ops)
        {
            foreach (var op in ops)
            {
                Push(op);
            }
        }

        void newButton_Click(object sender, RoutedEventArgs e) 
        {
            var button = sender as Button;
            if (button != null) {
                if (button != _nestingHierachy.Last().Button) {
                    while (_nestingHierachy.Count >= 1) {
                        var level= _nestingHierachy.Last();
                        if (level.Button != button) {
                            _nestingHierachy.Remove(level);
                            Children.Remove(level.Button);
                        }
                        else {
                            JumpOutEvent(level, new RoutedEventArgs());
                            level.TimeViewState = null;
                            level.SubOperator = null;
                            break;
                        }
                    }
                    UpdateHighlight();
                    e.Handled= true;
                }
            }            
        }

        private void UpdateHighlight() {
            for (int i=0; i < Children.Count -1; ++i) {
                var crumb = Children[i] as Button;
                if (crumb != null) {
                    crumb.IsDefault = false;
                }
            }
            if (Children.Count > 0) {
                var crumb = Children[Children.Count -1] as Button;
                if (crumb != null) {
                    crumb.IsDefault = true;
                }
            }
        }

        protected override Size MeasureOverride(Size constraint) {
            if (Children.Count < 1) return new Size(0, 0);

            foreach (UIElement child in Children) {
                child.Measure(new Size(Double.PositiveInfinity, constraint.Height));
            }
            return new Size(Math.Min( constraint.Width, 1200), Children[0].DesiredSize.Height);
        }

        protected override Size ArrangeOverride(Size arrangeSize) {

            // Collect requested widths
            List<double> widths = new List<double>();
            double sumRequested= 0;
            for (int i= 0; i<Children.Count; ++i) {
                var crumb = Children[i] as Button;
                if (crumb != null) {
                    double w= crumb.DesiredSize.Width;
                    widths.Add(w);
                    sumRequested+= w;
                }
            }

            if (sumRequested > arrangeSize.Width) {
                double newLastWidth = Math.Min(widths.Last(), arrangeSize.Width * 0.6);              

                double shrinkFactor=   (arrangeSize.Width - newLastWidth)/ (sumRequested - widths.Last());
                widths[widths.Count-1] = newLastWidth;

                for (int ii=0; ii < widths.Count-1; ++ii) {
                    widths[ii]*= shrinkFactor;
                }
            }


            int j=0;
            double x=0;
            foreach (UIElement child in Children) {
                child.Arrange(new Rect(x, 0, widths[j], arrangeSize.Height));
                x+= widths[j];
                j++;
            }

            //return base.ArrangeOverride(arrangeSize);
            return arrangeSize;
        }

        private CompositionView CV {
            get {
                if (_CV == null)
                    _CV = UIHelper.FindParent<CompositionView>(this);
                return _CV;
            }
        }
        private CompositionView _CV;

    }
}
