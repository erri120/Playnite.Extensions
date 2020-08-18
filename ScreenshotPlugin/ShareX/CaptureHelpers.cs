/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2020 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PInvoke;

namespace ScreenshotPlugin.ShareX
{
    public static class CaptureHelpers
    {
        private static readonly Version OSVersion = Environment.OSVersion.Version;
        
        private static bool IsWindowsVistaOrGreater()
        {
            return OSVersion.Major >= 6;
        }
        
        private static bool IsWindows10OrGreater(int build = -1)
        {
            return OSVersion.Major >= 10 && OSVersion.Build >= build;
        }
        
        public static Rectangle GetScreenBounds()
        {
            return SystemInformation.VirtualScreen;
        }

        private static bool IsDWMEnabled()
        {
            return IsWindowsVistaOrGreater() && NativeFunctions.DwmIsCompositionEnabled();
        }

        private static Point GetCursorPosition()
        {
            if (User32.GetCursorPos(out var point))
            {
                return point;
            }

            return Point.Empty;
        }
        
        public static Rectangle GetActiveScreenBounds()
        {
            return Screen.FromPoint(GetCursorPosition()).Bounds;
        }
        
        private static bool GetExtendedFrameBounds(IntPtr handle, out Rectangle rectangle)
        {
            var result = NativeFunctions.DwmGetWindowAttribute(handle, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                out var rect, Marshal.SizeOf(typeof(RECT)));
            
            rectangle = rect.ToRectangle();
            return result == 0;
        }

        private static bool GetBorderSize(IntPtr handle, out Size size)
        {
            var wi = new User32.WINDOWINFO();

            var result = User32.GetWindowInfo(handle, ref wi);

            size = result ? new Size((int)wi.cxWindowBorders, (int)wi.cyWindowBorders) : Size.Empty;

            return result;
        }

        private static Rectangle MaximizedWindowFix(IntPtr handle, Rectangle windowRect)
        {
            if (GetBorderSize(handle, out var size))
            {
                windowRect = new Rectangle(windowRect.X + size.Width, windowRect.Y + size.Height, windowRect.Width - (size.Width * 2), windowRect.Height - (size.Height * 2));
            }

            return windowRect;
        }
        
        public static Rectangle GetWindowRectangle(IntPtr handle)
        {
            var rect = Rectangle.Empty;

            if (IsDWMEnabled())
            {
                if (GetExtendedFrameBounds(handle, out var tempRect))
                {
                    rect = tempRect;
                }
            }

            if (rect.IsEmpty)
            {
                if(User32.GetWindowRect(handle, out var r))
                    rect = r.ToRectangle();
            }

            if (!IsWindows10OrGreater() && User32.IsZoomed(handle))
            {
                rect = MaximizedWindowFix(handle, rect);
            }

            return rect;
        }
    }
}