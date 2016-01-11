using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation.Characterization
{
    public static class TextureFeaturesProcessing
    {
        public static int[][][] filters; // массив фильтров Лавса
        public static int numOfFeatures;
        public static int filterSize;
        public static int colorsCount;

        static TextureFeaturesProcessing()
        {
            numOfFeatures = 4;
            filterSize = 5;
            colorsCount = 3;
            filters = new int[numOfFeatures][][];
            for (int i = 0; i < numOfFeatures; i++)
            {
                filters[i] = new int[filterSize][];
                for (int j = 0; j < filterSize; j++)
                    filters[i][j] = new int[filterSize];
            }

            //L5L5
            filters[0][0][0] = 1;
            filters[0][0][1] = 4;
            filters[0][0][2] = 6;
            filters[0][0][3] = 4;
            filters[0][0][4] = 1;

            filters[0][1][0] = 4;
            filters[0][1][1] = 16;
            filters[0][1][2] = 24;
            filters[0][1][3] = 16;
            filters[0][1][4] = 4;

            filters[0][2][0] = 6;
            filters[0][2][1] = 24;
            filters[0][2][2] = 36;
            filters[0][2][3] = 24;
            filters[0][2][4] = 6;

            filters[0][3][0] = 4;
            filters[0][3][1] = 16;
            filters[0][3][2] = 24;
            filters[0][3][3] = 16;
            filters[0][3][4] = 4;

            filters[0][4][0] = 1;
            filters[0][4][1] = 4;
            filters[0][4][2] = 6;
            filters[0][4][3] = 4;
            filters[0][4][4] = 1;

            //L5R5
            filters[1][0][0] = 1;
            filters[1][0][1] = 4;
            filters[1][0][2] = 6;
            filters[1][0][3] = -4;
            filters[1][0][4] = 1;

            filters[1][1][0] = 4;
            filters[1][1][1] = 16;
            filters[1][1][2] = 24;
            filters[1][1][3] = -16;
            filters[1][1][4] = 4;

            filters[1][2][0] = 6;
            filters[1][2][1] = 24;
            filters[1][2][2] = 36;
            filters[1][2][3] = -24;
            filters[1][2][4] = 6;

            filters[1][3][0] = 4;
            filters[1][3][1] = 16;
            filters[1][3][2] = 24;
            filters[1][3][3] = -16;
            filters[1][3][4] = 4;

            filters[1][4][0] = 1;
            filters[1][4][1] = 4;
            filters[1][4][2] = 6;
            filters[1][4][3] = -4;
            filters[1][4][4] = 1;

            //W5L5
            filters[2][0][0] = 1;
            filters[2][0][1] = 4;
            filters[2][0][2] = 6;
            filters[2][0][3] = 4;
            filters[2][0][4] = 1;

            filters[2][1][0] = 4;
            filters[2][1][1] = 16;
            filters[2][1][2] = 24;
            filters[2][1][3] = 16;
            filters[2][1][4] = 4;

            filters[2][2][0] = 6;
            filters[2][2][1] = 24;
            filters[2][2][2] = 36;
            filters[2][2][3] = 24;
            filters[2][2][4] = 6;

            filters[2][3][0] = -4;
            filters[2][3][1] = -16;
            filters[2][3][2] = -24;
            filters[2][3][3] = -16;
            filters[2][3][4] = -4;

            filters[2][4][0] = 1;
            filters[2][4][1] = 4;
            filters[2][4][2] = 6;
            filters[2][4][3] = 4;
            filters[2][4][4] = 1;

            //W5W5
            filters[3][0][0] = 1;
            filters[3][0][1] = 4;
            filters[3][0][2] = 6;
            filters[3][0][3] = -4;
            filters[3][0][4] = 1;

            filters[3][1][0] = 4;
            filters[3][1][1] = 16;
            filters[3][1][2] = 24;
            filters[3][1][3] = -16;
            filters[3][1][4] = 4;

            filters[3][2][0] = 6;
            filters[3][2][1] = 24;
            filters[3][2][2] = 36;
            filters[3][2][3] = -24;
            filters[3][2][4] = 6;

            filters[3][3][0] = -4;
            filters[3][3][1] = -16;
            filters[3][3][2] = -24;
            filters[3][3][3] = -16;
            filters[3][3][4] = -4;

            filters[3][4][0] = 1;
            filters[3][4][1] = 4;
            filters[3][4][2] = 6;
            filters[3][4][3] = -4;
            filters[3][4][4] = 1;
        }

        /// <summary>
        /// Выполняет фильтрацию парямоугольной матрицы входных значений заданным фильтром Лавса
        /// </summary>
        /// <param name="inputMatrix">Исходная матрица входных значений для фильтрации</param>
        /// <param name="filteringMatrix">Матрица фильтра</param>
        /// <param name="matrixHeight">Высота матрицы входных значений</param>
        /// <param name="matrixWidth">Ширина матрицы входных значений</param>
        /// <param name="filterHeight">Высота матрицы фильтра</param>
        /// <param name="filterWidth">Ширина матрицы фильтра</param>
        /// <returns>Матрица текстурных характеристик</returns>
        public static double[][] LavsFiltering(int[][] inputMatrix, int[][] filteringMatrix, int matrixHeight, int matrixWidth,
            int filterHeight, int filterWidth)
        {
            double[][] filteredMatrix = new double[matrixHeight][];
            for (int i = 0; i < matrixHeight; i++)
                filteredMatrix[i] = new double[matrixWidth];

            int shiftX = (filterHeight - 1) / 2;
            int shiftY = (filterWidth - 1) / 2;

            int positionHeight = matrixHeight + matrixHeight + shiftY;
            int positionWidth = matrixWidth + matrixWidth + shiftX;

            int min = Int32.MaxValue;
            int max = Int32.MinValue;
            for (int i = 0; i < matrixHeight; i++)
            {
                for (int j = 0; j < matrixWidth; j++)
                {
                    int convolution = 0;
                    for (int fi = 0; fi < filterHeight; fi++)
                    {
                        for (int fj = 0; fj < filterWidth; fj++)
                        {
                            int initialYCoordinate = i + fi - shiftY;
                            int initialXCoordinate = j + fj - shiftX;
                            int xCoordinate = 0;
                            int yCoordinate = 0;

                            if (initialXCoordinate < 0)
                                xCoordinate = shiftX - j - fj - 1;
                            else if (initialXCoordinate < matrixWidth)
                                xCoordinate = initialXCoordinate;
                            else
                                xCoordinate = positionWidth - j - fj - 1;

                            if (initialYCoordinate < 0)
                                yCoordinate = shiftY - i - fi - 1;
                            else if (initialYCoordinate < matrixHeight)
                                yCoordinate = initialYCoordinate;
                            else
                                yCoordinate = positionHeight - i - fi - 1;

                            convolution += inputMatrix[yCoordinate][xCoordinate] * filteringMatrix[fj][fi];
                        }
                    }

                    // поиск максимального и минимального значения для последующей нормализации тексутрных характеристик
                    if (convolution < min)
                        min = convolution;
                    if (convolution > max)
                        max = convolution;

                    filteredMatrix[i][j] = convolution;
                }
            }

            // нормализация значений текстурных характеристик от 0 до 255
            //for (int i = 0; i < matrixHeight; i++)
            //    for (int j = 0; j < matrixWidth; j++)
            //        filteredMatrix[i][j] = (int)(((filteredMatrix[i][j] - min) / ((double)(max - min))) * 255.0);

            // нормализация значения текстурных характеристик от -1 до 1
            for (int i = 0; i < matrixHeight; i++)
                for (int j = 0; j < matrixWidth; j++)
                    filteredMatrix[i][j] = ((double)((2 * filteredMatrix[i][j]) - (max + min)) / ((double)(max - min)));

            return filteredMatrix;
        }

        /// <summary>
        /// Производит дискретное 2D вейвлет-преобразование с помощью вейвлета Хаара
        /// </summary>
        /// <param name="colorMatrix">Двухмерная матрица входных значений</param>
        /// <param name="imageHeight">Высота двухмерной матрицы (изображения)</param>
        /// <param name="imageWidth">Ширина двухмерной матрицы (изображения)</param>
        /// <param name="iterations">Количество уровенй преобразования</param>
        public static void Forward2DHaarTransformation(ref double[][] colorMatrix, int imageHeight, int imageWidth, int iterations)
        {
            int rows = imageHeight;
            int cols = imageWidth;

            double[] row;
            double[] col;

            for (int k = 0; k < iterations; k++)
            {
                int lev = 1 << k;
                int levCols = cols / lev;
                int levRows = rows / lev;

                row = new double[levCols];
                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = colorMatrix[i][j];

                    Forward1DHaarTransformation(ref row);

                    for (int j = 0; j < row.Length; j++)
                        colorMatrix[i][j] = row[j];
                }

                col = new double[levRows];
                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = colorMatrix[i][j];

                    Forward1DHaarTransformation(ref col);

                    for (int i = 0; i < col.Length; i++)
                        colorMatrix[i][j] = col[i];
                }
            }
        }

        /// <summary>
        /// Производит одномерное дискретное вейвлет-преобразование с помощью вейвлета Хаара
        /// </summary>
        /// <param name="data">Входной одномерный массив значений</param>
        public static void Forward1DHaarTransformation(ref double[] data)
        {
            double w0 = 0.5;
            double w1 = -0.5;
            double s0 = 0.5;
            double s1 = 0.5;

            double[] temp = new double[data.Length];

            int h = data.Length >> 1;
            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[i] = data[k] * s0 + data[k + 1] * s1;
                temp[i + h] = data[k] * w0 + data[k + 1] * w1;
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }
    }
}
