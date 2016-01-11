using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation.Characterization
{
    class ColorSpacesProcessing
    {
        /// <summary>
        /// Переводит данные из RGB в Lab
        /// Результат записывает в матрицу labData
        /// </summary>
        /// <param name="rgbData">rgbData - массив, где для каждого элемента строки в столбцах стоят элементы r, g и b</param>
        /// <param name="labData">labData - массив, где для каждого элемента строки в столбцах будут стоять элементы L, a и b</param>
        public static void TransformRGBtoLab(int[][] rgbData, ref double[][] labData)
        {
            int elementsCount = rgbData.Length;
            for (int i = 0; i < elementsCount; i++)
            {
                double[] xyzVector = RGBtoXYZ(rgbData[i][0], rgbData[i][1], rgbData[i][2]);
                double[] labVector = XYZtoLab(xyzVector[0], xyzVector[1], xyzVector[2]);
                labData[i][0] = labVector[0];
                labData[i][1] = labVector[1];
                labData[i][2] = labVector[2];
            }
        }

        /*
         * Переводит данные из RGB в XYZ
         * Результат записывается в матрицу xyzData:
         * строки - количество элементов
         * столбцы - компоненты X, Y и Z соответственно
         */
        public static void TransformRGBtoXYZ(int[][] rgbData, ref double[][] xyzData)
        {
            int elementsCount = rgbData.Length;
            for (int i = 0; i < elementsCount; i++)
            {
                double[] xyzVector = RGBtoXYZ(rgbData[i][0], rgbData[i][1], rgbData[i][2]);
                xyzData[i][0] = xyzVector[0];
                xyzData[i][1] = xyzVector[1];
                xyzData[i][2] = xyzVector[2];
            }
        }

        /*
         * Возвращает вектор значений X Y Z для элемента RGB
         * Вектор значений состоит из 3-х компонентов:
         * X, Y и Z значений соответственно
         */
        private static double[] RGBtoXYZ(int red, int green, int blue)
        {
            double R = (double)red / 255.0;
            double G = (double)green / 255.0;
            double B = (double)blue / 255.0;

            if (R > 0.04045) R = Math.Pow((R + 0.055) / 1.055, 2.4);
            else R = R / 12.92;
            if (G > 0.04045) G = Math.Pow((G + 0.055) / 1.055, 2.4);
            else G = G / 12.92;
            if (B > 0.04045) B = Math.Pow((B + 0.055) / 1.055, 2.4);
            else B = B / 12.92;

            R = R * 100.0;
            G = G * 100.0;
            B = B * 100.0;

            double[] outVector = new double[3];
            outVector[0] = R * 0.4124 + G * 0.3576 + B * 0.1805; // X компонента
            outVector[1] = R * 0.2126 + G * 0.7152 + B * 0.0722; // Y компонента
            outVector[2] = R * 0.0193 + G * 0.1192 + B * 0.9505; // Z компонента

            return outVector;
        }

        /*
         * Переводит данные из XYZ в Lab
         * Результат записывается в матрицу labData:
         * строки - количество элементов
         * столбцы - компоненты L, a и b соответственно
         */
        public static void TransformXYZtoLab(double[][] xyzData, ref double[][] labData)
        {
            int elementsCount = xyzData.Length;
            for (int i = 0; i < elementsCount; i++)
            {
                double[] labVector = XYZtoLab(xyzData[i][0], xyzData[i][1], xyzData[i][2]);
                labData[i][0] = labVector[0];
                labData[i][1] = labVector[1];
                labData[i][2] = labVector[2];
            }
        }

        /*
         * Возвращает вектор значений L a b для элемента XYZ
         * Вектор значений состоит из 3-х компонентов:
         * L, a и b значений соответственно
         */
        private static double[] XYZtoLab(double x, double y, double z)
        {
            double ref_x = 95.047;
            double ref_y = 100.000;
            double ref_z = 108.883;

            double X = x / ref_x;
            double Y = y / ref_y;
            double Z = z / ref_z;

            if (X > 0.008856) X = Math.Pow(X, 1.0 / 3.0);
            else X = (7.787 * X) + (16.0 / 116.0);
            if (Y > 0.008856) Y = Math.Pow(Y, 1.0 / 3.0);
            else Y = (7.787 * Y) + (16.0 / 116.0);
            if (Z > 0.008856) Z = Math.Pow(Z, 1.0 / 3.0);
            else Z = (7.787 * Z) + (16.0 / 116.0);

            double[] outVector = new double[3];
            outVector[0] = (116.0 * Y) - 16.0;
            outVector[1] = 500.0 * (X - Y);
            outVector[2] = 200.0 * (Y - Z);

            return outVector;
        }
    }
}
