// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;

namespace ScreenshotPlugin
{
    public enum HotkeyStatus
    {
        [Description("Registered")]
        Registered,
        [Description("Failed")]
        Failed,
        [Description("Not Configured")]
        NotConfigured
    }
    
    public class Hotkey
    {
        [JsonIgnore]
        public ushort ID { get; set; }
        [JsonProperty]
        public Key KeyCode { get; set; } = Key.None;
        [JsonProperty]
        public HotkeyStatus Status { get; set; } = HotkeyStatus.NotConfigured;
        [JsonProperty]
        public ModifierKeys KeyModifiers { get; set; } = ModifierKeys.None;
        [JsonIgnore]
        public bool IsValidHotkey => KeyCode != Key.None;

        public string DebugString()
        {
            return $"{(ID == 0 ? "" : $"ID: {ID} ")}Keys: \"{ToString()}\" Status: {Enum.GetName(typeof(HotkeyStatus), Status)}";
        }
        
        public override string ToString()
        {
            var str = new StringBuilder();

            if (KeyModifiers.HasFlag(ModifierKeys.Control))
                str.Append("Ctrl + ");
            if (KeyModifiers.HasFlag(ModifierKeys.Shift))
                str.Append("Shift + ");
            if (KeyModifiers.HasFlag(ModifierKeys.Alt))
                str.Append("Alt + ");
            if (KeyModifiers.HasFlag(ModifierKeys.Windows))
                str.Append("Win + ");

            str.Append(KeyCode);

            return str.ToString();
        }
    }
    
    public class HotkeyComparer : IEqualityComparer<Hotkey>
    {
        public bool Equals(Hotkey x, Hotkey y)
        {
            if (x == null) return false;
            if (y == null) return false;
            return x.ID == y.ID && x.KeyCode == y.KeyCode && x.Status == y.Status;
        }

        public int GetHashCode(Hotkey obj)
        {
            unchecked
            {
                var hashCode = (int) obj.ID;
                hashCode = (hashCode * 397) ^ (int) obj.KeyCode;
                hashCode = (hashCode * 397) ^ (int) obj.Status;
                return hashCode;
            }
        }
    }
}