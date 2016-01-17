using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageSegmentation
{
    class ImageProcessing
    {
        /// <summary>
        /// Загружает 24-х битное изображение Bitmap в матрицу colorData
        /// </summary>
        /// <param name="bitmap">Объект изображения Bitmap</param>
        /// <param name="colorData">Матрица цветов изображения, где в каждой строке записаны значения r, g и b</param>
        /// <param name="imageHeight">Высота изображения</param>
        /// <param name="imageWidth">Ширина изображения</param>
        public static void LoadImage(Bitmap bitmap, ref int[][] colorData,
            ref int imageHeight, ref int imageWidth)
        {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(new Point(0, 0),
               bitmap.Size), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            imageHeight = bitmap.Height;
            imageWidth = bitmap.Width;
            int imageSize = imageHeight * imageWidth;

            colorData = new int[imageSize][];
            for (int i = 0; i < imageSize; i++)
                colorData[i] = new int[3];

            unsafe
            {
                int byteCounts = 3;
                for (int i = 0; i < bitmap.Height; i++)
                {
                    byte* OriginalRowPtr = (byte*)bitmapData.Scan0 +
                        i * bitmapData.Stride;
                    for (int j = 0; j < bitmap.Width; j++)
                    {
                        int ColorPosition = j * byteCounts;

                        byte b = OriginalRowPtr[ColorPosition];
                        byte g = OriginalRowPtr[ColorPosition + 1];
                        byte r = OriginalRowPtr[ColorPosition + 2];

                        colorData[i * imageWidth + j][0] = r;
                        colorData[i * imageWidth + j][1] = g;
                        colorData[i * imageWidth + j][2] = b;
                    }
                }
            }

            bitmap.UnlockBits(bitmapData);
        }

        /// <summary>
        /// Создает и возвращает изображение из массива цветов
        /// </summary>
        /// <param name="colorData">Матрица цветов пикселей, в каждой строке которой записаны компоненты r, g и b</param>
        /// <param name="imageHeight">Высота изображения</param>
        /// <param name="imageWidth">Ширина изображения</param>
        /// <returns></returns>
        public static Bitmap ExportImage(int[][] colorData, int imageHeight, int imageWidth)
        {
            Bitmap resultBitmap = new Bitmap(imageWidth, imageHeight, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte bytesCount = 3;
                // объявление данных, обновляемых в изображении
                BitmapData updatingData = resultBitmap.LockBits(new Rectangle(new Point(0, 0), resultBitmap.Size),
                    ImageLockMode.WriteOnly, resultBitmap.PixelFormat);
                for (int i = 0; i < imageHeight; i++)
                {
                    byte* bitmapRowPtr = (byte*)updatingData.Scan0 + i * updatingData.Stride;
                    for (int j = 0; j < imageWidth; j++)
                    {
                        int colorPosition = j * bytesCount;
                        bitmapRowPtr[colorPosition] = (byte)colorData[(i * imageWidth) + j][2]; // запись синего компонента
                        bitmapRowPtr[colorPosition + 1] = (byte)colorData[(i * imageWidth) + j][1]; // запись зеленого компонента
                        bitmapRowPtr[colorPosition + 2] = (byte)colorData[(i * imageWidth) + j][0]; // запись красного компонента
                    }
                }
                resultBitmap.UnlockBits(updatingData);
            }

            return resultBitmap;
        }

        /// <summary>
        /// Возвращает матрицу оттенков серых цветов,
        /// где в каждой строке стоят значения элементов серого цвета
        /// </summary>
        /// <param name="colorData">матрица, в которой в каждой строке стоят элементы r, g и b</param>
        /// <param name="imageHeight">высота матрицы изображения</param>
        /// <param name="imageWidth">ширина матрицы изображения</param>
        /// <returns></returns>
        public static int[][] getGrayScaleMatrix(int[][] colorData, int imageHeight, int imageWidth)
        {
            int[][] grayScaleMatrix = new int[imageHeight][];
            for (int i = 0; i < imageHeight; i++)
                grayScaleMatrix[i] = new int[imageWidth];
            for (int i = 0; i < imageHeight; i++)
                for (int j = 0; j < imageWidth; j++)
                {
                    int index = i * imageWidth + j;
                    grayScaleMatrix[i][j] = (int)((colorData[index][0] +
                        colorData[index][1] + colorData[index][2]) / 3);
                }
            return grayScaleMatrix;
        }

        /// <summary>
        /// Возвращает матрицу красных цветов,
        /// где в каждой строке стоят значения элементов красного цвета
        /// </summary>
        /// <param name="colorData">матрица, в которой в каждой строке стоят элементы r, g и b</param>
        /// <param name="imageHeight">высота матрицы изображения</param>
        /// <param name="imageWidth">ширина матрицы изображения</param>
        /// <returns></returns>
        public static int[][] getRedMatrix(int[][] colorData, int imageHeight, int imageWidth)
        {
            int[][] outMatrix = new int[imageHeight][];
            for (int i = 0; i < imageHeight; i++)
                outMatrix[i] = new int[imageWidth];
            for (int i = 0; i < imageHeight; i++)
                for (int j = 0; j < imageWidth; j++)
                {
                    int index = i * imageWidth + j;
                    outMatrix[i][j] = colorData[index][0];
                }
            return outMatrix;
        }

        /// <summary>
        /// Возврщает матрицу зеленых цветов,
        /// где в каждой строке стоят значения элементов зеленого цвета
        /// </summary>
        /// <param name="colorData">матрица, в которой в каждой строке стоят элементы r, g и b</param>
        /// <param name="imageHeight">высота матрицы изображения</param>
        /// <param name="imageWidth">ширина матрицы изображения</param>
        /// <returns></returns>
        public static int[][] getGreenMatrix(int[][] colorData, int imageHeight, int imageWidth)
        {
            int[][] outMatrix = new int[imageHeight][];
            for (int i = 0; i < imageHeight; i++)
                outMatrix[i] = new int[imageWidth];
            for (int i = 0; i < imageHeight; i++)
                for (int j = 0; j < imageWidth; j++)
                {
                    int index = i * imageWidth + j;
                    outMatrix[i][j] = colorData[index][1];
                }
            return outMatrix;
        }

        /// <summary>
        /// Возвращает матрицу синих цветов,
        /// где в каждой строке стоят значения элементов синего цвета
        /// </summary>
        /// <param name="colorData">матрица, в которой в каждой строке стоят элементы r, g и b</param>
        /// <param name="imageHeight">высота матрицы изображения</param>
        /// <param name="imageWidth">ширина матрицы изображения</param>
        /// <returns></returns>
        public static int[][] getBlueMatrix(int[][] colorData, int imageHeight, int imageWidth)
        {
            int[][] outMatrix = new int[imageHeight][];
            for (int i = 0; i < imageHeight; i++)
                outMatrix[i] = new int[imageWidth];
            for (int i = 0; i < imageHeight; i++)
                for (int j = 0; j < imageWidth; j++)
                {
                    int index = i * imageWidth + j;
                    outMatrix[i][j] = colorData[index][2];
                }
            return outMatrix;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static Bitmap EasyResizeImage(Bitmap imgToResize, Size size)
        {
            Bitmap bmp = new Bitmap(imgToResize, size);
            Bitmap targetBmp = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.Format24bppRgb);
            return targetBmp;
        }

        public static Image ResizeImageNew(Image image, int new_height, int new_width)
        {
            Bitmap new_image = new Bitmap(new_height, new_width);
            Graphics g = Graphics.FromImage((Image)new_image);
            g.InterpolationMode = InterpolationMode.High;
            g.DrawImage(image, 0, 0, new_width, new_height);
            return new_image;
        }
    }
}
