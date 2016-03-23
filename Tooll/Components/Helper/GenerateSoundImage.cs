// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Framefield.Core;
using Un4seen;
using Un4seen.Bass;

namespace Framefield.Tooll.Components.Helper
{
    public static class GenerateSoundImage
    {
        public static string GenerateSoundSprectrumAndVolume(String soundFilePath)
        {
            if ( String.IsNullOrEmpty(soundFilePath) || !File.Exists(soundFilePath))
                return null;

            var imageFilePath = soundFilePath+".waveform.png";
            if (File.Exists(imageFilePath))
            {
                Logger.Info("Reusing sound image file: {0}", imageFilePath);
                return imageFilePath;
            }

            Logger.Info("Generating {0}...", imageFilePath);
            
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_LATENCY, IntPtr.Zero);
            var stream = Bass.BASS_StreamCreateFile(soundFilePath, 0, 0,
                                                    BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_STREAM_PRESCAN);

            const int SAMPLING_FREQUENCY = 100; //warning: in TimelineImage this samplingfrequency is assumed to be 100
            const double SAMPLING_RESOLUTION = 1.0 / SAMPLING_FREQUENCY;

            var sampleLength = Bass.BASS_ChannelSeconds2Bytes(stream, SAMPLING_RESOLUTION);            
            var numSamples = Bass.BASS_ChannelGetLength(stream, BASSMode.BASS_POS_BYTES) / sampleLength;

            Bass.BASS_ChannelPlay(stream, false);

            const int spectrumLength = 1024;
            const int imageHeight = 256;
            var spectrumImage = new Bitmap((int)numSamples, imageHeight);
            var volumeImage = new Bitmap((int)numSamples, imageHeight);

            var tempBuffer = new float[spectrumLength];

            const float maxIntensity = 500;
            const int COLOR_STEPS = 255;
            const int PALETTE_SIZE = 3 * COLOR_STEPS;

            int a, b, r, g;
            var palette = new Color[PALETTE_SIZE];
            int palettePos;

            for (palettePos = 0; palettePos < PALETTE_SIZE; ++palettePos)
            {
                a = 255;
                if (palettePos < PALETTE_SIZE * 0.666f)
                    a = (int)(palettePos * 255 / (PALETTE_SIZE * 0.666f));

                b = 0;
                if (palettePos < PALETTE_SIZE * 0.333f)
                    b = palettePos;
                else if (palettePos < PALETTE_SIZE * 0.666f)
                    b = -palettePos + 510;

                r = 0;
                if (palettePos > PALETTE_SIZE * 0.666f)
                    r = 255;
                else if (palettePos > PALETTE_SIZE * 0.333f)
                    r = palettePos - 255;

                g = 0;
                if (palettePos > PALETTE_SIZE * 0.666f)
                    g = palettePos - 510;

                palette[palettePos] = Color.FromArgb(a, r, g, b);
            }

            var f = (float)(spectrumLength / Math.Log((float)(imageHeight + 1)));
            var f2 = (float)((PALETTE_SIZE - 1) / Math.Log(maxIntensity + 1));
            var f3 = (float)((imageHeight - 1) / Math.Log(32768.0f + 1));

            for (var sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
            {
                Bass.BASS_ChannelSetPosition(stream, sampleIndex * sampleLength, BASSMode.BASS_POS_BYTES);
                Bass.BASS_ChannelGetData(stream, tempBuffer, (int)Un4seen.Bass.BASSData.BASS_DATA_FFT2048);
               
                for (var rowIndex = 0; rowIndex < imageHeight; ++rowIndex)
                {
                    var j_ = (int)(f * Math.Log(rowIndex + 1));
                    var pj_ = (int)(rowIndex > 0 ? f * Math.Log(rowIndex - 1 + 1) : j_);
                    var nj_ = (int)(rowIndex < imageHeight - 1 ? f * Math.Log(rowIndex + 1 + 1) : j_);
                    float intensity = 125.0f * tempBuffer[spectrumLength - pj_ - 1] +
                                      750.0f * tempBuffer[spectrumLength - j_ - 1] +
                                      125.0f * tempBuffer[spectrumLength - nj_ - 1];
                    intensity = Math.Min(maxIntensity, intensity);
                    intensity = Math.Max(0.0f, intensity);

                    palettePos = (int)(f2 * Math.Log(intensity + 1));
                    spectrumImage.SetPixel(sampleIndex, rowIndex, palette[palettePos]);
                }

                if (sampleIndex%1000 == 0)
                {
                    Logger.Info("   computing sound image {0}% complete", (100*sampleIndex/numSamples));
                }
            }
            
            spectrumImage.Save(imageFilePath);
            Bass.BASS_ChannelStop(stream);
            Bass.BASS_StreamFree(stream);

            return imageFilePath;
        }
    }
}
