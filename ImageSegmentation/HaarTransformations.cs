using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation
{
    class HaarTransformations
    {
        /*
         * Производит дискретное преобразование Хаара над двумерной матрицей цвета colorMatrix
         * По отдельности: сначала с каждой строкой, затем с каждым столбцом
         * Результат - массив colorMatrix содержит текстурные характерипстики
         */
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

        /*
         * Производит дискретное преобразование Хаара над одномерным массивом чисел data
         * Результат - массив data содержит тексутрные характеристики
         */
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
