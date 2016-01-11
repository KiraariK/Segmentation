using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation
{
    class ColorSpacesProcessing
    {
        /*
         * Переводит данные из RGB в XYZ
         * Результат записывается в матрицу xyzData:
         * строки - количество элементов
         * столбцы - компоненты X, Y и Z соответственно
         */
        public static void TransformRGBtoXYZ(int[][] rgbData, ref double[][] xyzData)
        {
            int elementsCount = rgbData.Length / 3;
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
            double R = red / 255;
            double G = green / 255;
            double B = blue / 255;

            if (R > 0.04045) R = Math.Pow((R + 0.055) / 1.055, 2.4);
            else R = R / 12.92;
            if (G > 0.04045) G = Math.Pow((G + 0.055) / 1.055, 2.4);
            else G = G / 12.92;
            if (B > 0.04045) B = Math.Pow((B + 0.055) / 1.055, 2.4);
            else B = B / 12.92;

            R = R * 100;
            G = G * 100;
            B = B * 100;

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
            int elementsCount = xyzData.Length / 3;
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

            if (X > 0.008856) X = Math.Pow(X, 1 / 3);
            else X = (7.787 * X) + (16 / 116);
            if (Y > 0.008856) Y = Math.Pow(Y, 1 / 3);
            else Y = (7.787 * Y) + (16 / 116);
            if (Z > 0.008856) Z = Math.Pow(Z, 1 / 3);
            else Z = (7.787 * Z) + (16 / 116);

            double[] outVector = new double[3];
            outVector[0] = (116 * Y) - 16;
            outVector[1] = 500 * (X - Y);
            outVector[2] = 200 * (Y - Z);

            return outVector;
        }
    }
}
