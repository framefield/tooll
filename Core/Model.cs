// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using Newtonsoft.Json;


namespace Framefield.Core
{
    public class Model : INotifyPropertyChanged, IDisposable
    {
        public delegate void ChangedDelegate(object obj, EventArgs args);
        public event ChangedDelegate ClearEvent;
        public event ChangedDelegate GlobalTimeChangedEvent = (o, a) => {};

        public void Dispose()
        {
            MetaOpManager.Dispose();
        }

        /** 
         * Debug function to measure number of listeners
         */

        public Delegate[] GetGlobalTimeChangedEventInvocationList()
        {
            return GlobalTimeChangedEvent.GetInvocationList();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Operator HomeOperator { get; private set; }

        public OperatorPart MainOutput
        {
            get
            {
                if (HomeOperator.Outputs.Count > 0)
                    return HomeOperator.Outputs[0];
                else
                    return null;
            }
        }

        public MetaManager MetaOpManager { get; private set; }

        public void RebuildMetaOpManager()
        {
            MetaManager.Dipose();
            MetaOpManager = MetaManager.Instance;
            MetaOpManager.LoadMetaOperators();
            HomeOperator = MetaOpManager.HomeOperator.CreateOperator(Guid.NewGuid());
        }

        public double GlobalTime
        {
            get { return _globalTime; }
            set
            {
                _globalTime = value; 
                if (PropertyChanged != null)
                    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("GlobalTime"));

                GlobalTimeChangedEvent(this, EventArgs.Empty);
            }
        }

        public Model()
        {
            MetaOpManager = MetaManager.Instance;
            HomeOperator = MetaOpManager.HomeOperator.CreateOperator(Guid.NewGuid());
        }

        public void Clear()
        {
            _globalTime = 0;

            if (ClearEvent != null)
                ClearEvent(this, EventArgs.Empty);
        }

        public void Save()
        {
            MetaOpManager.StoreMetaOperators();
            StoreHomeOperator(@"Config\");
        }

        public void StoreHomeOperator(string path, bool clearChangedFlags = true)
        {
            using (var writer = new StreamWriter(path + @"Home.mop"))
            {
                var json = new Json();
                json.Writer = new JsonTextWriter(writer);
                json.Writer.Formatting = Formatting.Indented;
                json.WriteMetaOperator(HomeOperator.Definition);
                json.WriteOperator(HomeOperator);
            }
            if (clearChangedFlags)
                HomeOperator.Definition.Changed = false;
        }

        private void LoadInstanceData(string filename, Operator op)
        {
            var json = new Json();
            using (var reader = new StreamReader(filename))
            {
                json.Reader = new JsonTextReader(reader);
                json.ReadAndSetOperatorValues(op);
            }
        }

        private double _globalTime = 0.0;
    }

}
