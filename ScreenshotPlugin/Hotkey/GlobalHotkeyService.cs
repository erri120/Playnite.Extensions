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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using Extensions.Common;
using Newtonsoft.Json;
using Playnite.SDK;

namespace ScreenshotPlugin.Hotkey
{
    //partially from https://tyrrrz.me/blog/wndproc-in-wpf

    public static partial class PInvoke
    {
        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        public static extern ushort GlobalAddAtom(string lpString);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern ushort GlobalDeleteAtom(ushort nAtom);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, ModifierKeys fsModifiers, int vk);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
    
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
    
    public class GlobalHotkeyService : IDisposable
    {
        private readonly SpongeWindow _sponge;
        private readonly HashSet<Hotkey> _hotkeys;
        private readonly ILogger _logger;

        public delegate void HotkeyEventHandler(Hotkey hotkey);

        public event HotkeyEventHandler HotkeyPress;
        
        public GlobalHotkeyService(ILogger logger)
        {
            _sponge = new SpongeWindow();
            _sponge.WndProcCalled += (s, e) => ProcessMessage(e);

            _hotkeys = new HashSet<Hotkey>(new HotkeyComparer());
            _logger = logger;
        }

        public void RegisterHotkey(Hotkey hotkey)
        {
            if (hotkey.Status == HotkeyStatus.Registered)
            {
                _logger.Warn($"Hotkey {hotkey.DebugString()} is already registered!");
                return;
            }

            if (!hotkey.IsValidHotkey)
            {
                _logger.Warn($"Hotkey {hotkey.DebugString()} is not valid!");
                hotkey.Status = HotkeyStatus.NotConfigured;
                return;
            }

            if (hotkey.ID == 0)
            {
                var guid = Guid.NewGuid().ToString("N");
                hotkey.ID = PInvoke.GlobalAddAtom(guid);

                if (hotkey.ID == 0)
                {
                    hotkey.Status = HotkeyStatus.Failed;
                    _logger.Error($"Could not generate unique ID for Hotkey {hotkey.DebugString()}");
                    return;
                }
            }

            var vk = KeyInterop.VirtualKeyFromKey(hotkey.KeyCode);
            if (!PInvoke.RegisterHotKey(_sponge.Handle, hotkey.ID, hotkey.KeyModifiers, vk))
            {
                PInvoke.GlobalDeleteAtom(hotkey.ID);
                hotkey.ID = 0;
                hotkey.Status = HotkeyStatus.Failed;
                _logger.Error($"Could not register hotkey {hotkey.DebugString()}");
                return;
            }

            hotkey.Status = HotkeyStatus.Registered;
            _hotkeys.Add(hotkey);
            _logger.Info($"Registered hotkey {hotkey.DebugString()}");
        }

        public void UnregisterHotkey(Hotkey hotkey)
        {
            if (hotkey.ID == 0)
            {
                hotkey.Status = HotkeyStatus.Failed;
                _logger.Error($"Unable to unregister hotkey {hotkey.DebugString()}, ID is 0!");
                return;
            }

            if (PInvoke.UnregisterHotKey(_sponge.Handle, hotkey.ID))
            {
                PInvoke.GlobalDeleteAtom(hotkey.ID);
                hotkey.ID = 0;
                hotkey.Status = HotkeyStatus.NotConfigured;
                _logger.Info($"Unregistered hotkey {hotkey.DebugString()}");
                return;
            }

            hotkey.Status = HotkeyStatus.Failed;
            _logger.Error($"Unable to unregister hotkey {hotkey.DebugString()}");
        }

        private void UnregisterAllHotkeys()
        {
            _hotkeys.Do(UnregisterHotkey);
            _hotkeys.Clear();
        }
        
        private void ProcessMessage(Message m)
        {
            if (m.Msg != 0x0312)
                return;

            var id = (ushort)m.WParam;
            var hotkey = _hotkeys.FirstOrDefault(x => x.ID == id);
            if (hotkey == null)
                return;

            var key = KeyInterop.KeyFromVirtualKey(((int)m.LParam >> 16) & 0xFFFF);
            var modifiers = (ModifierKeys) ((int)m.LParam & 0xFFFF);
            //var key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
            //var modifiers = (PInvoke.KeyModifiers)((int)m.LParam & 0xFFFF);

            if (hotkey.KeyCode != key)
            {
                _logger.Error($"Hotkey {hotkey.DebugString()} got pressed but key in message is {key}!");
                return;
            }

            if (hotkey.KeyModifiers != modifiers)
            {
                _logger.Error($"Hotkey {hotkey.DebugString()} got pressed but modifiers are {modifiers}");
                return;
            }

            HotkeyPress?.Invoke(hotkey);
        }
        
        public void Dispose()
        {
            UnregisterAllHotkeys();
        }
    }
}