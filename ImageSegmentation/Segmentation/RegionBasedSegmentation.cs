using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSegmentation.Characterization;

namespace ImageSegmentation.Segmentation
{
    public class RegionBasedSegmentation
    {
        public static int defaultSegmentSize = 5; // размер начального квадратного сегмента по умолчанию
        public static int defaultSegmentsCount = 80; // максимальное количество начальных квадратных сегментов изображения
        public static double regularizationParameter = 1.0; // регуляризационный параметр для рассчета геометрической близости
        public static int requiredSegmentsCount = 4; // требуемое количество регионов

        public static SegmentedImage PerformSegmentation(Bitmap image)
        {
            int imageWidth = image.Width;
            int imageHeight = image.Height;
            int imageSize = imageWidth * imageHeight;
            int[][] imageColorData = new int[imageSize][];
            for (int i = 0; i < imageSize; i++)
                imageColorData[i] = new int[3];

            // заполнение массива цветов изображения - загрузка изображения
            ImageProcessing.LoadImage(image, ref imageColorData, ref imageHeight, ref imageWidth);

            // подбор начального размера сегмента, исходя из расчета максимум defaultSegmentsCount начальных сегментов на изображении
            int segmentSize = defaultSegmentSize < ((int)Math.Sqrt(imageHeight * imageWidth / ((double)defaultSegmentsCount))) ?
                ((int)Math.Ceiling(Math.Sqrt(imageHeight * imageWidth / ((double)defaultSegmentsCount)))) : defaultSegmentSize;

            // создание начального сегментированного изображения
            SegmentedImage segmentedImage = new SegmentedImage(imageColorData, imageHeight, imageWidth, segmentSize);

            // заполнение L*a*b данных для каждого пикселя сегмента
            FillSegmentedImageIntensityFeatures(imageColorData, ref segmentedImage);

            // заполние текстурных характеристик для каждого пикселя изображения
            FillSegmentedImageTextureFeatures(imageColorData, ref segmentedImage);

            // выполнение условной фильтрации и заполние характеристик интенсивности пикселей после фильтрации
            PerformConditionalIntencityFiltering(ref segmentedImage);

            // выполние рассчета всех параметров регионов после заполнения всех параметров пикселей
            for (int i = 0; i < segmentedImage.Regions.Count; i++)
                segmentedImage.Regions[i].CalculateParameters(segmentedImage.Distances);

            // классификация пикселей на основе KMCC алгоритма
            KMCCClassification(regularizationParameter, ref segmentedImage);

            // объединение полученных регионов
            RegionsClassification(regularizationParameter, requiredSegmentsCount, ref segmentedImage);

            // Подсчет разброса точек регионов для сегментированного изображения
            segmentedImage.CalculateDispersion(requiredSegmentsCount);

            return segmentedImage;
        }

        /// <summary>
        /// Заполняет пиксели сегментируемого изображения L*a*b данными
        /// </summary>
        /// <param name="rgbData">RGB-данные изображения в виде матрицы значений r, g и b</param>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        private static void FillSegmentedImageIntensityFeatures(int[][] rgbData, ref SegmentedImage segmentedImage)
        {
            double[][] labData = new double[segmentedImage.Height * segmentedImage.Width][];
            for (int i = 0; i < segmentedImage.Height * segmentedImage.Width; i++)
                labData[i] = new double[3];
            ColorSpacesProcessing.TransformRGBtoLab(rgbData, ref labData);
            for (int i = 0; i < segmentedImage.Regions.Count; i++)
            {
                for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                {
                    int pixelX = segmentedImage.Regions[i].RegionPixels[j].Id[0];
                    int pixelY = segmentedImage.Regions[i].RegionPixels[j].Id[1];
                    segmentedImage.Regions[i].RegionPixels[j].IntensityFeatures = labData[(pixelX * segmentedImage.Width) + pixelY];
                }
            }
        }

