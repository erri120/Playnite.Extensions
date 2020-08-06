using HtmlAgilityPack;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Web;

namespace Extensions.Common
{
    public static class Utils
    {
        private static JsonSerializerSettings GenericJsonSettings => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, GenericJsonSettings);
        }

        public static T FromJson<T>(this string data)
        {
            return JsonConvert.DeserializeObject<T>(data, GenericJsonSettings);
        }

        public static bool IsEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static bool IsEmpty(this string s, ILogger logger, string name, string id)
        {
            if (!IsEmpty(s))
                return false;

            logger.Warn($"{name} for {id} is empty!");
            return true;
        }

        public static bool IsNull(this HtmlNode node, ILogger logger, string name, string id)
        {
            if (node != null)
                return false;

            logger.Warn($"Found no {name} node for {id}!");
            return true;
        }

        public static bool IsNullOrEmpty(this HtmlNodeCollection collection, ILogger logger, string name, string id)
        {
            if (collection != null && collection.Count > 0)
                return false;

            logger.Warn($"Found no {name} for {id}!");
            return true;
        }

        public static bool TryGetInnerText(this HtmlNode baseNode, string xpath, ILogger logger, string name, string id, out string innerText)
        {
            innerText = null;
            var node = baseNode.SelectSingleNode(xpath);
            if (node.IsNull(logger, name, id))
                return false;

            var sText = node.DecodeInnerText();
            if (sText.IsEmpty(logger, name, id))
                return false;

            innerText = sText;
            return true;
        }

        public static void Do<T>(this IEnumerable<T> col, Action<T> a)
        {
            foreach (var item in col) a(item);
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> col)
        {
            return col.Where(x => x != null);
        }

        public static string GetValue(this HtmlNode node, string attr)
        {
            if (!node.HasAttributes)
                return null;
            var value = node.GetAttributeValue(attr, string.Empty);

            return value.IsEmpty() ? null : value;
        }

        public static string DecodeInnerText(this HtmlNode node)
        {
            return HttpUtility.HtmlDecode(node?.InnerText);
        }

        public static List<string> CollageImage(    List<string> ImageURLs,
                                                    bool UsePattern1 = true, 
                                                    bool UsePattern2 = true, 
                                                    bool UsePattern3 = true,
                                                    bool SkipFirst = true )
        {
            if (ImageURLs.Count > 1)
            {
                Tuple<int, int> GoalResolution = new Tuple<int, int>(1920, 1080);
                List<int> x_Size = new List<int>();
                List<int> y_Size = new List<int>();

                List<Bitmap> BitmapList = new List<Bitmap>();

                try
                {
                    foreach (string url in ImageURLs)
                    {

                        System.Net.WebRequest request =
                            System.Net.WebRequest.Create(url);
                        System.Net.WebResponse response = request.GetResponse();
                        System.IO.Stream responseStream = response.GetResponseStream();

                        Bitmap bitmapImage = new Bitmap(responseStream);

                        x_Size.Add(bitmapImage.Width);
                        y_Size.Add(bitmapImage.Height);
                        BitmapList.Add(bitmapImage);

                    }

                    x_Size.Sort();
                    y_Size.Sort();

                    Rectangle TileTemplate = new Rectangle(0, 0, x_Size[0], y_Size[0]);

                    Tuple<int, int> ImageSlots = new Tuple<int, int>(
                        Convert.ToInt32(    // X Slots
                            Math.Ceiling(
                                Convert.ToDecimal(GoalResolution.Item1)
                                / Convert.ToDecimal(TileTemplate.Width)
                            )),
                        Convert.ToInt32(    // Y Slots
                            Math.Ceiling(
                                Convert.ToDecimal(GoalResolution.Item2)
                                / Convert.ToDecimal(TileTemplate.Height)
                            )));

                    if (UsePattern1)
                    {
                        Bitmap canvas = new Bitmap(
                            ImageSlots.Item1 * TileTemplate.Width,
                            ImageSlots.Item2 * TileTemplate.Height);

                        using (Graphics canvasGraphic = Graphics.FromImage(canvas))
                        {
                            Point CurrentSlot = new Point(0, 0);
                            int CurrentImage = 0;
                            int TotalImages = BitmapList.Count - 1; // Indexing to 0

                            while (CurrentSlot.Y < ImageSlots.Item2)
                            {
                                while (CurrentSlot.X < ImageSlots.Item1)
                                {
                                    TileTemplate.X = TileTemplate.Width * CurrentSlot.X;
                                    TileTemplate.Y = TileTemplate.Height * CurrentSlot.Y;

                                    canvasGraphic.DrawImage(BitmapList[CurrentImage], TileTemplate);

                                    CurrentSlot.X++;

                                    if (CurrentImage == TotalImages)
                                    {
                                        CurrentImage = 0;
                                    }
                                    else
                                    {
                                        CurrentImage++;
                                    }
                                }

                                CurrentSlot.Y++;
                                CurrentSlot.X = 0;
                            }

                            string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";

                            canvas.Save(fileName, ImageFormat.Png);
                            canvas.Dispose();

                            ImageURLs.Add(fileName);
                        }
                    }

                    if (UsePattern2)
                    {
                        Bitmap canvas = new Bitmap(
                            ImageSlots.Item1 * TileTemplate.Width,
                            ImageSlots.Item2 * TileTemplate.Height);

                        int Modifier = 1;

                        using (Graphics canvasGraphic = Graphics.FromImage(canvas))
                        {
                            Point CurrentSlot = new Point(0, 0);
                            int CurrentImage = 0;
                            int TotalImages = BitmapList.Count - 1; // Indexing to 0

                            while (CurrentSlot.Y < ImageSlots.Item2)
                            {
                                while (CurrentSlot.X < ImageSlots.Item1)
                                {
                                    TileTemplate.X = TileTemplate.Width * CurrentSlot.X;
                                    TileTemplate.Y = TileTemplate.Height * CurrentSlot.Y;

                                    canvasGraphic.DrawImage(BitmapList[CurrentImage], TileTemplate);

                                    CurrentSlot.X++;

                                    if (CurrentImage == TotalImages)
                                    {
                                        Modifier = -1;
                                    }
                                    else if (CurrentImage == 0)
                                    {
                                        Modifier = 1;
                                    }

                                    CurrentImage += Modifier;
                                }

                                CurrentSlot.Y++;
                                CurrentSlot.X = 0;
                            }

                            string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";

                            canvas.Save(fileName, ImageFormat.Png);
                            canvas.Dispose();

                            ImageURLs.Add(fileName);
                        }
                    }

                    if (UsePattern3)
                    {
                        Bitmap canvas = new Bitmap(
                            ImageSlots.Item1 * TileTemplate.Width,
                            ImageSlots.Item2 * TileTemplate.Height);

                        using (Graphics canvasGraphic = Graphics.FromImage(canvas))
                        {
                            Point CurrentSlot = new Point(0, 0);
                            int TotalImages = BitmapList.Count - 1; // Indexing to 0
                            Random rnd = new Random();

                            while (CurrentSlot.Y < ImageSlots.Item2)
                            {
                                while (CurrentSlot.X < ImageSlots.Item1)
                                {
                                    TileTemplate.X = TileTemplate.Width * CurrentSlot.X;
                                    TileTemplate.Y = TileTemplate.Height * CurrentSlot.Y;

                                    canvasGraphic.DrawImage(BitmapList[rnd.Next(BitmapList.Count)], TileTemplate);

                                    CurrentSlot.X++;

                                }

                                CurrentSlot.Y++;
                                CurrentSlot.X = 0;
                            }

                            string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";

                            canvas.Save(fileName, ImageFormat.Png);
                            canvas.Dispose();

                            ImageURLs.Add(fileName);
                        }
                    }

                    if (SkipFirst & (ImageURLs.Count > 2))
                    {
                        BitmapList.RemoveAt(0);

                        if (UsePattern1)
                        {
                            Bitmap canvas = new Bitmap(
                                ImageSlots.Item1 * TileTemplate.Width,
                                ImageSlots.Item2 * TileTemplate.Height);

                            using (Graphics canvasGraphic = Graphics.FromImage(canvas))
                            {
                                Point CurrentSlot = new Point(0, 0);
                                int CurrentImage = 0;
                                int TotalImages = BitmapList.Count - 1; // Indexing to 0

                                while (CurrentSlot.Y < ImageSlots.Item2)
                                {
                                    while (CurrentSlot.X < ImageSlots.Item1)
                                    {
                                        TileTemplate.X = TileTemplate.Width * CurrentSlot.X;
                                        TileTemplate.Y = TileTemplate.Height * CurrentSlot.Y;

                                        canvasGraphic.DrawImage(BitmapList[CurrentImage], TileTemplate);

                                        CurrentSlot.X++;

                                        if (CurrentImage == TotalImages)
                                        {
                                            CurrentImage = 0;
                                        }
                                        else
                                        {
                                            CurrentImage++;
                                        }
                                    }

                                    CurrentSlot.Y++;
                                    CurrentSlot.X = 0;
                                }

                                string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";

                                canvas.Save(fileName, ImageFormat.Png);
                                canvas.Dispose();

                                ImageURLs.Add(fileName);
                            }
                        }

                        if (UsePattern2)
                        {
                            Bitmap canvas = new Bitmap(
                                ImageSlots.Item1 * TileTemplate.Width,
                                ImageSlots.Item2 * TileTemplate.Height);

                            int Modifier = 1;

                            using (Graphics canvasGraphic = Graphics.FromImage(canvas))
                            {
                                Point CurrentSlot = new Point(0, 0);
                                int CurrentImage = 0;
                                int TotalImages = BitmapList.Count - 1; // Indexing to 0

                                while (CurrentSlot.Y < ImageSlots.Item2)
                                {
                                    while (CurrentSlot.X < ImageSlots.Item1)
                                    {
                                        TileTemplate.X = TileTemplate.Width * CurrentSlot.X;
                                        TileTemplate.Y = TileTemplate.Height * CurrentSlot.Y;

                                        canvasGraphic.DrawImage(BitmapList[CurrentImage], TileTemplate);

                                        CurrentSlot.X++;

                                        if (CurrentImage == TotalImages)
                                        {
                                            Modifier = -1;
                                        }
                                        else if (CurrentImage == 0)
                                        {
                                            Modifier = 1;
                                        }

                                        CurrentImage += Modifier;
                                    }

                                    CurrentSlot.Y++;
                                    CurrentSlot.X = 0;
                                }

                                string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";

                                canvas.Save(fileName, ImageFormat.Png);
                                canvas.Dispose();

                                ImageURLs.Add(fileName);
                            }
                        }

                        if (UsePattern3)
                        {
                            Bitmap canvas = new Bitmap(
                                ImageSlots.Item1 * TileTemplate.Width,
                                ImageSlots.Item2 * TileTemplate.Height);

                            using (Graphics canvasGraphic = Graphics.FromImage(canvas))
                            {
                                Point CurrentSlot = new Point(0, 0);
                                int TotalImages = BitmapList.Count - 1; // Indexing to 0
                                Random rnd = new Random();

                                while (CurrentSlot.Y < ImageSlots.Item2)
                                {
                                    while (CurrentSlot.X < ImageSlots.Item1)
                                    {
                                        TileTemplate.X = TileTemplate.Width * CurrentSlot.X;
                                        TileTemplate.Y = TileTemplate.Height * CurrentSlot.Y;

                                        canvasGraphic.DrawImage(BitmapList[rnd.Next(BitmapList.Count)], TileTemplate);

                                        CurrentSlot.X++;

                                    }

                                    CurrentSlot.Y++;
                                    CurrentSlot.X = 0;
                                }

                                string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";

                                canvas.Save(fileName, ImageFormat.Png);
                                canvas.Dispose();

                                ImageURLs.Add(fileName);
                            }
                        }
                    }

                    
                }
                finally
                {
                    foreach (Bitmap bitmap in BitmapList)
                    {
                        bitmap.Dispose();
                    }
                }

            }

            return ImageURLs;

        }
    }
}