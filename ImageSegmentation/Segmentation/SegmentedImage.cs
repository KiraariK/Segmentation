using System.Collections.Generic;
using System.Drawing;

namespace ImageSegmentation.Segmentation
{
    public class SegmentedImage
    {
        public int Height { get; set; } // Высота изображения
        public int Width { get; set; } // Ширина изображения
        public List<Region> Regions { get; set; } // Список регионов изображения

        /// <summary>
        /// Конструктор класса создает начальное разбиение изображения на сегменты заданного размера
        /// </summary>
        /// <param name="rgbData">Матрица цветов пикселей, в каждой строке которой записаны компоненты r, g и b</param>
        /// <param name="imageHeight">Высота изображения</param>
        /// <param name="imageWidth">Ширина изображения</param>
        /// <param name="defaultSegmentSize">Начальный размер квадратного сегмента</param>
        public SegmentedImage(int[][] rgbData, int imageHeight, int imageWidth, int defaultSegmentSize)
        {
            Height = imageHeight;
            Width = imageWidth;
            Regions = new List<Region>();

            // создание начальных регионов размером defaultSegmentSize х defaultSegmentSize
            for (int i = 0; i < imageHeight; i += defaultSegmentSize)
            {
                for (int j = 0; j < imageWidth; j += defaultSegmentSize)
                {
                    int regionHeight = imageHeight - i < defaultSegmentSize ? imageHeight - i : defaultSegmentSize;
                    int regionWidth = imageWidth - j < defaultSegmentSize ? imageWidth - j : defaultSegmentSize;
                    Pixel[] pixels = new Pixel[regionHeight * regionWidth];
                    for (int x = 0; x < regionHeight; x++)
                    {
                        for (int y = 0; y < regionWidth; y++)
                        {
                            int[] pixelId = { i + x, j + y };
                            pixels[(x * regionWidth) + y] = new Pixel(pixelId, rgbData[((i + x) * imageWidth) + (j + y)], Width);
                        }
                    }
                    Region region = new Region(pixels);

                    for (int k = 0; k < region.RegionPixels.Count; k++)
                        region.RegionPixels[k].Region = region;

                    Regions.Add(region);
                }
            }
        }

        /// <summary>
        /// Усредняет значения цветовых компонент пикселя по региону и записывает в массив SegmentsRgbData пикселя, обновляет серый цвет
        /// </summary>
        public void AverageRegionPixelsColor()
        {
            // массив для хранения средних значений цветовых компонент по региону
            double[][] averageRegionsRGB = new double[Regions.Count][];
            for (int i = 0; i < Regions.Count; i++)
                averageRegionsRGB[i] = new double[3];

            // расчет средних значений цветовых компонент по региону
            for (int i = 0; i < Regions.Count; i++)
            {
                double[] sum = new double[3];
                for (int j = 0; j < Regions[i].RegionPixels.Count; j++)
                    for (int k = 0; k < Regions[i].RegionPixels[j].RgbData.Length; k++)
                        sum[k] += Regions[i].RegionPixels[j].RgbData[k];

                for (int j = 0; j < sum.Length; j++)
                {
                    sum[j] /= Regions[i].RegionPixels.Count;
                    averageRegionsRGB[i][j] = sum[j];
                }
            }

            // запись цветов пикселей, как средних значений цветовых компонент пикселей региона
            for (int i = 0; i < Regions.Count; i++)
            {
                for (int j = 0; j < Regions[i].RegionPixels.Count; j++)
                {
                    int colorSum = 0;
                    for (int k = 0; k < Regions[i].RegionPixels[j].SegmentsRgbData.Length; k++)
                    {
                        Regions[i].RegionPixels[j].SegmentsRgbData[k] = (int)averageRegionsRGB[i][k];
                        colorSum += (int)averageRegionsRGB[i][k];
                    }
                    // запись серых цветов для пикселей, основанных на измененных данных SegmentsRgbData
                    Regions[i].RegionPixels[j].SegmentsGrayColor = colorSum / 3;
                }
            }
        }

        /// <summary>
        /// Создает изображение из сегментов
        /// </summary>
        /// <returns>24-х битное изображение типа Bitmap</returns>
        public Bitmap GetBitmapFromSegments(bool highlightBorders = false)
        {
            int pixelCount = Height * Width;
            int[][] colorData = new int[pixelCount][];
            for (int i = 0; i < pixelCount; i++)
                colorData[i] = new int[3];
            if (!highlightBorders)
            {
                for (int i = 0; i < Regions.Count; i++)
                    for (int j = 0; j < Regions[i].RegionPixels.Count; j++)
                        colorData[Regions[i].RegionPixels[j].GlobalNumber] = Regions[i].RegionPixels[j].SegmentsRgbData;
            }
            else
            {
                for (int i = 0; i < Regions.Count; i++)
                {
                    for (int j = 0; j < Regions[i].RegionPixels.Count; j++)
                    {
                        if (Regions[i].RegionPixels[j].isNeighboring)
                            colorData[Regions[i].RegionPixels[j].GlobalNumber] = new int[] { 0, 0, 0 };
                        else
                            colorData[Regions[i].RegionPixels[j].GlobalNumber] = Regions[i].RegionPixels[j].SegmentsRgbData;
                    }
                }
            }

            return ImageProcessing.ExportImage(colorData, Height, Width);
        }
    }
}
