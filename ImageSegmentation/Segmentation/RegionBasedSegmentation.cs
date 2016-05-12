using System;
using System.Drawing;
using System.Linq;
using ImageSegmentation.Characterization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ImageSegmentation.Segmentation
{
    public class RegionBasedSegmentation
    {
        public static int defaultSegmentSize = 5; // размер начального квадратного сегмента по умолчанию
        public static int defaultSegmentsCount = 400; // максимальное количество начальных квадратных сегментов изображения
        public static double regularizationParameter = 1.0; // регуляризационный параметр для рассчета геометрической близости
        public static int requiredSegmentsCount = 13; // требуемое количество регионов
        public static double lowThresholdForRegionSize = 0.01; // минимальный размер сегмента в долях от размера изображения

        public static SegmentedImage PerformSegmentation(Bitmap image)
        {
            int imageWidth = image.Width;
            int imageHeight = image.Height;
            int imageSize = imageWidth * imageHeight;
            int[][] imageColorData = new int[imageSize][];
            for (int i = 0; i < imageSize; i++)
                imageColorData[i] = new int[3];

            //Logger logger = new Logger("../../Log/log_performance.txt");
            //logger.WriteLog("Начало алгоритма сегментации\r\n");
            //DateTime start = DateTime.Now;

            //logger.WriteLog("Загрузка изображения: ");
            //DateTime loadImageTime = DateTime.Now;
            // заполнение массива цветов изображения - загрузка изображения
            ImageProcessing.LoadImage(image, ref imageColorData, ref imageHeight, ref imageWidth);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - loadImageTime));

            // подбор начального размера сегмента, исходя из расчета максимум defaultSegmentsCount начальных сегментов на изображении
            int segmentSize = defaultSegmentSize < ((int)Math.Sqrt(imageHeight * imageWidth / ((double)defaultSegmentsCount))) ?
                ((int)Math.Ceiling(Math.Sqrt(imageHeight * imageWidth / ((double)defaultSegmentsCount)))) : defaultSegmentSize;

            //logger.WriteLog("Создание начального сегментированного изображения: ");
            //DateTime defaultSegmentsTime = DateTime.Now;
            // создание начального сегментированного изображения
            SegmentedImage segmentedImage = new SegmentedImage(imageColorData, imageHeight, imageWidth, segmentSize);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - defaultSegmentsTime));

            //logger.WriteLog("Получение и заполнение Lab-данных: ");
            //DateTime labDataTime = DateTime.Now;
            // заполнение L*a*b данных для каждого пикселя сегмента
            FillSegmentedImageIntensityFeatures(imageColorData, ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - labDataTime));

            //logger.WriteLog("Получение и заполнение текстурных характеристик: ");
            //DateTime textureFeaturesTime = DateTime.Now;
            // заполние текстурных характеристик для каждого пикселя изображения
            FillSegmentedImageTextureFeatures(imageColorData, ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - textureFeaturesTime));

            //logger.WriteLog("Выполнение условной фильтрации: ");
            //DateTime conditionalFilteringTime = DateTime.Now;
            // выполнение условной фильтрации и заполние характеристик интенсивности пикселей после фильтрации
            PerformConditionalIntencityFiltering(ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - conditionalFilteringTime));

            //logger.WriteLog("Выполнение перерасчета параметров регионов: ");
            //DateTime calcRegionParamsTime1 = DateTime.Now;
            // выполние рассчета всех параметров регионов после заполнения всех параметров пикселей
            Parallel.For(0, segmentedImage.Regions.Count, i =>
            {
                segmentedImage.Regions[i].CalculateParameters();
            });
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - calcRegionParamsTime1));

            // Раскоментрпорвать только при использовании альтернативного метода KMCCClassification
            //logger.WriteLog("Первое определение соседства регионов: ");
            //DateTime firstNeighborsTime = DateTime.Now;
            //// определение соседства регионов
            //CalculateNeighborhood(ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - firstNeighborsTime));

            //logger.WriteLog("Выполнение этапа миграции пикселей: ");
            //DateTime pixelMigrationTime = DateTime.Now;
            // классификация пикселей на основе KMCC алгоритма
            KMCCClassification(regularizationParameter, ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - pixelMigrationTime));

            //logger.WriteLog("Выполнение этапа объединения регионов: ");
            //DateTime regionsMergingTime = DateTime.Now;
            // объединение полученных регионов
            RegionsClassification(regularizationParameter, requiredSegmentsCount, ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - regionsMergingTime));

            // постобработка результатов сегментации

            //logger.WriteLog("Пространственное разделение регионов: ");
            //DateTime regionsSplitingTime = DateTime.Now;
            // отделение пространственно разделенных частей регионов
            SplitRegions(ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - regionsSplitingTime));

            //logger.WriteLog("Выполнение перерасчета параметров регионов: ");
            //DateTime calcRegionParamsTime2 = DateTime.Now;
            // Пересчитываем все параметры регионов, т.к. количество регионов изменилось
            Parallel.For(0, segmentedImage.Regions.Count, i =>
            {
                segmentedImage.Regions[i].CalculateParameters();
            });
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - calcRegionParamsTime2));

            //logger.WriteLog("Второе определение соседства регионов: ");
            //DateTime SecondNeighborsTime = DateTime.Now;
            // определение соседства регионов
            CalculateNeighborhood(ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - SecondNeighborsTime));

            //logger.WriteLog("Удаление маленьких регионов: ");
            //DateTime removingSmallRegions = DateTime.Now;
            // сливаем маленькие регионы с соседними регионами
            RemoveSmallRegions(ref segmentedImage);
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - removingSmallRegions));

            // Подсчет разброса точек регионов для сегментированного изображения
            //segmentedImage.CalculateDispersion(requiredSegmentsCount);

            //logger.WriteLog("Завершение сегментации, все время сегментации: ");
            //logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - start));

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
        private static void KMCCClassification(double regularizationParameter, ref SegmentedImage smg)
        {
            SegmentedImage segmentedImage = smg;
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
                        Parallel.For(0, segmentedImage.Regions.Count, k =>
                        {
                        //for (int k = 0; k < segmentedImage.Regions.Count; k++)
                        //{

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
                        //}
                        });

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
                                        segmentedImage.Regions[i].RegionPixels[j].Id));

                                    if (segmentedImage.Regions[i].RegionPixels.Count != 0) // если в регионе еще есть пиксели
                                    {
                                        if (j != segmentedImage.Regions[i].RegionPixels.Count - 1) // если текущий удаленный пиксель не был последним рассматриваемым в регионе
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
            smg = segmentedImage;
        }

        //private static void KMCCClassification(double regularizationParameter, ref SegmentedImage segmentedImage)
        //{
        //    // Составляем топографически ориентированный массив пикселей
        //    Pixel[][] imagePixels = new Pixel[segmentedImage.Height][];
        //    for (int i = 0; i < segmentedImage.Height; i++)
        //        imagePixels[i] = new Pixel[segmentedImage.Width];

        //    for (int i = 0; i < segmentedImage.Regions.Count; i++)
        //    {
        //        for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
        //        {
        //            Pixel currentPixel = segmentedImage.Regions[i].RegionPixels[j];
        //            int x = currentPixel.Id[0];
        //            int y = currentPixel.Id[1];

        //            imagePixels[x][y] = currentPixel;
        //        }
        //    }

        //    bool isNeedInteration = true;
        //    int iterationsCount = 1;
        //    while (isNeedInteration && iterationsCount > 0)
        //    {
        //        iterationsCount--;

        //        // настраиваем параметры перед очередной итерацией алгоритма
        //        isNeedInteration = false;
        //        for (int i = 0; i < segmentedImage.Regions.Count; i++)
        //            for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
        //                segmentedImage.Regions[i].RegionPixels[j].IsChecked = false;

        //        //проходимся по всем пикселям изображения в двойном цикле по пикселям регионов
        //        for (int i = 0; i < segmentedImage.Regions.Count; i++)
        //        {
        //            for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
        //            {
        //                Pixel currentPixel = segmentedImage.Regions[i].RegionPixels[j];

        //                if (currentPixel.IsChecked)
        //                    continue; // пропускаем пиксели, которые уже были проверены на данной итерации

        //                currentPixel.IsChecked = true;

        //                // подсчет средней площади для всех регионов
        //                double averageArea = 0.0;
        //                for (int z = 0; z < segmentedImage.Regions.Count; z++)
        //                    averageArea += segmentedImage.Regions[z].Area;
        //                averageArea /= segmentedImage.Regions.Count;

        //                // нахождение расстояния пикселя до региона, в котором он находится сейчас
        //                double selfDistance = 0.0;
        //                // подсчет квадратов разностей элементов векторов условной интенсивности
        //                double selfSum = 0.0;
        //                for (int z = 0; z < currentPixel.ConditionalIntensityFeatures.Length; z++)
        //                {
        //                    // находится квадрат разности элементов вектора условной интенсивности текущего пикселя и текущего региона
        //                    selfSum += (currentPixel.ConditionalIntensityFeatures[z] -
        //                        currentPixel.Region.AverageConditionalIntensityFeature[z]) *
        //                        (currentPixel.ConditionalIntensityFeatures[z] -
        //                        currentPixel.Region.AverageConditionalIntensityFeature[z]);
        //                }
        //                // Евклидово расстояние между векторами условной интенсивности текущего пикселя и текущего региона прибавляется к общему расстоянию
        //                selfDistance += Math.Sqrt(selfSum);

        //                // подсчет квадратов разностей элементов векторов текстурных характеристик
        //                selfSum = 0.0;
        //                for (int z = 0; z < currentPixel.TextureFeatures.Length; z++)
        //                {
        //                    // находится квадрат разности элементов вектора текстурной характеристики текущего пикселя и текущего региона
        //                    selfSum += (currentPixel.TextureFeatures[z] -
        //                        currentPixel.Region.AverageTextureFeature[z]) *
        //                        (currentPixel.TextureFeatures[z] -
        //                        currentPixel.Region.AverageTextureFeature[z]);
        //                }
        //                // Евклидово расстояние между векторами текстурной характеристики текущего пикселя и текущего региона прибавляется к общему расстоянию
        //                selfDistance += Math.Sqrt(selfSum);

        //                // подсчет квадатов разностей элементов вектора геометрической позиции текущего пикселя и центра текущего региона
        //                selfSum = 0.0;
        //                for (int z = 0; z < currentPixel.Id.Length; z++)
        //                {
        //                    // находится квадрат разности элементов вектора геометрической позиции текущего пикселя и центра текущего региона
        //                    selfSum += (currentPixel.Id[z] - currentPixel.Region.SpacialSenterId[z]) *
        //                        (currentPixel.Id[z] - currentPixel.Region.SpacialSenterId[z]);
        //                }
        //                // Евклидово расстояние между векторами геометрического положения текущего пикселя и центра текущего региона прибавляется к общему расстоянию
        //                selfDistance += regularizationParameter * (averageArea / currentPixel.Region.Area) * Math.Sqrt(selfSum);

        //                // массив для хранения расстояний от текущего пикселя до соседних регионов
        //                double[] distances = new double[currentPixel.Region.Neighbors.Count];
        //                // Нахождение расстояния от текущего пикселя до соседних регионов
        //                for (int k = 0; k < currentPixel.Region.Neighbors.Count; k++)
        //                {
        //                    Region currentRegion = currentPixel.Region.Neighbors[k];

        //                    // подсчет квадратов разностей элементов векторов условной интенсивности
        //                    double sum = 0.0;
        //                    for (int z = 0; z < currentPixel.ConditionalIntensityFeatures.Length; z++)
        //                    {
        //                        // находится квадрат разности элементов вектора условной интенсивности текущего пикселя и текущего региона
        //                        sum += (currentPixel.ConditionalIntensityFeatures[z] -
        //                            currentRegion.AverageConditionalIntensityFeature[z]) *
        //                            (currentPixel.ConditionalIntensityFeatures[z] -
        //                            currentRegion.AverageConditionalIntensityFeature[z]);
        //                    }
        //                    // Евклидово расстояние между векторами условной интенсивности текущего пикселя и текущего региона прибавляется к общему расстоянию
        //                    distances[k] += Math.Sqrt(sum);

        //                    // подсчет квадратов разностей элементов векторов текстурных характеристик
        //                    sum = 0.0;
        //                    for (int z = 0; z < currentPixel.TextureFeatures.Length; z++)
        //                    {
        //                        // находится квадрат разности элементов вектора текстурной характеристики текущего пикселя и текущего региона
        //                        sum += (currentPixel.TextureFeatures[z] -
        //                            currentRegion.AverageTextureFeature[z]) *
        //                            (currentPixel.TextureFeatures[z] -
        //                            currentRegion.AverageTextureFeature[z]);
        //                    }
        //                    // Евклидово расстояние между векторами текстурной характеристики текущего пикселя и текущего региона прибавляется к общему расстоянию
        //                    distances[k] += Math.Sqrt(sum);

        //                    // подсчет квадатов разностей элементов вектора геометрической позиции текущего пикселя и центра текущего региона
        //                    sum = 0.0;
        //                    for (int z = 0; z < currentPixel.Id.Length; z++)
        //                    {
        //                        // находится квадрат разности элементов вектора геометрической позиции текущего пикселя и центра текущего региона
        //                        sum += (currentPixel.Id[z] - currentRegion.SpacialSenterId[z]) *
        //                            (currentPixel.Id[z] - currentRegion.SpacialSenterId[z]);
        //                    }
        //                    // Евклидово расстояние между векторами геометрического положения текущего пикселя и центра текущего региона прибавляется к общему расстоянию
        //                    distances[k] += regularizationParameter * (averageArea / currentRegion.Area) * Math.Sqrt(sum);
        //                }

        //                // определение минимального расстояния и перемещение пикселя
        //                double minDistance = distances.Min();
        //                // Выполняем миграцию пикселя тогда, когда до соседнего региона расстояние меньше, чем до собственного
        //                if (minDistance < selfDistance)
        //                {
        //                    for (int k = 0; k < distances.Length; k++)
        //                    {
        //                        if (distances[k] == minDistance)
        //                        {
        //                            // удаляем пиксель из текущего региона и добавляем его в самый близкий для него соседний регион (регион k)
        //                            currentPixel.Region.Neighbors[k].AddPixelWithParametersRecalculation(
        //                                currentPixel.Region.RemovePixelWithParametersRecalculation(currentPixel.Id));

        //                            List<Region> alreadyCheckedRegions = new List<Region>(); // Регионы, все соседи которого перепроверены
        //                            // рассматриваем 8-ми связную окрестность перемещенного пикселя
        //                            for (int x = currentPixel.Id[0] - 1; x <= currentPixel.Id[0] + 1; x++)
        //                            {
        //                                for (int y = currentPixel.Id[1] - 1; y <= currentPixel.Id[1] + 1; y++)
        //                                {
        //                                    // пропускаем пиксель, если такого нет
        //                                    if (x < 0 || x >= segmentedImage.Height || y < 0 || y >= segmentedImage.Width)
        //                                        continue;

        //                                    Pixel firstNeighboringPixel = imagePixels[x][y];

        //                                    // для каждого пикселя из окрестности проводим определение соседства регионов
        //                                    // для каждого из них также рассматриваем 8-ми связную окрестность
        //                                    bool isNeedCheck = false;
        //                                    for (int xx = x - 1; xx <= x + 1; xx++)
        //                                    {
        //                                        for (int yy = y - 1; yy <= y + 1; yy++)
        //                                        {
        //                                            // пропускаем пиксель, если такого нет
        //                                            if (xx < 0 || xx >= segmentedImage.Height || yy < 0 || yy >= segmentedImage.Width)
        //                                                continue;

        //                                            Pixel secondNeighboringPixel = imagePixels[xx][yy];

        //                                            if (firstNeighboringPixel.Region != secondNeighboringPixel.Region)
        //                                            {
        //                                                firstNeighboringPixel.isNeighboring = true;

        //                                                if (firstNeighboringPixel.Region.Neighbors.IndexOf(secondNeighboringPixel.Region) == -1)
        //                                                    firstNeighboringPixel.Region.Neighbors.Add(secondNeighboringPixel.Region);

        //                                                if (secondNeighboringPixel.Region.Neighbors.IndexOf(firstNeighboringPixel.Region) == -1)
        //                                                    secondNeighboringPixel.Region.Neighbors.Add(firstNeighboringPixel.Region);
        //                                            }
        //                                            else
        //                                                // TODO: Возможно, среди регионов firstNeighboringPixel.Region есть регион, с которым firstNeighboringPixel.Region уже не граничит
        //                                                isNeedCheck = true;
        //                                        }
        //                                    }

        //                                    // Если хотя бы один соседний пиксель принадлежит тому же региону и этот регион еще не проверялся, то нужно проверить соседство с соседями
        //                                    if (isNeedCheck && (alreadyCheckedRegions.IndexOf(firstNeighboringPixel.Region) == -1))
        //                                    {
        //                                        List<Region> checkRegions = new List<Region>();
        //                                        for (int z = 0; z < firstNeighboringPixel.Region.RegionPixels.Count; z++)
        //                                        {
        //                                            int pixelX = firstNeighboringPixel.Region.RegionPixels[z].Id[0];
        //                                            int pixelY = firstNeighboringPixel.Region.RegionPixels[z].Id[1];

        //                                            // Рассматриваем 8-ми связную окрестность пикселя
        //                                            for (int xx = pixelX - 1; xx <= pixelX + 1; xx++)
        //                                            {
        //                                                for (int yy = pixelY - 1; yy <= pixelY + 1; yy++)
        //                                                {
        //                                                    // пропускаем пиксель, если такого нет
        //                                                    if (xx < 0 || xx >= segmentedImage.Height || yy < 0 || yy >= segmentedImage.Width)
        //                                                        continue;

        //                                                    // Если в списке соседей такой регион есть
        //                                                    if (firstNeighboringPixel.Region.Neighbors.IndexOf(imagePixels[xx][yy].Region) != -1)
        //                                                    {
        //                                                        // Если в контрольном списке соседей такого региона нет
        //                                                        if (checkRegions.IndexOf(imagePixels[xx][yy].Region) == -1)
        //                                                            checkRegions.Add(imagePixels[xx][yy].Region);
        //                                                    }
        //                                                }
        //                                            }
        //                                        }

        //                                        // Если какой-то регион физическо исчез из соседей, но в списке остался,
        //                                        // то он будет присутствовать в списке соседей, но его не будет в контрольном списке соседей
        //                                        for (int z = 0; z < firstNeighboringPixel.Region.Neighbors.Count; z++)
        //                                        {
        //                                            if (checkRegions.IndexOf(firstNeighboringPixel.Region.Neighbors[z]) == -1)
        //                                                firstNeighboringPixel.Region.Neighbors.RemoveAt(z);
        //                                        }

        //                                        alreadyCheckedRegions.Add(firstNeighboringPixel.Region);
        //                                    }
        //                                }
        //                            }

        //                            if (segmentedImage.Regions[i].RegionPixels.Count != 0) // если в регионе еще есть пиксели
        //                            {
        //                                if (j != segmentedImage.Regions[i].RegionPixels.Count - 1) // если текущий удаленный пиксель не был последним рассматриваемым в регионе
        //                                {
        //                                    // значит удаленный пиксель был внутри региона, в силу особенностей удаления элемента из List
        //                                    // чтобы не пропускать пиксели делаем:
        //                                    j--;
        //                                }
        //                            }

        //                            isNeedInteration = true;
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Объединяет близкие по параметрам регионы для получения заданного количества регионов
        /// </summary>
        /// <param name="regularizationParameter">Регуляризационный параметр ламбда, используемый при вычислении геометрического расстояния</param>
        /// <param name="segmentsCount">Требуемое количество регионов</param>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        private static void RegionsClassification(double regularizationParameter, int segmentsCount, ref SegmentedImage smg)
        {
            SegmentedImage segmentedImage = smg;
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
                    //for (int j = 0; j < segmentedImage.Regions.Count; j++)
                    Parallel.For(0, segmentedImage.Regions.Count, j =>
                    {
                        //if (i == j)
                        //    continue; // пропускаем сравнение региона самого с собой

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
                    });

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
                                segmentedImage.Regions[j].RemovePixels());

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
                smg = segmentedImage;
            }
        }

        /// <summary>
        /// Делит регионы по пространственному признаку (если регион состоит из не соединенных фрагментов) на несколько регионов
        /// </summary>
        /// <param name="segmentedImage">Сегментированное изображение</param>
        public static void SplitRegions(ref SegmentedImage segmentedImage)
        {
            // Производим расчет усредненных по региону цветов пикселей
            segmentedImage.AverageRegionPixelsColor();

            // Составляем топографически ориентированную карту цветов пикселей, определяющую принадлежность пикселя к региону
            int[][] classMap = new int[segmentedImage.Height][];
            for (int i = 0; i < segmentedImage.Height; i++)
                classMap[i] = new int[segmentedImage.Width];

            for (int i = 0; i < segmentedImage.Regions.Count; i++)
                for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                    classMap[segmentedImage.Regions[i].RegionPixels[j].Id[0]][segmentedImage.Regions[i].RegionPixels[j].Id[1]] =
                        segmentedImage.Regions[i].RegionPixels[j].SegmentsGrayColor;

            // Составляем топографически ориентированный массив пикселей
            Pixel[][] imagePixels = new Pixel[segmentedImage.Height][];
            for (int i = 0; i < segmentedImage.Height; i++)
                imagePixels[i] = new Pixel[segmentedImage.Width];

            for (int i = 0; i < segmentedImage.Regions.Count; i++)
            {
                for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                {
                    Pixel currentPixel = segmentedImage.Regions[i].RegionPixels[j];
                    int x = currentPixel.Id[0];
                    int y = currentPixel.Id[1];

                    imagePixels[x][y] = new Pixel(currentPixel.Id, currentPixel.RgbData, segmentedImage.Width);
                    imagePixels[x][y].GlobalNumber = currentPixel.GlobalNumber;
                    imagePixels[x][y].Region = null; // необходимо для операции разделения регионов
                    imagePixels[x][y].SegmentsRgbData = currentPixel.SegmentsRgbData;
                    imagePixels[x][y].SegmentsGrayColor = currentPixel.SegmentsGrayColor;
                    imagePixels[x][y].IntensityFeatures = currentPixel.IntensityFeatures;
                    imagePixels[x][y].ConditionalIntensityFeatures = currentPixel.ConditionalIntensityFeatures;
                    imagePixels[x][y].TextureFeatures = currentPixel.TextureFeatures;
                    imagePixels[x][y].isNeighboring = currentPixel.isNeighboring;
                    imagePixels[x][y].Type = Pixel.PixelType.inner;
                    if ((x == 0) || (x == segmentedImage.Height - 1))
                        imagePixels[x][y].Type = Pixel.PixelType.border;
                    if ((y == 0) || (y == segmentedImage.Width - 1))
                        imagePixels[x][y].Type = Pixel.PixelType.border;
                }
            }
            List<Region> tempRegions = new List<Region>();

            // фомирование новых регионов
            for (int i = 0; i < segmentedImage.Height; i++)
            {
                for (int j = 0; j < segmentedImage.Width; j++)
                {
                    if (imagePixels[i][j].Region == null)
                    {
                        Region region = new Region();
                        tempRegions.Add(region);
                        region.RegionPixels.Add(imagePixels[i][j]);
                        imagePixels[i][j].Region = region;
                    }

                    // рассматриваем 8-ми связную окресность текущего пикселя
                    for (int fi = -1; fi <= 1; fi++)
                    {
                        for (int fj = -1; fj <= 1; fj++)
                        {
                            if ((fi + i >= 0) && (fi + i < segmentedImage.Height) &&
                                (fj + j >= 0) && (fj + j < segmentedImage.Width))
                            {
                                int indexX = fi + i;
                                int indexY = fj + j;
                                Pixel neighbourPixel = imagePixels[indexX][indexY];

                                if (classMap[i][j] != classMap[indexX][indexY])
                                    imagePixels[i][j].Type = Pixel.PixelType.border;

                                if (neighbourPixel.Region == null)
                                {
                                    if (classMap[i][j] == classMap[indexX][indexY])
                                    {
                                        imagePixels[indexX][indexY].Region = imagePixels[i][j].Region;
                                        imagePixels[i][j].Region.RegionPixels.Add(imagePixels[indexX][indexY]);
                                    }
                                }
                                else if (neighbourPixel.Region != imagePixels[i][j].Region)
                                {
                                    if (classMap[i][j] == classMap[indexX][indexY])
                                    {
                                        // Перемещаем пиксели из региона пикселя окрестности в регион текущего пикселя
                                        Region regionFROM = imagePixels[indexX][indexY].Region;
                                        Region regionTO = imagePixels[i][j].Region;
                                        for (int k = 0; k < regionFROM.RegionPixels.Count; k++)
                                        {
                                            Pixel pixel = regionFROM.RegionPixels[k];
                                            pixel.Region = regionTO;
                                            regionTO.RegionPixels.Add(pixel);
                                        }
                                        regionFROM.RegionPixels.Clear();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            List<Region> newRegions = new List<Region>();
            for (int i = 0; i < tempRegions.Count; i++)
            {
                if (tempRegions[i].RegionPixels.Count > 0)
                    newRegions.Add(tempRegions[i]);
            }

            segmentedImage.Regions = newRegions;
        }

        /// <summary>
        /// Заполняет списки id соседних регионов для каждого региона, помечает пиксели, как граничные или неграничные
        /// </summary>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        private static void CalculateNeighborhood(ref SegmentedImage segmentedImage)
        {
            // Составляем топографически ориентированный массив пикселей
            Pixel[][] imagePixels = new Pixel[segmentedImage.Height][];
            for (int i = 0; i < segmentedImage.Height; i++)
                imagePixels[i] = new Pixel[segmentedImage.Width];

            for (int i = 0; i < segmentedImage.Regions.Count; i++)
            {
                segmentedImage.Regions[i].Neighbors.Clear();
                for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                {
                    Pixel currentPixel = segmentedImage.Regions[i].RegionPixels[j];
                    currentPixel.isNeighboring = false;
                    int x = currentPixel.Id[0];
                    int y = currentPixel.Id[1];

                    imagePixels[x][y] = currentPixel;
                }
            }

            for (int i = 0; i < segmentedImage.Height; i++)
            {
                for (int j = 0; j < segmentedImage.Width; j++)
                {
                    Pixel currentPixel = imagePixels[i][j];
                    // Рассматриваем 8-ми связную окрестность текущего пикселя
                    for (int x = i - 1; x <= i + 1; x++)
                    {
                        for (int y = j - 1; y <= j + 1; y++)
                        {
                            // пропускаем пиксель, если такого нет
                            if (x < 0 || x >= segmentedImage.Height || y < 0 || y >= segmentedImage.Width)
                                continue;

                            Pixel neighborPixel = imagePixels[x][y];

                            // Если соседний пиксель относится к другому региону
                            if (currentPixel.Region != neighborPixel.Region)
                            {
                                currentPixel.isNeighboring = true;

                                // Если такой регион еще не занесен в списки соседних регионов текущего региона
                                if (currentPixel.Region.Neighbors.IndexOf(neighborPixel.Region) == -1)
                                    currentPixel.Region.Neighbors.Add(neighborPixel.Region);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Удаляет регионы, размером меньше, чем lowThresholdForRegionSize часть изображения путем слияния с наиболее близкими регионами
        /// </summary>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        public static void RemoveSmallRegions(ref SegmentedImage segmentedImage)
        {
            // сортируем регионы по количеству пикселей в них - по возрастанию
            segmentedImage.Regions.Sort(delegate (Region a, Region b)
            {
                return a.RegionPixels.Count.CompareTo(b.RegionPixels.Count);
            });

            int minSegmentSize = (int)Math.Round(lowThresholdForRegionSize * segmentedImage.Width * segmentedImage.Height);

            for (int i = 0; i < segmentedImage.Regions.Count; i++)
            {
                // выходим из цикла после обработки всех маленьких сегментов
                if (segmentedImage.Regions[i].RegionPixels.Count > minSegmentSize)
                    break;

                // массив для хранения расстояний между текущим регионом и его соседними регионами
                double[] distances = new double[segmentedImage.Regions[i].Neighbors.Count];
                for (int j = 0; j < distances.Length; j++)
                    distances[j] = 0.0;

                // сравниваем текущий регион со всеми соседними
                for (int j = 0; j < segmentedImage.Regions[i].Neighbors.Count; j++)
                //Parallel.For(0, segmentedImage.Regions[i].Neighbors.Count, j =>
                {
                    Region neighbor = segmentedImage.Regions[i].Neighbors[j];

                    double sum = 0.0;
                    // подсчет квадратов разностей элементов векторов условной интенсивности
                    for (int k = 0; k < neighbor.AverageConditionalIntensityFeature.Length; k++)
                    {
                        sum += (segmentedImage.Regions[i].AverageConditionalIntensityFeature[k] -
                            neighbor.AverageConditionalIntensityFeature[k]) *
                            (segmentedImage.Regions[i].AverageConditionalIntensityFeature[k] -
                            neighbor.AverageConditionalIntensityFeature[k]);
                    }
                    distances[j] += Math.Sqrt(sum);

                    // подсчет квадратов разностей элементов векторов текстурных характеристик
                    sum = 0.0;
                    for (int k = 0; k < neighbor.AverageTextureFeature.Length; k++)
                    {
                        sum += (segmentedImage.Regions[i].AverageTextureFeature[k] -
                            neighbor.AverageTextureFeature[k]) *
                            (segmentedImage.Regions[i].AverageTextureFeature[k] -
                            neighbor.AverageTextureFeature[k]);
                    }
                    distances[j] += Math.Sqrt(sum);

                    // подсчет квадатов разностей элементов вектора геометрической позиции сравниваемых регионов
                    sum = 0.0;
                    for (int k = 0; k < neighbor.SpacialSenterId.Length; k++)
                    {
                        sum += (segmentedImage.Regions[i].SpacialSenterId[k] -
                            neighbor.SpacialSenterId[k]) *
                            (segmentedImage.Regions[i].SpacialSenterId[k] -
                            neighbor.SpacialSenterId[k]);
                    }
                    // подсчет средней площади для всех оцениваемых регионов
                    double averageArea = segmentedImage.Regions[i].Area;
                    for (int k = 0; k < segmentedImage.Regions[i].Neighbors.Count; k++)
                        averageArea += segmentedImage.Regions[i].Neighbors[k].Area;
                    averageArea /= segmentedImage.Regions[i].Neighbors.Count + 1;
                    distances[j] += regularizationParameter * (averageArea / neighbor.Area) * Math.Sqrt(sum);
                }

                // определение минимального расстояния и перемещение пикселей регионов
                double minDistance = distances[0] != 0.0 ? distances[0] : distances[1];
                for (int j = 0; j < distances.Length; j++)
                    if (distances[j] != 0.0 && distances[j] < minDistance)
                        minDistance = distances[j];
                for (int j = 0; j < distances.Length; j++)
                {
                    if (distances[j] == minDistance)
                    {
                        // самый близкий к текущему маленькому региону поглощает текущий регион
                        segmentedImage.Regions[i].Neighbors[j].AddPixelsWithParametersRecalculation(
                            segmentedImage.Regions[i].RemovePixels());

                        // регион, который поглотил текущий регион, должен знать о своих новых соседях, доставшихся ему от старого региона
                        for (int k = 0; k < segmentedImage.Regions[i].Neighbors.Count; k++)
                        {
                            if (segmentedImage.Regions[i].Neighbors[j].Neighbors.IndexOf(segmentedImage.Regions[i].Neighbors[k]) == -1)
                                segmentedImage.Regions[i].Neighbors[j].Neighbors.Add(segmentedImage.Regions[i].Neighbors[k]);
                        }

                        // соседи текущего региона должны должны знать о новом регионе
                        for (int k = 0; k < segmentedImage.Regions[i].Neighbors.Count; k++)
                        {
                            // если среди соседей мы наткнулись на тот регион, который только что поглотил текущий регион, пропускаем его
                            if (segmentedImage.Regions[i].Neighbors[k] == segmentedImage.Regions[i].Neighbors[j])
                                continue;

                            // для всех остальных
                            // добавляем им в качестве соседа регион, который только что поглотил текущий регион
                            if (segmentedImage.Regions[i].Neighbors[k].Neighbors.IndexOf(segmentedImage.Regions[i].Neighbors[j]) == -1)
                                segmentedImage.Regions[i].Neighbors[k].Neighbors.Add(segmentedImage.Regions[i].Neighbors[j]);
                        }

                        // все соседи текущего поглощенного региона должны дружно забыть о нем, как о соседе
                        for (int k = 0; k < segmentedImage.Regions[i].Neighbors.Count; k++)
                        {
                            int oldRegionIndex = segmentedImage.Regions[i].Neighbors[k].Neighbors.IndexOf(segmentedImage.Regions[i]);
                            if (oldRegionIndex != -1)
                                segmentedImage.Regions[i].Neighbors[k].Neighbors.RemoveAt(oldRegionIndex);
                        }

                        // удаляем пустой текущий регион
                        segmentedImage.Regions.RemoveAt(i);

                        // поскольку мы текущий регион удалили, то, чтобы не пропускать следующие регионы, делаем
                        i--;

                        break;
                    }
                }
            }
        }
    }
}
