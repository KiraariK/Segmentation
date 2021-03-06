﻿using ImageSegmentation.Characterization;

namespace ImageSegmentation.Segmentation
{
    public class Pixel
    {
        public enum PixelType { none, border, inner } // Тип пикселя, используемый для операций соседства пикселей

        public int[] Id { get; set; } // Идентификатор пикселя изображения: массив для x и y координаты
        public int GlobalNumber { get; set; } // Глобальный номер пикселя изображения (номера по строками изображения)
        public Region Region { get; set; } // Ссылка на регион, к которому принадлежит пиксель
        public int[] RgbData { get; set; } // RGB-данные пикселя изображения
        public int[] SegmentsRgbData { get; set; } // RGB-данные пикслея, успедненные по региону
        public int SegmentsGrayColor { get; set; } // Срдений (серый) цвет пикселя, создаваемый на основе данных SegmentsRgbData
        public double[] IntensityFeatures { get; set; } // Lab-данные пикселя изображения
        public double[] ConditionalIntensityFeatures { get; set; } // Характеристики интенсивности, полученные после условной фильтрации
        public double[] TextureFeatures { get; set; } // Текстурные характеристики пикселя
        public bool isNeighboring { get; set; } // Флаг показывающий, является ли данный пиксель граничным в сегменте
        public PixelType Type { get; set; } // Тип пикселя для операции разделения регионов
        public bool IsChecked { get; set; } // Флаг отмечающий, был ли проверен пиксель на данной итерации KMCC

        public Pixel(int[] id, int[] rgbData, int imageWidth)
        {
            Id = new int[2];
            RgbData = new int[3];
            SegmentsRgbData = new int[3];
            for (int i = 0; i < Id.Length; i++)
                Id[i] = id[i];
            GlobalNumber = (Id[0] * imageWidth) + Id[1];
            int colorSum = 0;
            for (int i = 0; i < RgbData.Length; i++)
            {
                RgbData[i] = rgbData[i];
                SegmentsRgbData[i] = rgbData[i];
                colorSum += SegmentsRgbData[i];
            }
            SegmentsGrayColor = colorSum / 3;

            Region = null;

            IntensityFeatures = new double[3];
            ConditionalIntensityFeatures = new double[3];
            TextureFeatures = new double[TextureFeaturesProcessing.numOfFeatures * TextureFeaturesProcessing.colorsCount];

            Type = PixelType.none;
            isNeighboring = false;
        }
    }
}
