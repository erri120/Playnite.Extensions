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

using System.Drawing;
using PInvoke;

namespace ScreenshotPlugin
{
    public static class NativeUtils
    {
        public static int GetX(this RECT r)
        {
            return r.left;
        }

        public static int GetY(this RECT r)
        {
            return r.top;
        }
        
        public static int GetWidth(this RECT r)
        {
            return r.right - r.left;
        }

        public static int GetHeight(this RECT r)
        {
            return r.bottom - r.top;
        }
        
        public static Rectangle ToRectangle(this RECT r)
        {
            return new Rectangle(r.left, r.top, r.GetWidth(), r.GetHeight());
        }

        public static RECT ToRECT(this Rectangle r)
        {
            return new RECT
            {
                left = r.Left,
                top = r.Top,
                bottom = r.Bottom,
                right = r.Right
            };
        }
    }
}