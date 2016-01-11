﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSegmentation.Characterization;

namespace ImageSegmentation.Segmentation
{
    public class Region
    {
        public List<Pixel> RegionPixels { get; set; } // Список пикселей данного региона
        public int[] SpacialSenterId { get; set; } // Идентификатор пикслея, являющегося центром региона
        public int Area { get; set; } // Пложадь региона
        public double[] AverageTextureFeature { get; set; } // Среднее значение текстурных характеристик региона
        public double[] AverageConditionalIntensityFeature { get; set; } // Среднее значение интенсивности региона, полученного после условной фильтрации
        public double Dispersion { get; set; } // Величина разброса точек региона

        public Region(Pixel[] pixels)
        {
            SpacialSenterId = new int[2];
            RegionPixels = new List<Pixel>();
            for (int i = 0; i < pixels.Length; i++)
                RegionPixels.Add(pixels[i]);

            Area = 0;
            AverageTextureFeature = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];
            AverageConditionalIntensityFeature = new double[3];

            Dispersion = 0.0;
        }

        /// <summary>
        /// Пересчитывает центральный пиксель региона
        /// </summary>
        private void CalculateCenter()
        {
            if (RegionPixels == null)
                return;

            // Извлекаем матрицу идентификаторов пикселей - в каждой строке находится идентификатор (координаты x и y пикселя)
            int[][] pixelIds = new int[RegionPixels.Count][];
            for (int i = 0; i < RegionPixels.Count; i++)
                pixelIds[i] = RegionPixels[i].Id;

            // Ищем суммы расстояний от каждой точки до каждой точки региона
            double[] sums = new double[RegionPixels.Count];
            for (int i = 0; i < RegionPixels.Count; i++)
            {
                // Считаем сумму расстояний от точки i до всех остальных точек
                for (int j = 0; j < RegionPixels.Count; j++)
                {
                    if (i != j)
                    {
                        // Прибавляем к сумме расстояние от точки i до точки j
                        sums[i] += Math.Sqrt((pixelIds[i][0] - pixelIds[j][0]) * (pixelIds[i][0] - pixelIds[j][0]) +
                            (pixelIds[i][1] - pixelIds[j][1]) * (pixelIds[i][1] - pixelIds[j][1]));
                    }
                }
            }

            // Определение центрального пикселя региона
            double minSum = sums.Min();
            for (int i = 0; i < sums.Length; i++)
            {
                if (sums[i] == minSum)
                {
                    SpacialSenterId = pixelIds[i];
                    break;
                }
            }
        }

        /// <summary>
        /// Добавляет пиксель к региону без пересчета параметров региона
        /// </summary>
        /// <param name="pixel">Пиксель, добавляемый к региону</param>
        public void AddPixel(Pixel pixel)
        {
            RegionPixels.Add(pixel);
        }

        /// <summary>
        /// Добавляет пиксель к региону и пересчитывает параметры региона
        /// </summary>
        /// <param name="pixel">Пиксель, добавляемый к региону</param>
        public void AddPixelWithParametersRecalculation(Pixel pixel)
        {
            RegionPixels.Add(pixel);
            CalculateParameters();
        }

        /// <summary>
        /// Добавляет массив пикселей к региону без пересчета параметров региона
        /// </summary>
        /// <param name="pixels">Добавляемый к региону массив пикселей</param>
        public void AddPixels(Pixel[] pixels)
        {
            RegionPixels.AddRange(pixels);                
        }


        /// <summary>
        /// Добавляет массив пикселей к региону и пересчитывает параметры региона
        /// </summary>
        /// <param name="pixels">Добавляемый к региону массив пикселей</param>
        public void AddPixelsWithParametersRecalculation(Pixel[] pixels)
        {
            RegionPixels.AddRange(pixels);
            CalculateParameters();
        }

        /// <summary>
        /// Удаляет пиксель из региона без пересчета параметров региона
        /// </summary>
        /// <param name="pixelId">Идентификатор удаляемого пикселя - массив с координатами i, j</param>
        public Pixel RemovePixel(int[] pixelId)
        {
            Pixel removedPixel = null;
            for (int i = 0; i < RegionPixels.Count; i++)
            {
                if (RegionPixels[i].Id[0] == pixelId[0] && RegionPixels[i].Id[1] == pixelId[1])
                {
                    removedPixel = RegionPixels[i];
                    RegionPixels.RemoveAt(i);
                    break;
                }
            }
            return removedPixel;
        }

