using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSegmentation.Characterization;

namespace ImageSegmentation.Segmentation
{
    public class Pixel
    {
        public int[] Id { get; set; } // Идентификатор пикселя изображения: массив для x и y координаты
        public int[] RgbData { get; set; } // RGB-данные пикселя изображения
        public double[] IntensityFeatures { get; set; } // Lab-данные пикселя изображения
        public double[] ConditionalIntensityFeatures { get; set; } // Характеристики интенсивности, полученные после условной фильтрации
        public double[] TextureFeatures { get; set; } // Текстурные характеристики пикселя
        public bool IsChecked { get; set; } // Флаг отмечающий, был ли проверен пиксель на данной итерации KMCC

        public Pixel(int[] id, int[] rgbData)
        {
            Id = new int[2];
            RgbData = new int[3];
            for (int i = 0; i < Id.Length; i++)
                Id[i] = id[i];
            for (int i = 0; i < RgbData.Length; i++)
                RgbData[i] = rgbData[i];

            IntensityFeatures = new double[3];
            ConditionalIntensityFeatures = new double[3];
            TextureFeatures = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];
        }
    }
}
