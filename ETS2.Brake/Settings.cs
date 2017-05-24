﻿using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace ETS2.Brake
{
    public class Settings
    {
        public int MaximumBreakAmount { get; set; } = 20;

        [JsonIgnore]
        public decimal CurrentIncreaseRatio { get; set; }

        public int IncreaseRatio { get; set; } = 1;

        public bool IsIncreaseRatioEnabled { get; set; } = true;

        public TimeSpan ResetIncreaseRatioTimeSpan { get; set; } = new TimeSpan(0, 0, 0, 0, 500);

        public bool Save(string path)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Load(string path)
        {
            try
            {
                var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
                Clone(settings);
                return true;
            }
            catch
            {
                return false;
            }
        }


        private void Clone(Settings baseClassInstance)
        {
            if (baseClassInstance == null) return;

            var fieldsOfClass = typeof(Settings).GetFields(
                BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var fi in fieldsOfClass)
            {
                try
                {
                    fi.SetValue(this, fi.GetValue(baseClassInstance));
                }
                catch
                {
                }
            }
        }
    }
}