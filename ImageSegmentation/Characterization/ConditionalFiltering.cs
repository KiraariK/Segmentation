using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSegmentation.Segmentation;

namespace ImageSegmentation.Characterization
{
    public class ConditionalFiltering
    {
        /// <summary>
        /// Вычисляет пороговое значение текстурных характеристик изображения
        /// </summary>
        /// <param name="textureFeatures">Текструные хараткеристики пикселей: строки - пиксели, столбцы - текстурные характеристики</param>
        /// <param name="imageHeight">Высота изображения</param>
        /// <param name="imageWidth">Ширина изображения</param>
        /// <returns>Пороговое значение текстурной характеристики</returns>
        public static double GetTextureFeatureThreshold(double[][] textureFeatures, int imageHeight, int imageWidth)
        {
            // массив для хранения норм векторов текстурных характеристик для каждого пикселя
            double[] textureFeatureNorms = new double[imageHeight * imageWidth];
            for (int i = 0; i < imageHeight * imageWidth; i++)
                textureFeatureNorms[i] = VectorNorm(textureFeatures[i]);

            // TODO: что-то с этим нужно делать
            // величина 0.65 * maxNorm всегда намного больше 14
            // Результат фильтрации Хаара - значения из диапазона от -1 до 1
            // Нужно нормировать значения полученных нами характеристик Лавса
            // либо нормировать значение 14 для наших текстурных характеристик
            // перед вычислением порогового значения текстурных характеристик
            double maxNorm = textureFeatureNorms.Max();

            return 0.65 * maxNorm >= 14 ? 0.65 * maxNorm : 14;
        }

        /// <summary>
        /// Вычисляет норму целочисленного вектора
        /// </summary>
        /// <param name="textureFeatures">Вектор целочисленных текстурных характеристик</param>
        /// <returns>Норма вектора</returns>
        public static double VectorNorm(double[] textureFeatures)
        {
            double sum = 0;
            for (int i = 0; i < TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount; i++)
                sum += Math.Abs(textureFeatures[i]);

            return sum;
        }
    }
}
