using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation.Segmentation
{
    public class SegmentedImage
    {
        public int Height { get; set; } // Высота изображения
        public int Width { get; set; } // Ширина изображения
        public List<Region> Regions { get; set; } // Список регионов изображения
        public double Dispersion { get; set; } // Величина разброса точек регионов для сегментируемого изображения

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
                            pixels[(x * regionWidth) + y] = new Pixel(pixelId, rgbData[((i + x) * imageWidth) + (j + y)], Width, Height);
                        }
                    }
                    Region region = new Region(Width, pixels);
                    Regions.Add(region);
                }
            }

            // Заполнение массивов расстояний до пикселей изображения (для каждого пикселя)
            // Получение массива всех пикселей
            Pixel[] allPixels = new Pixel[Width * Height];
            int iterator = 0;
            for (int i = 0; i < Regions.Count; i++)
            {
                for (int j = 0; j < Regions[i].RegionPixels.Count; j++)
                {
                    allPixels[iterator] = Regions[i].RegionPixels[j];
                    iterator++;
                }
            }

            for (int i = 0; i < allPixels.Length; i++)
            {
                for (int j = 0; j < allPixels.Length; j++)
                {
                    if (i == j)
                        continue;

                    // Считаем расстояние от пикселя i до пикселя j
                    double distance = Math.Sqrt((allPixels[i].Id[0] - allPixels[j].Id[0]) * (allPixels[i].Id[0] - allPixels[j].Id[0]) +
                        (allPixels[i].Id[1] - allPixels[j].Id[1]) * (allPixels[i].Id[1] - allPixels[j].Id[1]));

                    // Записываем на соответствующее место найденное расстояние в массив расстояний пикселя i
                    allPixels[i].Distances[allPixels[j].GlobalNumber] = distance;
                }
            }
            // После этого в пикселях регионов будут заполнены массивы Distances, т.к. мы изменяли указатели
        }

        /// <summary>
        /// Усредняет значения цветовых компонент пикселя по региону
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
                for (int j = 0; j < Regions[i].RegionPixels.Count; j++)
                    for (int k = 0; k < Regions[i].RegionPixels[j].RgbData.Length; k++)
                        Regions[i].RegionPixels[j].RgbData[k] = (int)averageRegionsRGB[i][k];
        }

        /// <summary>
        /// Создает изображение из сегментов
        /// </summary>
        /// <returns>24-х битное изображение типа Bitmap</returns>
        public Bitmap GetBitmapFromSegments()
        {
            int pixelCount = Height * Width;
            int[][] colorData = new int[pixelCount][];
            for (int i = 0; i < pixelCount; i++)
                colorData[i] = new int[3];

            for (int i = 0; i < Regions.Count; i++)
                for (int j = 0; j < Regions[i].RegionPixels.Count; j++)
                    colorData[Regions[i].RegionPixels[j].GlobalNumber] = Regions[i].RegionPixels[j].RgbData;

            return ImageProcessing.ExportImage(colorData, Height, Width);
        }
        
        /// <summary>
        /// Вычисляет величину разброса точек регионов сегментируемого изображения
        /// </summary>
        /// <param name="requiredSegmentsCount">Количество регионов</param>
        public void CalculateDispersion(double requiredSegmentsCount)
        {
            for (int i = 0; i < Regions.Count; i++)
            {
                Regions[i].CalculateDispersion();
                Dispersion += Regions[i].Dispersion;
            }
            Dispersion /= requiredSegmentsCount;
            //Dispersion = Math.Sqrt(Width * Width + Height * Height) / Dispersion;
        }
    }
}
