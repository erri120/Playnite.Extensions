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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using Extensions.Common;
using JetBrains.Annotations;
using Playnite.SDK;

namespace ScreenshotPlugin
{
    //partially from https://tyrrrz.me/blog/wndproc-in-wpf

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

        public void RegisterHotkey([CanBeNull] Hotkey hotkey)
        {
            if (hotkey == null) return;

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
                hotkey.ID = NativeFunctions.GlobalAddAtom(guid);

                if (hotkey.ID == 0)
                {
                    hotkey.Status = HotkeyStatus.Failed;
                    _logger.Error($"Could not generate unique ID for Hotkey {hotkey.DebugString()}");
                    return;
                }
            }

            var vk = KeyInterop.VirtualKeyFromKey(hotkey.KeyCode);
            if (!NativeFunctions.RegisterHotKey(_sponge.Handle, hotkey.ID, hotkey.KeyModifiers, vk))
            {
                NativeFunctions.GlobalDeleteAtom(hotkey.ID);
                hotkey.ID = 0;
                hotkey.Status = HotkeyStatus.Failed;
                _logger.Error($"Could not register hotkey {hotkey.DebugString()}");
                return;
            }

            hotkey.Status = HotkeyStatus.Registered;
            _hotkeys.Add(hotkey);
            _logger.Info($"Registered hotkey {hotkey.DebugString()}");
        }

        public void UnregisterHotkey([CanBeNull] Hotkey hotkey)
        {
            if (hotkey == null) return;
            
            if (hotkey.ID == 0)
            {
                hotkey.Status = HotkeyStatus.Failed;
                _logger.Error($"Unable to unregister hotkey {hotkey.DebugString()}, ID is 0!");
                return;
            }

            if (NativeFunctions.UnregisterHotKey(_sponge.Handle, hotkey.ID))
            {
                NativeFunctions.GlobalDeleteAtom(hotkey.ID);
                hotkey.ID = 0;
                hotkey.Status = HotkeyStatus.NotConfigured;
                _logger.Info($"Unregistered hotkey {hotkey.DebugString()}");
                _hotkeys.Remove(hotkey);
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