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
using JetBrains.Annotations;

namespace ScreenshotPlugin.ShareX
{
    public static class Screenshot
    {
        /*public bool CaptureClientArea { get; set; } = false;
        public bool RemoveOutsideScreenArea { get; set; } = true;
        public bool CaptureShadow { get; set; } = false;
        public int ShadowOffset { get; set; } = 20;*/

        [CanBeNull]
        private static Bitmap CaptureRectangle(Rectangle rect)
        {
            //if (!RemoveOutsideScreenArea) return CaptureRectangleNative(rect);
            
            var bounds = CaptureHelpers.GetScreenBounds();
            rect = Rectangle.Intersect(bounds, rect);

            return CaptureRectangleNative(rect);
        }
        
        [CanBeNull]
        public static Bitmap CaptureFullscreen()
        {
            var bounds = CaptureHelpers.GetScreenBounds();

            return CaptureRectangle(bounds);
        }

        [CanBeNull]
        private static Bitmap CaptureWindow(IntPtr handle)
        {
            if (handle.ToInt32() <= 0) return null;
            var rect = CaptureHelpers.GetWindowRectangle(handle);

            /*if (CaptureClientArea)
            {
                if (PInvoke.User32.GetClientRect(handle, out var r))
                    rect = r.ToRectangle();
            }
            else
            {
                rect = CaptureHelpers.GetWindowRectangle(handle);
            }*/

            return CaptureRectangle(rect);

        }

        [CanBeNull]
        public static Bitmap CaptureActiveWindow()
        {
            var handle = PInvoke.User32.GetForegroundWindow();

            return CaptureWindow(handle);
        }

        [CanBeNull]
        public static Bitmap CaptureActiveMonitor()
        {
            var bounds = CaptureHelpers.GetActiveScreenBounds();

            return CaptureRectangle(bounds);
        }
        
        [CanBeNull]
        private static Bitmap CaptureRectangleNative(Rectangle rect)
        {
            var handle = PInvoke.User32.GetDesktopWindow();
            return CaptureRectangleNative(handle, rect);
        }

        [CanBeNull]
        private static Bitmap CaptureRectangleNative(IntPtr handle, Rectangle rect)
        {
            if (rect.Width == 0 || rect.Height == 0)
            {
                return null;
            }

            var hdcSrc = PInvoke.User32.GetWindowDC(handle);
            var hdcDest = PInvoke.Gdi32.CreateCompatibleDC(hdcSrc);
            var hBitmap = PInvoke.Gdi32.CreateCompatibleBitmap(hdcSrc, rect.Width, rect.Height);
            var hOld = PInvoke.Gdi32.SelectObject(hdcDest, hBitmap);

            PInvoke.Gdi32.BitBlt(hdcDest.DangerousGetHandle(), 0, 0, rect.Width, rect.Height, hdcSrc.DangerousGetHandle(), rect.X, rect.Y, 0x00CC0020 | 0x40000000);

            PInvoke.Gdi32.SelectObject(hdcDest, hOld);
            PInvoke.Gdi32.DeleteDC(hdcDest);
            PInvoke.User32.ReleaseDC(handle, hdcSrc.DangerousGetHandle());

            var bmp = Image.FromHbitmap(hBitmap);
            PInvoke.Gdi32.DeleteObject(hBitmap);

            return bmp;
        }
    }
}