        /// <summary>
        /// Удаляет пиксель из региона и пересчитывает параметры региона
        /// </summary>
        /// <param name="pixelId">Идентификатор удаляемого пикселя - массив с координатами i, j</param>
        public Pixel RemovePixelWithParametersRecalculation(int[] pixelId)
        {
            Pixel removedPixel = null;
            for (int i = 0; i < RegionPixels.Count; i++)
            {
                if (RegionPixels[i].Id[0] == pixelId[0] && RegionPixels[i].Id[1] == pixelId[1])
                {
                    removedPixel = RegionPixels[i];
                    RegionPixels.RemoveAt(i);
                    break;
                }
            }
            CalculateParameters();
            return removedPixel;
        }

        /// <summary>
        /// Удаляет все пиксели региона без пересчета параметров региона
        /// </summary>
        /// <returns>Массив удаленных пикселей региона</returns>
        public Pixel[] RemovePixels()
        {
            Pixel[] removedPixels = new Pixel[RegionPixels.Count];
            for (int i = 0; i < RegionPixels.Count; i++)
                removedPixels[i] = RegionPixels[i];

            RegionPixels.Clear();

            return removedPixels;
        }

        /// <summary>
        /// Удаляет все пиксели региона и пересчитывает праметры региона
        /// </summary>
        /// <returns>Массив удаленных пикселей региона</returns>
        public Pixel[] RemovePixelsWithParametersRecalculation()
        {
            Pixel[] removedPixels = new Pixel[RegionPixels.Count];
            for (int i = 0; i < RegionPixels.Count; i++)
                removedPixels[i] = RegionPixels[i];

            RegionPixels.Clear();
            CalculateParameters();

            return removedPixels;
        }

        /// <summary>
        /// Пересчитывает все параметры для региона на основе параметров пикселей региона
        /// </summary>
        public void CalculateParameters()
        {
            if (RegionPixels == null)
                return;

            // если в регионе не осталось пикселей, то параметры региона сбрасываются
            if (RegionPixels.Count == 0)
            {
                SpacialSenterId[0] = SpacialSenterId[1] = -1;
                Area = 0;
                for (int i = 0; i < AverageTextureFeature.Length; i++)
                    AverageTextureFeature[i] = 0;
                for (int i = 0; i < AverageConditionalIntensityFeature.Length; i++)
                    AverageConditionalIntensityFeature[i] = 0;
                return;
            }

            // расчет центра региона
            CalculateCenter();

            // расчет площади региона
            Area = RegionPixels.Count;

            // расчет среднего значения вектора текстурной характеристики для региона
            for (int i = 0; i < RegionPixels[0].TextureFeatures.Length; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < RegionPixels.Count; j++)
                    sum += RegionPixels[j].TextureFeatures[i];
                AverageTextureFeature[i] = sum / (double)RegionPixels.Count;
            }

            // расчет среднего значения вектора интенсивности, полученного после условной фильтрации, для региона
            for (int i = 0; i < RegionPixels[0].ConditionalIntensityFeatures.Length; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < RegionPixels.Count; j++)
                    sum += RegionPixels[j].ConditionalIntensityFeatures[i];
                AverageConditionalIntensityFeature[i] = sum / RegionPixels.Count;
            }
        }

        /// <summary>
        /// Вычисляет величину разброса точек региона
        /// </summary>
        public void CalculateDispersion()
        {
            // Извлекаем матрицу идентификаторов пикселей - в каждой строке находится идентификатор (координаты x и y пикселя)
            int[][] pixelIds = new int[RegionPixels.Count][];
            for (int i = 0; i < RegionPixels.Count; i++)
                pixelIds[i] = RegionPixels[i].Id;

            // Ищем минимальные расстояния от каждой точки региона до каждой точки региона
            double[] minDistances = new double[RegionPixels.Count];
            for (int i = 0; i < RegionPixels.Count; i++)
            {
                double[] distances = new double[RegionPixels.Count];
                // Считаем сумму расстояний от точки i до всех остальных точек
                for (int j = 0; j < RegionPixels.Count; j++)
                {
                    if (i != j)
                    {
                        // Находим расстояние до каждой точки
                        distances[j] = Math.Sqrt((pixelIds[i][0] - pixelIds[j][0]) * (pixelIds[i][0] - pixelIds[j][0]) +
                            (pixelIds[i][1] - pixelIds[j][1]) * (pixelIds[i][1] - pixelIds[j][1]));
                    }
                    else
                        distances[j] = double.MaxValue;
                }
                // Если не нашелся хотя бы один соседний пиксель
                if (!distances.Contains(1.0))
                    Dispersion += distances.Min();
            }
        }
    }
}