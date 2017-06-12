using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace ETS2.Brake
{
    public class Settings
    {
        [JsonIgnore]
        public decimal CurrentIncreaseRatio { get; set; }

        /// <summary>
        ///     The time between increasing inside the increasing loop.
        /// </summary>
        public TimeSpan IncreaseDelay { get; set; } = new TimeSpan(0, 0, 0, 0, 50);

        /// <summary>
        ///     Set if increasing the increase ratio is enabled
        /// </summary>
        public bool IsIncreaseRatioEnabled { get; set; } = true;

        /// <summary>
        /// Set if a memory usage text will appear below the progressbar
        /// </summary>
        public bool ShowMemoryUsage { get; set; }

        /// <summary>
        ///     The time it takes to reset the increase value back to it's base after releasing the 'S' key.
        /// </summary>
        public TimeSpan ResetIncreaseRatioTimeSpan { get; set; } = new TimeSpan(0, 0, 0, 0, 500);

        /// <summary>
        ///     The base increasing value
        /// </summary>
        public int StartIncreaseRatio { get; set; } = 150;

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


        private void Clone(Settings baseClassInstance)
        {
            if (baseClassInstance == null) return;

            var fieldsOfClass = typeof(Settings).GetFields(
                BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var fi in fieldsOfClass)
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