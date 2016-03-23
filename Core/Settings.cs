// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Framefield.Core
{

    public class Settings
    {
        public Object this[string key]
        {
            get
            {
                if (_values.ContainsKey(key))
                {
                    return _values[key];
                }
                throw new ArgumentException(String.Format("Requesting undefined setting: '{0}'", key));
            }
            set
            {
                _values[key] = value;
            }
        }

        public Settings(String filepath)
        {
            _filepath = filepath;
            if (File.Exists(_filepath))
            {
                string json = null;
                using (var reader = new StreamReader(_filepath))
                {
                    json = reader.ReadToEnd();
                }
                _values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            else
            {
                _values = new Dictionary<string, object>();
            }
        }

        ~Settings()
        {
            Save();
        }

        public bool Contains(string key)
        {
            return _values.ContainsKey(key);
        }

        public void Remove(string key)
        {
            _values.Remove(key);
        }

        public string TryGet(string key, string defaultValue)
        {
            object value;
            if (_values.TryGetValue(key, out value))
            {
                return Convert.ToString(value);
            }

            return defaultValue;
        }

        public double TryGet(string key, double defaultValue)
        {
            object value;
            if (_values.TryGetValue(key, out value))
            {
                return Convert.ToDouble(value);
            }

            return defaultValue;
        }

        public bool TryGet(string key, bool defaultValue)
        {
            object value;
            if (_values.TryGetValue(key, out value))
            {
                return Convert.ToBoolean(value);
            }

            return defaultValue;
        }

        public string GetOrSetDefault(string key, string defaultValue)
        {
            if (!_values.ContainsKey(key))
            {
                _values[key] = defaultValue;
                return defaultValue;
            }
            else
            {
                return Convert.ToString(_values[key]);
            }
        }

        public double GetOrSetDefault(string key, double defaultValue)
        {
            if (!_values.ContainsKey(key))
            {
                _values[key] = defaultValue;
                return defaultValue;
            }
            else
            {
                return Convert.ToDouble(_values[key]);
            }
        }

        public bool GetOrSetDefault(string key, bool defaultValue)
        {
            if (!_values.ContainsKey(key))
            {
                _values[key] = defaultValue;
                return defaultValue;
            }
            else
            {
                return Convert.ToBoolean(_values[key]);
            }
        }

        public void Save()
        {
            SaveAs(_filepath);
        }

        public void SaveAs(string filePath)
        {
            var json = JsonConvert.SerializeObject(_values, Formatting.Indented);
            using (var sw = new StreamWriter(filePath))
            {
                sw.Write(json);
            }
        }

        private readonly string _filepath;
        private Dictionary<String, Object> _values;
    }
}
