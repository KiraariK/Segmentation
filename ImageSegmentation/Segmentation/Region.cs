using System;
using System.Collections.Generic;
using System.Linq;
using ImageSegmentation.Characterization;

namespace ImageSegmentation.Segmentation
{
    public class Region
    {
        public enum PixelAction { none, remove, add } // Перечисление, необходимое для обозначения действия над пикселями

        public List<Pixel> RegionPixels { get; set; } // Список пикселей данного региона
        public List<double> DistanceSums { get; set; } // Список сумм расстояний от пикселя до всех остальных пикселей региона
        public List<Region> Neighbors { get; set; } // Список индексов соседних регионов
        public int[] SpacialSenterId { get; set; } // Идентификатор пикслея, являющегося центром региона
        public int Area { get; set; } // Пложадь региона
        public double[] AverageTextureFeature { get; set; } // Среднее значение текстурных характеристик региона
        public double[] TextureFeatureSums { get; set; } // Суммы текстурных характеристик по каждому пикселю региона
        public double[] AverageConditionalIntensityFeature { get; set; } // Среднее значение интенсивности региона, полученного после условной фильтрации
        public double[] ConditionalIntensityFeatureSums { get; set; } // Суммы условных характеристик по каждому пикселю региона
        public double Dispersion { get; set; } // Величина разброса точек региона

        public Region()
        {
            SpacialSenterId = new int[2];
            RegionPixels = new List<Pixel>();
            Neighbors = new List<Region>();

            Area = 0;
            AverageTextureFeature = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];
            TextureFeatureSums = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];
            AverageConditionalIntensityFeature = new double[3];
            ConditionalIntensityFeatureSums = new double[3];

