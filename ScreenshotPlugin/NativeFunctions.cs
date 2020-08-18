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
using System.Runtime.InteropServices;
using System.Windows.Input;
using PInvoke;

namespace ScreenshotPlugin
{
    public static class NativeFunctions
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
        
        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();
        
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, DwmApi.DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute);
    }
}