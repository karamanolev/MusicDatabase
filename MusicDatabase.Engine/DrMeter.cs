using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicDatabase.Engine
{
    public class DrMeter
    {
        private const double FACTOR16 = 1.0 / 32768;
        private const double FACTOR24 = 1.0 / 8388608;
        private const int FRAGMENT_LENGTH = 3000;

        private int channels, sampleRate, sampleSize;
        private List<double>[] rmsValues;
        private List<double>[] peakValues;
        private double[] sum;
        private double[] peak;
        private int fragment, fragmentSize, fragmentRead;
        private bool fragmentStarted;

        private double dynamicRange = double.NaN;

        public DrMeter(int channels, int sampleRate, int sampleSize)
        {
            this.channels = channels;
            this.sampleRate = sampleRate;
            this.sampleSize = sampleSize;

            this.rmsValues = new List<double>[this.channels];
            this.peakValues = new List<double>[this.channels];

            for (int ch = 0; ch < this.channels; ++ch)
            {
                this.rmsValues[ch] = new List<double>();
                this.peakValues[ch] = new List<double>();
            }

            this.sum = new double[this.channels];
            this.peak = new double[this.channels];

            this.fragmentSize = (int)((long)this.sampleRate * FRAGMENT_LENGTH / 1000);
        }

        private void FragmentStart()
        {
            if (this.fragmentStarted)
            {
                throw new InvalidOperationException();
            }

            for (int i = 0; i < this.channels; ++i)
            {
                this.sum[i] = 0;
                this.peak[i] = 0;
            }

            this.fragmentRead = 0;
            this.fragmentStarted = true;
        }

        private void Scan(int[,] buffer, int offset, int toScan)
        {
            int end = offset + toScan;
            for (int i = offset; i < end; ++i)
            {
                for (int ch = 0; ch < this.channels; ++ch)
                {
                    double value;
                    switch (this.sampleSize)
                    {
                        case 16:
                            value = buffer[i, ch] * FACTOR16;
                            break;
                        case 24:
                            value = buffer[i, ch] * FACTOR24;
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    this.sum[ch] += value * value;
                    if (this.peak[ch] < value)
                    {
                        this.peak[ch] = value;
                    }
                }
            }
        }

        private void FragmentFinish()
        {
            if (!this.fragmentStarted)
            {
                throw new InvalidOperationException();
            }

            for (int ch = 0; ch < this.channels; ++ch)
            {
                this.rmsValues[ch].Add(Math.Sqrt(2 * this.sum[ch] / this.fragmentRead));
                this.peakValues[ch].Add(this.peak[ch]);
            }

            ++this.fragment;
            this.fragmentStarted = false;
        }

        public void Feed(int[,] buffer, int length)
        {
            int offset = 0;

            while (length > 0)
            {
                if (!this.fragmentStarted)
                {
                    this.FragmentStart();
                }

                int fragmentLeft = this.fragmentSize - this.fragmentRead;
                int toScan = Math.Min(fragmentLeft, length);

                this.Scan(buffer, offset, toScan);

                offset += toScan;
                length -= toScan;
                this.fragmentRead += toScan;

                if (this.fragmentRead >= this.fragmentSize)
                {
                    this.FragmentFinish();
                }
            }
        }

        private int CompareSamples(double a, double b)
        {
            if (a > b) return -1;
            if (a < b) return 1;
            return 0;
        }

        public void Finish()
        {
            if (!double.IsNaN(this.dynamicRange))
            {
                throw new InvalidOperationException("Finish() has already been called.");
            }

            if (this.fragmentStarted)
            {
                this.FragmentFinish();
            }

            double[] rmsScore = new double[this.channels];
            double[] rms = new double[this.channels];
            double[] peakScore = new double[this.channels];
            double[] drChannel = new double[this.channels];
            double drSum = 0;

            for (int ch = 0; ch < this.channels; ++ch)
            {
                this.rmsValues[ch].Sort(this.CompareSamples);

                double rmsSum = 0;
                int valuestoUse = this.fragment / 5;
                for (int i = 0; i < valuestoUse; ++i)
                {
                    double value = this.rmsValues[ch][i];
                    rmsSum += value * value;
                }
                rmsScore[ch] = Math.Sqrt(rmsSum / valuestoUse);

                rmsSum = 0;
                for (int i = 0; i < this.fragment; ++i)
                {
                    double value = this.rmsValues[ch][i];
                    rmsSum += value * value;
                }
                rms[ch] = Math.Sqrt(rmsSum / this.fragment);

                this.peakValues[ch].Sort(this.CompareSamples);
                peakScore[ch] = this.peakValues[ch][Math.Min(1, this.fragment)];

                drChannel[ch] = ToDb(peakScore[ch] / rmsScore[ch]);
                drSum += drChannel[ch];
            }

            this.dynamicRange = drSum / this.channels;

            this.peak = null;
            this.sum = null;
            this.peakValues = null;
            this.rmsValues = null;
        }

        private static double ToDb(double p)
        {
            return 20 * Math.Log10(p);
        }

        public double GetDynamicRange()
        {
            if (this.dynamicRange == double.NaN)
            {
                throw new InvalidOperationException("You must first call Finish() before getting the dynamic range.");
            }
            return this.dynamicRange;
        }
    }
}
