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

            Logger logger = new Logger("../../Log/log_performance.txt");
            logger.WriteLog("Начало алгоритма сегментации\r\n");
            DateTime start = DateTime.Now;

            logger.WriteLog("Загрузка изображения: ");
            DateTime loadImageTime = DateTime.Now;
            // заполнение массива цветов изображения - загрузка изображения
            ImageProcessing.LoadImage(image, ref imageColorData, ref imageHeight, ref imageWidth);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - loadImageTime));

            // подбор начального размера сегмента, исходя из расчета максимум defaultSegmentsCount начальных сегментов на изображении
            int segmentSize = defaultSegmentSize < ((int)Math.Sqrt(imageHeight * imageWidth / ((double)defaultSegmentsCount))) ?
                ((int)Math.Ceiling(Math.Sqrt(imageHeight * imageWidth / ((double)defaultSegmentsCount)))) : defaultSegmentSize;

            logger.WriteLog("Создание начального сегментированного изображения: ");
            DateTime defaultSegmentsTime = DateTime.Now;
            // создание начального сегментированного изображения
            SegmentedImage segmentedImage = new SegmentedImage(imageColorData, imageHeight, imageWidth, segmentSize);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - defaultSegmentsTime));

            logger.WriteLog("Получение и заполнение Lab-данных: ");
            DateTime labDataTime = DateTime.Now;
            // заполнение L*a*b данных для каждого пикселя сегмента
            FillSegmentedImageIntensityFeatures(imageColorData, ref segmentedImage);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - labDataTime));

            logger.WriteLog("Получение и заполнение текстурных характеристик: ");
            DateTime textureFeaturesTime = DateTime.Now;
            // заполние текстурных характеристик для каждого пикселя изображения
            FillSegmentedImageTextureFeatures(imageColorData, ref segmentedImage);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - textureFeaturesTime));

            logger.WriteLog("Выполнение условной фильтрации: ");
            DateTime conditionalFilteringTime = DateTime.Now;
            // выполнение условной фильтрации и заполние характеристик интенсивности пикселей после фильтрации
            PerformConditionalIntencityFiltering(ref segmentedImage);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - conditionalFilteringTime));

            logger.WriteLog("Выполнение перерасчета параметров регионов: ");
            DateTime calcRegionParamsTime1 = DateTime.Now;
            // выполние рассчета всех параметров регионов после заполнения всех параметров пикселей
            Parallel.For(0, segmentedImage.Regions.Count, i =>
            {
                segmentedImage.Regions[i].CalculateParameters();
            });
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - calcRegionParamsTime1));

            logger.WriteLog("Выполнение этапа миграции пикселей: ");
            DateTime pixelMigrationTime = DateTime.Now;
            // классификация пикселей на основе KMCC алгоритма
            KMCCClassification(regularizationParameter, ref segmentedImage);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - pixelMigrationTime));

            logger.WriteLog("Выполнение этапа объединения регионов: ");
            DateTime regionsMergingTime = DateTime.Now;
            // объединение полученных регионов
            RegionsClassification(regularizationParameter, requiredSegmentsCount, ref segmentedImage);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - regionsMergingTime));

            // постобработка результатов сегментации

            logger.WriteLog("Пространственное разделение регионов: ");
            DateTime regionsSplitingTime = DateTime.Now;
            // отделение пространственно разделенных частей регионов
            SplitRegions(ref segmentedImage);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - regionsSplitingTime));

            logger.WriteLog("Выполнение перерасчета параметров регионов: ");
            DateTime calcRegionParamsTime2 = DateTime.Now;
            // Пересчитываем все параметры регионов, т.к. количество регионов изменилось
            Parallel.For(0, segmentedImage.Regions.Count, i =>
            {
                segmentedImage.Regions[i].CalculateParameters();
            });
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - calcRegionParamsTime2));

            logger.WriteLog("Определение соседства регионов: ");
            DateTime neighborsTime = DateTime.Now;
            // определение соседства регионов
            CalculateNeighborhood(ref segmentedImage);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - neighborsTime));

            logger.WriteLog("Удаление маленьких регионов: ");
            DateTime removingSmallRegions = DateTime.Now;
            // сливаем маленькие регионы с соседними регионами
            RemoveSmallRegions(ref segmentedImage);
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - removingSmallRegions));

            // Подсчет разброса точек регионов для сегментированного изображения
            //segmentedImage.CalculateDispersion(requiredSegmentsCount);

            logger.WriteLog("Завершение сегментации, все время сегментации: ");
            logger.WriteLog(string.Format("{0}\r\n", DateTime.Now - start));

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
                            // TODO: пропускать те регионы, с которыми не соседствует регион, в котором в данный момент находится пиксель

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
                                        segmentedImage.Regions[i].RegionPixels[j].Id));

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
            List<Region> newSegmentedImageRegions = new List<Region>();

            int imageWidth = segmentedImage.Width;
            for (int i = 0; i < segmentedImage.Regions.Count; i++)
            {
                // производим сортировку пикселей региона по порядковому номеру пикселя, получаемому по id региона
                segmentedImage.Regions[i].RegionPixels.Sort(delegate (Pixel a, Pixel b)
                {
                    int numberA = (a.Id[0] * imageWidth) + a.Id[1];
                    int numberB = (b.Id[0] * imageWidth) + b.Id[1];
                    return numberA.CompareTo(numberB);
                });

                // объявляем новый список регионов, которые будут получены из текущего региона
                List<Region> newRegions = new List<Region>();
                List<string> neighbors = new List<string>(); // список соседства регионов, каждая строка 2 номера региона: "0 1"
                Region firstRegion = new Region();
                firstRegion.RegionPixels.Add(segmentedImage.Regions[i].RegionPixels[0]);
                newRegions.Add(firstRegion);
                for (int j = 1; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                {
                    bool isPartOfExistingRegion = false;
                    int indexOfRegion = -1;
                    int x = segmentedImage.Regions[i].RegionPixels[j].Id[0];
                    int y = segmentedImage.Regions[i].RegionPixels[j].Id[1];
                    if (x == 26 && y == 39)
                        indexOfRegion = 0;
                    for (int z = 0; z < newRegions.Count; z++)
                    {
                        // для проверки принадлжености пикселя окрестности региона используется упрощенная функция
                        // специально для отсортированного массива пикселей изображения
                        if (newRegions[z].isPixelInNeighborhood(new int[] { x, y }, segmentedImage.Height, segmentedImage.Width))
                        {
                            // если пиксель уже был отнесен к какому-то региону
                            if (isPartOfExistingRegion)
                            {
                                // значит пиксель принадлежит 2-м регионам одновременно - добавляем их к соседним
                                if (neighbors.IndexOf(indexOfRegion + " " + z) == -1)
                                    neighbors.Add(indexOfRegion + " " + z);
                                indexOfRegion = z;
                            }
                            else // если пиксель был определен, как сосед региона впервые
                            {
                                isPartOfExistingRegion = true;
                                indexOfRegion = z;
                                newRegions[z].RegionPixels.Add(segmentedImage.Regions[i].RegionPixels[j]);
                            }
                        }
                    }

                    // если пиксель не находится в окрестности ни одного из известных регионов
                    if (!isPartOfExistingRegion)
                    {
                        // создаем новые регион
                        Region region = new Region();
                        // добавляем пиксель к новому региону
                        region.RegionPixels.Add(segmentedImage.Regions[i].RegionPixels[j]);
                        // добавляем новый регион в список
                        newRegions.Add(region);
                    }
                    else // объединяем регионы, если нужно
                    {
                        for (int z = neighbors.Count - 1; z >= 0; z--)
                        {
                            string[] neighborsParts = neighbors[z].Split(' ');
                            int firstRegionIndex = int.Parse(neighborsParts[0]);
                            int secondRegionIndex = int.Parse(neighborsParts[1]);
                            // заполняем первый регион пикселями из второго
                            for (int p = 0; p < newRegions[secondRegionIndex].RegionPixels.Count; p++)
                                newRegions[firstRegionIndex].RegionPixels.Add(newRegions[secondRegionIndex].RegionPixels[p]);

                            newRegions[secondRegionIndex].RegionPixels.Clear();
                        }
                        neighbors.Clear();

                        for (int z = 0; z < newRegions.Count; z++)
                        {
                            if (newRegions[z].RegionPixels.Count == 0)
                            {
                                newRegions.RemoveAt(z);
                                z--;
                            }
                        }
                                
                    }
                }

                // добавляем новые регионы, полученные вместо i-го региона к новому списку регионов
                for (int j = 0; j < newRegions.Count; j++)
                {
                    if (newRegions[j].RegionPixels.Count != 0)
                        newSegmentedImageRegions.Add(newRegions[j]);
                }
            }

            // мы получили новый список регионов сегментированного изображения, но без средних для региона характеристик (их можно посчитать)
            // заменяем существующие регионы изображения новыми только что полученными
            segmentedImage.Regions.Clear();
            for (int i = 0; i < newSegmentedImageRegions.Count; i++)
                segmentedImage.Regions.Add(newSegmentedImageRegions[i]);
        }

        /// <summary>
        /// Заполняет списки id соседних регионов для каждого региона, помечает пиксели, как граничные или неграничные
        /// </summary>
        /// <param name="segmentedImage">Сегментируемое изображение</param>
        private static void CalculateNeighborhood(ref SegmentedImage segmentedImage)
        {
            for (int i = 0; i < segmentedImage.Regions.Count; i++)
            {
                for (int j = 0; j < segmentedImage.Regions[i].RegionPixels.Count; j++)
                {
                    int[] pixelId = segmentedImage.Regions[i].RegionPixels[j].Id;

                    bool isNeighboring = false;
                    // проверяем 8-ми связную окрестность пикселя
                    for (int x = pixelId[0] - 1; x <= pixelId[0] + 1; x++)
                    {
                        for (int y = pixelId[1] - 1; y <= pixelId[1] + 1; y++)
                        {
                            // пропускаем пиксель, если такого нет
                            if (x < 0 || x > segmentedImage.Height || y < 0 || y > segmentedImage.Width)
                                continue;

                            // пропускаем сравнение пикселя с самим собой
                            if (x == pixelId[0] && y == pixelId[1])
                                continue;

                            // если в текущем регионе пикселя нет
                            if (!segmentedImage.Regions[i].isPixelInRegion(new int[] { x, y }))
                            {
                                // говорим, что пиксель должен быть граничным
                                isNeighboring = true;

                                // ищем в других регионах
                                for (int z = 0; z < segmentedImage.Regions.Count; z++)
                                {
                                    // пропускаем текущий регион
                                    if (segmentedImage.Regions[z].SpacialSenterId == segmentedImage.Regions[i].SpacialSenterId)
                                        continue;

                                    // когда нашли регион, которму принадлежит пиксель
                                    if (segmentedImage.Regions[z].isPixelInRegion(new int[] { x, y }))
                                    {
                                        // Добавляем регион в список соседних регионов текущего региона
                                        if (segmentedImage.Regions[i].Neighbors.IndexOf(segmentedImage.Regions[z]) == -1)
                                            segmentedImage.Regions[i].Neighbors.Add(segmentedImage.Regions[z]);
                                    }
                                }
                            }
                        }
                    }

                    // помечаем пиксель, как граничный или неграничный
                    if (!isNeighboring)
                        segmentedImage.Regions[i].RegionPixels[j].isNeighboring = false;
                    else
                        segmentedImage.Regions[i].RegionPixels[j].isNeighboring = true;
                }
            }
        }

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
