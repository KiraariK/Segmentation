using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation
{
    public class ConditionalFiltering
    {
        /*
         * Вычисление порогового значения текстурной характеристики
         */
        private static double GetTextureThreshold(double[] textureFeatureData)
        {
            double max = textureFeatureData[0];
            for (var i = 0; i < textureFeatureData.Length; i++)
                if (max < textureFeatureData[i])
                    max = textureFeatureData[i];
            
            return 0.65 * max >= 14 ? 0.65 * max : 14;
        }


        /*
         * Функция возвращает значение условной фильтрации в зависимости от порогового значения текстурной характеристики
         * xyzData - массив значений интенсивности в цветовом пространстве Lab
         * textureFeatureData - массив значений текстурных характеристик, полученных с помощью преобразования Хаара
         */
        public static double[][] GetOutput (double[][] xyzData, double[] textureFeatureData)
        {
            double[][] result = new double[xyzData.Length / 3][];
            for (var i = 0; i < xyzData.Length / 3; i++)
                result[i] = new double[3];

            double textureFeatureThreshold = GetTextureThreshold(textureFeatureData);

            return result;
        }
    }
}