            Dispersion = 0.0;
        }

        public Region(Pixel[] pixels)
        {
            SpacialSenterId = new int[2];
            RegionPixels = new List<Pixel>();
            Neighbors = new List<Region>();
            for (int i = 0; i < pixels.Length; i++)
                RegionPixels.Add(pixels[i]);

            Area = 0;
            AverageTextureFeature = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];
            TextureFeatureSums = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];
            AverageConditionalIntensityFeature = new double[3];
            ConditionalIntensityFeatureSums = new double[3];

            Dispersion = 0.0;
        }

        /// <summary>
        /// Пересчитывает центральный пиксель региона
        /// </summary>
        /// <param name="action">Действие выполненной над пикселями региона перед пересчетом центрального пикселя</param>
        /// <param name="pixels">Массив пикселей, который были изменены в регионе перед пересчетом центрального пикселя</param>
        private void CalculateCenter(PixelAction action = PixelAction.none, Pixel[] pixels = null)
        {
            if (RegionPixels == null)
                return;

            // Извлекаем матрицу идентификаторов пикселей - в каждой строке находится идентификатор (координаты x и y пикселя)
            int[][] pixelIds = new int[RegionPixels.Count][];
            for (int i = 0; i < RegionPixels.Count; i++)
                pixelIds[i] = RegionPixels[i].Id;

            if (action == PixelAction.none)
            {
                // Инициализация массива сумм расстояний
                DistanceSums = new List<double>();
                for (int i = 0; i < RegionPixels.Count; i++)
                    DistanceSums.Add(0.0);

                // Ищем суммы расстояний от каждой точки до каждой точки региона
                for (int i = 0; i < RegionPixels.Count; i++)
                {
                    // Считаем сумму расстояний от точки i до всех остальных точек
                    for (int j = 0; j < RegionPixels.Count; j++)
                    {
                        if (i != j)
                        {
                            // Прибавляем к сумме расстояние от пикселя i до пикселя j
                            DistanceSums[i] += Math.Sqrt((pixelIds[i][0] - pixelIds[j][0]) * (pixelIds[i][0] - pixelIds[j][0]) +
                                (pixelIds[i][1] - pixelIds[j][1]) * (pixelIds[i][1] - pixelIds[j][1]));
                        }
                    }
                }
            }
            else if (action == PixelAction.add)
            {
                if (pixels == null)
                    return;

                // Добавляем новые элементы списка расстояний для новых пикселей и добавляем к уже существующим суммам новые расстояния
                int oldDistancesSumsCount = DistanceSums.Count; // Фиксируем количество пикселей, которые были в регионе до добавления
                for (int i = 0; i < pixels.Length; i++)
                {
                    int oldDistancesIterator = 0;
                    double sum = 0.0;
                    for (int j = 0; j < RegionPixels.Count; j++)
                    {
                        double distance = Math.Sqrt((pixels[i].Id[0] - pixelIds[j][0]) * (pixels[i].Id[0] - pixelIds[j][0]) +
                            (pixels[i].Id[1] - pixelIds[j][1]) * (pixels[i].Id[1] - pixelIds[j][1]));

                        sum += distance;

                        // Если текущий пиксель относится еще к старым пикселям региона
                        if (oldDistancesIterator < oldDistancesSumsCount)
                            DistanceSums[j] += distance;

                        // Увеличиваем счетчик просмотренных пикселей
                        oldDistancesIterator++;
                    }

                    DistanceSums.Add(sum);
                }
            }
            else if (action == PixelAction.remove)
            {
                // В случае удаления пикселей, они удалены из списка пикселей и из списка расстояний уже до текущего момента

                if (pixels == null)
                    return;

                // Уменьшаем суммы расстояний оставшихся пикселей на величины расстояний до удаленных пикселей
                for (int i = 0; i < pixels.Length; i++)
                    for (int j = 0; j < RegionPixels.Count; j++)
                        DistanceSums[j] -= Math.Sqrt((pixels[i].Id[0] - pixelIds[j][0]) * (pixels[i].Id[0] - pixelIds[j][0]) +
                            (pixels[i].Id[1] - pixelIds[j][1]) * (pixels[i].Id[1] - pixelIds[j][1]));
            }
            else
                return;

            // Определение центрального пикселя региона
            double minSum = DistanceSums.Min();
            for (int i = 0; i < DistanceSums.Count; i++)
            {
                if (DistanceSums[i] == minSum)
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
            CalculateParameters(PixelAction.add, new Pixel[] { pixel });
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
            CalculateParameters(PixelAction.add, pixels);
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
                    // удаляем также и из списка расстояний
                    DistanceSums.RemoveAt(i);
                    break;
                }
            }
            CalculateParameters(PixelAction.remove, new Pixel[] { removedPixel });
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
            // удаляем также список расстояний
            DistanceSums.Clear();

            return removedPixels;
        }

        /// <summary>
        /// Пересчитывает все параметры для региона на основе параметров пикселей региона
        /// </summary>
        /// <param name="action">Действие, которое было произведено над пикселями перед вызовом функции</param>
        /// <param name="pixels">Массив пикселей, над которыми было произведено действие</param>
        public void CalculateParameters(PixelAction action = PixelAction.none, Pixel[] pixels = null)
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

            if (action == PixelAction.none)
            {
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
                    TextureFeatureSums[i] = sum;
                    AverageTextureFeature[i] = sum / RegionPixels.Count;
                }

                // расчет среднего значения вектора интенсивности, полученного после условной фильтрации, для региона
                for (int i = 0; i < RegionPixels[0].ConditionalIntensityFeatures.Length; i++)
                {
                    double sum = 0.0;
                    for (int j = 0; j < RegionPixels.Count; j++)
                        sum += RegionPixels[j].ConditionalIntensityFeatures[i];
                    ConditionalIntensityFeatureSums[i] = sum;
                    AverageConditionalIntensityFeature[i] = sum / RegionPixels.Count;
                }
            }
            else
            {
                if (pixels == null)
                    return;

                // расчет центра региона
                CalculateCenter(action, pixels);

                // расчет площади региона
                Area = RegionPixels.Count;

                // расчет среднего значения вектора текстурной характеристики для региона
                if (action == PixelAction.add)
                {
                    for (int i = 0; i < pixels.Length; i++)
                        for (int j = 0; j < RegionPixels[0].TextureFeatures.Length; j++)
                            TextureFeatureSums[j] += pixels[i].TextureFeatures[j];

                    for (int i = 0; i < RegionPixels[0].TextureFeatures.Length; i++)
                        AverageTextureFeature[i] = TextureFeatureSums[i] / RegionPixels.Count;
                }
                if (action == PixelAction.remove)
                {
                    for (int i = 0; i < pixels.Length; i++)
                        for (int j = 0; j < RegionPixels[0].TextureFeatures.Length; j++)
                            TextureFeatureSums[j] -= pixels[i].TextureFeatures[j];

                    for (int i = 0; i < RegionPixels[0].TextureFeatures.Length; i++)
                        AverageTextureFeature[i] = TextureFeatureSums[i] / RegionPixels.Count;
                }

                // расчет среднего значения вектора интенсивности, полученного после условной фильтрации, для региона
                if (action == PixelAction.add)
                {
                    for (int i = 0; i < pixels.Length; i++)
                        for (int j = 0; j < RegionPixels[0].ConditionalIntensityFeatures.Length; j++)
                            ConditionalIntensityFeatureSums[j] += pixels[i].ConditionalIntensityFeatures[j];

                    for (int i = 0; i < RegionPixels[0].ConditionalIntensityFeatures.Length; i++)
                        AverageConditionalIntensityFeature[i] = ConditionalIntensityFeatureSums[i] / RegionPixels.Count;
                }
                if (action == PixelAction.remove)
                {
                    for (int i = 0; i < pixels.Length; i++)
                        for (int j = 0; j < RegionPixels[0].ConditionalIntensityFeatures.Length; j++)
                            ConditionalIntensityFeatureSums[j] -= pixels[i].ConditionalIntensityFeatures[j];

                    for (int i = 0; i < RegionPixels[0].ConditionalIntensityFeatures.Length; i++)
                        AverageConditionalIntensityFeature[i] = ConditionalIntensityFeatureSums[i] / RegionPixels.Count;
                }
            }
        }

        /// <summary>
        /// Проверяет, есть ли в данном регионе пиксель с заданным Id
        /// </summary>
        /// <param name="pixelId">Id пикселя</param>
        /// <returns>true, если пиксель есть в регионе, false, если его нет</returns>
        public bool isPixelInRegion(int[] pixelId)
        {
            var pixel = RegionPixels.Find(x => x.Id[0] == pixelId[0] && x.Id[1] == pixelId[1]);
            if (pixel != null)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Проверяет, находится ли пиксель с заданным Id в окрестностях пикселей данного региона. Только для отсортированного массива пикселей!
        /// </summary>
        /// <param name="pixelId">Id пикселя из отсортированного по строками изображения массива пикселей изображения</param>
        /// <param name="imageHeight">Высота исходного изображения</param>
        /// <param name="imageWidth">Ширина исходного изображения</param>
        /// <returns>true, если пиксель в окрестности, false, если пикселя нет в окрестности</returns>
        public bool isPixelInNeighborhood(int[] pixelId,  int imageHeight, int imageWidth)
        {
            bool isFound = false;

            // проверяем принадлежгность данному региону для пикселей из окрестности текущего пикселя
            for (int x = pixelId[0] - 1; x <= pixelId[0] + 1; x++)
            {
                // Если нашли регион, выходим
                if (isFound)
                    break;

                for (int y = pixelId[1] - 1; y <= pixelId[1] + 1; y++)
                {
                    // Если нашли регион, выходим
                    if (isFound)
                        break;

                    // пропускаем пиксель, если такого нет
                    if (x < 0 || x > imageHeight || y < 0 || y > imageWidth)
                        continue;

                    // пропускаем сравнение пикселя с самим собой
                    if (x == pixelId[0] && y == pixelId[1])
                        continue;

                    // т.к. данная функция применяется для отсортированного массива пикселей, то лучше проходить с конца
                    for (int i = RegionPixels.Count - 1; i >= 0; i--)
                    {
                        // т.к. мы работаем с отсортированным массивом пикселей, то имеет смысл проверять максимум только imageWidth последних
                        if (RegionPixels.Count - 1 - i > imageWidth)
                            break;

                        if (isPixelInRegion(new int[] { x, y }))
                        {
                            isFound = true;
                            break;
                        }
                    }
                }
            }

            return isFound;
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
