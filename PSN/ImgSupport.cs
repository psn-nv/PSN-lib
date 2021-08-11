using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PSN
{
    /// <summary>
    /// Работа с изображениями.
    /// </summary>
    public static class ImgSupport
    {
        /// <summary>
        /// Создание картинок предпросмотра
        /// </summary>
        /// <param name="file_path">Путь к исходному файлу.</param>
        /// <param name="thumbnail_path">Путь к файлу предпросмотра.</param>
        /// <param name="ratio">Коэффициент изменения размеров. От 0 до 1 - уменьшение. Больше 1 - увеличение. Пропорционален и высоте и длине.</param>
        public static void CreateThumbnail(string file_path, string thumbnail_path, float ratio)
        {
            Image img = Image.FromFile(file_path);
            
            int w = (int)Math.Round(img.Width * ratio);
            int h = (int)Math.Round(img.Height * ratio);

            ScaleImage(img, w, h).Save(thumbnail_path);            
        }
        /// <summary>
        /// Создание картинок предпросмотра с фиксированной шириной.
        /// </summary>
        /// <param name="file_path">Путь к исходному файлу.</param>
        /// <param name="thumbnail_path">Путь к файлу предпросмотра.</param>
        public static void CreateThumbnail(string file_path, string thumbnail_path)
        {
            int max_width = 50;
            int img_width = Image.FromFile(file_path).Width;
            float ratio = (float)max_width / img_width;
            CreateThumbnail(file_path, thumbnail_path, ratio);
        }
        /// <summary>
        /// Масштабирование картинки.
        /// </summary>
        /// <param name="image">Картинка оригинал.</param>
        /// <param name="img_width">Новая ширина.</param>
        /// <param name="img_height">Новая высота.</param>
        /// <returns></returns>
        public static Image ScaleImage(Image image, int img_width, int img_height)
        {
            Bitmap result = new Bitmap(img_width, img_height);
            Graphics.FromImage(result).DrawImage(image, 0, 0, img_width, img_height);
            return result;
        }
        /// <summary>
        /// Получение иконки из рисунка
        /// </summary>
        /// <param name="img">Преобразуемый рисунок</param>
        /// <returns></returns>
        public static Icon IconFromImage(Image img)
        {
            var ms = new System.IO.MemoryStream();
            var binWriter = new System.IO.BinaryWriter(ms);
            // Header
            binWriter.Write((short)0);   // 0 : reserved
            binWriter.Write((short)1);   // 2 : 1=ico, 2=cur
            binWriter.Write((short)1);   // 4 : number of images
                                  // Image directory
            var w = img.Width;
            if (w >= 256) w = 0;
            binWriter.Write((byte)w);    // 0 : width of image
            var h = img.Height;
            if (h >= 256) h = 0;
            binWriter.Write((byte)h);    // 1 : height of image
            binWriter.Write((byte)0);    // 2 : number of colors in palette
            binWriter.Write((byte)0);    // 3 : reserved
            binWriter.Write((short)0);   // 4 : number of color planes
            binWriter.Write((short)0);   // 6 : bits per pixel
            var sizeHere = ms.Position;
            binWriter.Write((int)0);     // 8 : image size
            var start = (int)ms.Position + 4;
            binWriter.Write(start);      // 12: offset of image data
                                  // Image data
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var imageSize = (int)ms.Position - start;
            ms.Seek(sizeHere, System.IO.SeekOrigin.Begin);
            binWriter.Write(imageSize);
            ms.Seek(0, System.IO.SeekOrigin.Begin);

            // And load it
            return new Icon(ms);
        }
        /// <summary>
        /// Преобразовать рисунок в файл *.ico на диске.
        /// </summary>
        /// <param name="img">Исходный рисунок</param>
        /// <param name="file_path">Путь к результирующему файлу</param>
        /// <param name="size">Размер иконки</param>
        public static void SaveImageToIco(Image img, string file_path, int size)
        {
            Icon icon;
            using (var msImg = new MemoryStream())
            using (var msIco = new MemoryStream())
            {
                img.Save(msImg, ImageFormat.Png);
                using (var binWriter = new BinaryWriter(msIco))
                {
                    binWriter.Write((short)0);           //0-1 reserved
                    binWriter.Write((short)1);           //2-3 image type, 1 = icon, 2 = cursor
                    binWriter.Write((short)1);           //4-5 number of images
                    binWriter.Write((byte)size);         //6 image width
                    binWriter.Write((byte)size);         //7 image height
                    binWriter.Write((byte)0);            //8 number of colors
                    binWriter.Write((byte)0);            //9 reserved
                    binWriter.Write((short)0);           //10-11 color planes
                    binWriter.Write((short)32);          //12-13 bits per pixel
                    binWriter.Write((int)msImg.Length);  //14-17 size of image data
                    binWriter.Write(22);                 //18-21 offset of image data
                    binWriter.Write(msImg.ToArray());    // write image data
                    binWriter.Flush();
                    binWriter.Seek(0, SeekOrigin.Begin);
                    icon = new Icon(msIco);
                }
            }
            using (var fs = new FileStream(file_path, FileMode.Create, FileAccess.Write))
            {
                icon.Save(fs);
            }
        }
    }
}