        /// <summary>
        /// Заполняет пиксели сегментируемого изображения данными текстурных характеристик
        /// </summary>
        /// <param name="rgbData">RGB-данные изображения в виде матрицы значений r, g и b</param>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        private static void FillSegmentedImageTextureFeatures(int[][] rgbData, ref SegmentedImage segmentedImage)
        {
            // матрица текстурных характеристик
            // строки - элементы входной матрицы, соответствующие пикселям изображения
            // столбцы - значения тексурных характеристик для каждого из цветов: (r g b) x TextureFeaturesProcessing.numOfFeatures
            double[][] textureFeatures = new double[segmentedImage.Height * segmentedImage.Width][];
            for (int i = 0; i < segmentedImage.Height * segmentedImage.Width; i++)
                textureFeatures[i] = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];

            int iteration = 0; // текущая итерация получения текстурных характеристик
            for (int i = 0; i < TextureFeaturesProcessing.colorsCount; i++)
            {
                int[][] colorData; // массив отдельных цветовых компонентов изображения
                switch (i)
                {
                    case 0:
                        colorData = ImageProcessing.getRedMatrix(rgbData, segmentedImage.Height, segmentedImage.Width);
                        break;
                    case 1:
                        colorData = ImageProcessing.getGreenMatrix(rgbData, segmentedImage.Height, segmentedImage.Width);
                        break;
                    default:
                        colorData = ImageProcessing.getBlueMatrix(rgbData, segmentedImage.Height, segmentedImage.Width);
                        break;
                }
                for (int j = 0; j < TextureFeaturesProcessing.numOfFeatures; j++)
                {
                    double[][] textureFeatureMatrix = TextureFeaturesProcessing.LavsFiltering(colorData, TextureFeaturesProcessing.filters[j],
                        segmentedImage.Height, segmentedImage.Width, TextureFeaturesProcessing.filterSize, TextureFeaturesProcessing.filterSize);

                    // заполнение элементов массива текстурных характеристик для полученных значений текстурных характеристик
                    for (int x = 0; x < segmentedImage.Height; x++)
                    {
                        for (int y = 0; y < segmentedImage.Width; y++)
                        {
                            textureFeatures[(x * segmentedImage.Width) + y][iteration] = textureFeatureMatrix[x][y];
                        }
                    }

                    iteration++;
                }
            }

            // запоминаем полученные текстурные характеристики в каждом пикселе
            for (int i = 0; i < segmentedImage.Regions.Count; i++)
                for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                    segmentedImage.Regions[i].RegionPixels[j].TextureFeatures = textureFeatures[segmentedImage.Regions[i].RegionPixels[j].GlobalNumber];
        }

        /// <summary>
        /// Производит условную фильтрацию L*a*b данных изображения и записывает эти данные в пиксели изображения
        /// </summary>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        public static void PerformConditionalIntencityFiltering(ref SegmentedImage segmentedImage)
        {
            // извлечение текстурных характеристик пикселей в один массив
            double[][] textureFeatures = new double[segmentedImage.Height * segmentedImage.Width][];
            for (int i = 0; i < segmentedImage.Height * segmentedImage.Width; i++)
                textureFeatures[i] = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];

            // извлечение характеристик интенсивности пикселей в один массив
            double[][] intencityFeatures = new double[segmentedImage.Height * segmentedImage.Width][];
            for (int i = 0; i < segmentedImage.Height * segmentedImage.Width; i++)
                intencityFeatures[i] = new double[segmentedImage.Regions[0].RegionPixels[0].IntensityFeatures.Length];

            for (int i = 0; i < segmentedImage.Regions.Count; i++)
            {
                // TODO: Извечь в массивы TextureFeatures и IntencityFeatures
                for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                {
                    textureFeatures[segmentedImage.Regions[i].RegionPixels[j].GlobalNumber] = segmentedImage.Regions[i].RegionPixels[j].TextureFeatures;
                    intencityFeatures[segmentedImage.Regions[i].RegionPixels[j].GlobalNumber] = segmentedImage.Regions[i].RegionPixels[j].IntensityFeatures;
                }
            }

            // получаем пороговое значение текстурных характеристик изображения
            double textureFeatureThreshold = ConditionalFiltering.GetTextureFeatureThreshold(textureFeatures,
                segmentedImage.Height, segmentedImage.Width);

            // условная фильтрация
            for (int i = 0; i < segmentedImage.Regions.Count; i++)
            {
                for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                {
                    // если норма вектора текстурной характеристики пикселя меньше, чем пороговое значение
                    if (ConditionalFiltering.VectorNorm(segmentedImage.Regions[i].RegionPixels[j].TextureFeatures) < textureFeatureThreshold)
                        segmentedImage.Regions[i].RegionPixels[j].ConditionalIntensityFeatures =
                            segmentedImage.Regions[i].RegionPixels[j].IntensityFeatures;
                    else
                    {
                        // считаем покомпонентную сумму векторов интенсивности для текущего региона
                        double[] intencitySum = new double[segmentedImage.Regions[0].RegionPixels[0].IntensityFeatures.Length];
                        for (int k = 0; k < segmentedImage.Regions[i].RegionPixels.Count; k++)
                            for (int z = 0; z < segmentedImage.Regions[i].RegionPixels[k].IntensityFeatures.Length; z++)
                                intencitySum[z] += segmentedImage.Regions[i].RegionPixels[k].IntensityFeatures[z];

                        // делим каджую сумму на количество пикселей в регионе
                        for (int z = 0; z < intencitySum.Length; z++)
                            intencitySum[z] /= segmentedImage.Regions[i].RegionPixels.Count;

                        segmentedImage.Regions[i].RegionPixels[j].ConditionalIntensityFeatures = intencitySum;
                    }
                }
            }
        }

        /// <summary>
        /// Выполняет сегментацию изображения по алгоритму KMCC
        /// </summary>
        /// <param name="regularizationParameter">Регуляризационный параметр ламбда, используемый при вычислении геометрического расстояния</param>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        private static void KMCCClassification(double regularizationParameter, ref SegmentedImage segmentedImage)
        {
            bool isNeedInteration = true;
            int iterationsCount = 1;
            while (isNeedInteration && iterationsCount > 0)
            {
                iterationsCount--;

                // настраиваем параметры перед очередной итерацией алгоритма
                isNeedInteration = false;
                for (int i = 0; i < segmentedImage.Regions.Count; i++)
                    for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                        segmentedImage.Regions[i].RegionPixels[j].IsChecked = false;

                // проходимся по всем пикселям изображения в двойном цикле по пикселям регионов
                for (int i = 0; i < segmentedImage.Regions.Count; i++)
                {
                    for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                    {
                        if (segmentedImage.Regions[i].RegionPixels[j].IsChecked)
                            continue; // пропускаем пиксели, которые уже были проверены на данной итерации

                        segmentedImage.Regions[i].RegionPixels[j].IsChecked = true; // помечаем пиксель, как проверенный на текущей итерации

                        // массив для хранения расстояний по KMCC между текущим пикселем и всеми регионами
                        double[] distances = new double[segmentedImage.Regions.Count];
                        for (int k = 0; k < segmentedImage.Regions.Count; k++)
                        {
                            // подсчет квадратов разностей элементов векторов условной интенсивности
                            double sum = 0.0;
                            for (int z = 0; z < segmentedImage.Regions[k].RegionPixels[0].ConditionalIntensityFeatures.Length; z++)
                            {
                                // находится квадрат разности элементов вектора условной интенсивности текущего пикселя и текущего региона
                                sum += (segmentedImage.Regions[i].RegionPixels[j].ConditionalIntensityFeatures[z] -
                                    segmentedImage.Regions[k].AverageConditionalIntensityFeature[z]) *
                                    (segmentedImage.Regions[i].RegionPixels[j].ConditionalIntensityFeatures[z] -
                                    segmentedImage.Regions[k].AverageConditionalIntensityFeature[z]);
                            }
                            // Евклидово расстояние между векторами условной интенсивности текущего пикселя и текущего региона прибавляется к общему расстоянию
                            distances[k] += Math.Sqrt(sum);

                            // подсчет квадратов разностей элементов векторов текстурных характеристик
                            sum = 0.0;
                            for (int z = 0; z < segmentedImage.Regions[k].RegionPixels[0].TextureFeatures.Length; z++)
                            {
                                // находится квадрат разности элементов вектора текстурной характеристики текущего пикселя и текущего региона
                                sum += (segmentedImage.Regions[i].RegionPixels[j].TextureFeatures[z] -
                                    segmentedImage.Regions[k].AverageTextureFeature[z]) *
                                    (segmentedImage.Regions[i].RegionPixels[j].TextureFeatures[z] -
                                    segmentedImage.Regions[k].AverageTextureFeature[z]);
                            }
                            // Евклидово расстояние между векторами текстурной характеристики текущего пикселя и текущего региона прибавляется к общему расстоянию
                            distances[k] += Math.Sqrt(sum);

                            // подсчет квадатов разностей элементов вектора геометрической позиции текущего пикселя и центра текущего региона
                            sum = 0.0;
                            for (int z = 0; z < segmentedImage.Regions[k].RegionPixels[0].Id.Length; z++)
                            {
                                // находится квадрат разности элементов вектора геометрической позиции текущего пикселя и центра текущего региона
                                sum += (segmentedImage.Regions[i].RegionPixels[j].Id[z] -
                                    segmentedImage.Regions[k].SpacialSenterId[z]) *
                                    (segmentedImage.Regions[i].RegionPixels[j].Id[z] -
                                    segmentedImage.Regions[k].SpacialSenterId[z]);
                            }
                            // подсчет средней площади для всех регионов
                            double averageArea = 0.0;
                            for (int z = 0; z < segmentedImage.Regions.Count; z++)
                                averageArea += segmentedImage.Regions[z].Area;
                            averageArea /= segmentedImage.Regions.Count;
                            // Евклидово расстояние между векторами геометрического положения текущего пикселя и центра текущего региона прибавляется к общему расстоянию
                            distances[k] += regularizationParameter * (averageArea / segmentedImage.Regions[k].Area) * Math.Sqrt(sum);
                        }

                        // определение минимального расстояния и перемещение пикселя
                        double minDistance = distances.Min();
                        for (int k = 0; k < distances.Length; k++)
                        {
                            if (distances[k] == minDistance)
                            {
                                if (k != i) // пиксель находится не в своем регионе
                                {
                                    // удаляем пиксель из текущего региона (регион i) и добавляем его в самый близкий для него регион (регион k)
                                    segmentedImage.Regions[k].AddPixelWithParametersRecalculation(
                                        segmentedImage.Regions[i].RemovePixelWithParametersRecalculation(
                                        segmentedImage.Regions[i].RegionPixels[j].Id, segmentedImage.Distances),
                                        segmentedImage.Distances);

                                    if (segmentedImage.Regions[i].RegionPixels.Count != 0) // если в регионе еще есть пиксели
                                    {
                                        if (j != segmentedImage.Regions[i].RegionPixels.Count) // если текущий удаленный пиксель не был последним рассматриваемым в регионе
                                        {
                                            // значит удаленный пиксель был внутри региона, в силу особенностей удаления элемента из List
                                            // чтобы не пропускать пиксели делаем:
                                            j--;
                                        }
                                    }

                                    isNeedInteration = true;
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Объединяет близкие по параметрам регионы для получения заданного количества регионов
        /// </summary>
        /// <param name="regularizationParameter">Регуляризационный параметр ламбда, используемый при вычислении геометрического расстояния</param>
        /// <param name="segmentsCount">Требуемое количество регионов</param>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        private static void RegionsClassification(double regularizationParameter, int segmentsCount, ref SegmentedImage segmentedImage)
        {
            // цикл объединения регионов продолжается до тех пор, пока не будет получено нужное количество регионов
            while (segmentedImage.Regions.Count > segmentsCount)
            {
                // проходимся по всем регионам
                for (int i = 0; i < segmentedImage.Regions.Count; i++)
                {
                    // Если нужное количество регионов уже получено
                    if (segmentedImage.Regions.Count <= segmentsCount)
                        break;

                    // массив для хранения расстояний между текущим регионом и всеми другими регионами
                    double[] distances = new double[segmentedImage.Regions.Count];
                    for (int j = 0; j < distances.Length; j++)
                        distances[j] = 0.0;

                    // сравниваем текущий регион со всеми остальными
                    for (int j = 0; j < segmentedImage.Regions.Count; j++)
                    {
                        if (i == j)
                            continue; // пропускаем сравнение региона самого с собой

                        // подсчет квадратов разностей элементов векторов условной интенсивности
                        double sum = 0.0;
                        for (int k = 0; k < segmentedImage.Regions[j].AverageConditionalIntensityFeature.Length; k++)
                        {
                            sum += (segmentedImage.Regions[i].AverageConditionalIntensityFeature[k] -
                                segmentedImage.Regions[j].AverageConditionalIntensityFeature[k]) *
                                (segmentedImage.Regions[i].AverageConditionalIntensityFeature[k] -
                                segmentedImage.Regions[j].AverageConditionalIntensityFeature[k]);
                        }
                        distances[j] += Math.Sqrt(sum);

                        // подсчет квадратов разностей элементов векторов текстурных характеристик
                        sum = 0.0;
                        for (int k = 0; k < segmentedImage.Regions[j].AverageTextureFeature.Length; k++)
                        {
                            sum += (segmentedImage.Regions[i].AverageTextureFeature[k] -
                                segmentedImage.Regions[j].AverageTextureFeature[k]) *
                                (segmentedImage.Regions[i].AverageTextureFeature[k] -
                                segmentedImage.Regions[j].AverageTextureFeature[k]);
                        }
                        distances[j] += Math.Sqrt(sum);

                        // подсчет квадатов разностей элементов вектора геометрической позиции сравниваемых регионов
                        sum = 0.0;
                        for (int k = 0; k < segmentedImage.Regions[j].SpacialSenterId.Length; k++)
                        {
                            sum += (segmentedImage.Regions[i].SpacialSenterId[k] -
                                segmentedImage.Regions[j].SpacialSenterId[k]) *
                                (segmentedImage.Regions[i].SpacialSenterId[k] -
                                segmentedImage.Regions[j].SpacialSenterId[k]);
                        }
                        // подсчет средней площади для всех регионов
                        double averageArea = 0.0;
                        for (int k = 0; k < segmentedImage.Regions.Count; k++)
                            averageArea += segmentedImage.Regions[k].Area;
                        averageArea /= segmentedImage.Regions.Count;
                        distances[j] += regularizationParameter * (averageArea / segmentedImage.Regions[j].Area) * Math.Sqrt(sum);
                    }

                    // определение минимального расстояния и перемещение пикселей регионов
                    double minDistance = distances[0] != 0.0 ? distances[0] : distances[1];
                    for (int j = 0; j < distances.Length; j++)
                        if (distances[j] != 0.0 && distances[j] < minDistance) // исключаем сравнение региона с самим собой
                            minDistance = distances[j];
                    for (int j = 0; j < distances.Length; j++)
                    {
                        if (distances[j] == minDistance)
                        {
                            // перемещаем пиксели из j-го региона в i-ый регион
                            segmentedImage.Regions[i].AddPixelsWithParametersRecalculation(
                                segmentedImage.Regions[j].RemovePixels(), segmentedImage.Distances);

                            // удаляем пустой регион
                            segmentedImage.Regions.RemoveAt(j);

                            // если удаленные регион был до i-го региона в списке регионов, то индексы сместились, и,
                            // чтобы не пропускать регионы, делаем:
                            if (j < i)
                                i--;

                            break;
                        }
                    }
                }
            }
        }
    }
}
