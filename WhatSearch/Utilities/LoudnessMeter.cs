using System;
using System.Collections.Generic;

namespace WhatSearch.Utilities
{
    public class LoudnessResult
    {
        /// <summary>整首的 integrated loudness（LUFS，ITU-R BS.1770-4，含閘門）；整首太小聲/靜音時為 null</summary>
        public double? IntegratedLufs { get; set; }
        /// <summary>取樣峰值（0~1+，線性），用來避免加增益後爆音</summary>
        public double SamplePeak { get; set; }
    }

    /// <summary>
    /// ITU-R BS.1770-4 / EBU R128 的 integrated loudness 量測：K-weighting（高頻擱架 + RLB 高通）、
    /// 400ms 區塊（75% 重疊）、-70 LUFS 絕對閘門、-10 LU 相對閘門。支援單聲道與立體聲，
    /// 單聲道視為「兩聲道播同一份」（權重 2），因為實際播放時左右喇叭都會出聲。
    /// 以串流方式逐批餵入交錯（interleaved）的浮點取樣，記憶體只留每 100ms 一筆能量。
    /// </summary>
    public class LoudnessMeter
    {
        readonly int channels;
        readonly int segmentFrames;
        readonly double channelWeight;
        readonly Biquad[][] filters;      // [channel][stage]
        readonly double[] segmentEnergy;  // 目前這 100ms 內各聲道的 K-weighted 平方和
        int segmentCount;
        readonly double[] lastSegments = new double[4];
        int lastSegmentsFilled;
        readonly List<double> blockEnergies = new List<double>();
        double peak;

        public LoudnessMeter(int sampleRate, int channels)
        {
            if (sampleRate <= 0) { throw new ArgumentOutOfRangeException("sampleRate"); }
            if (channels < 1 || channels > 2) { throw new ArgumentOutOfRangeException("channels", "只支援單聲道與立體聲"); }
            this.channels = channels;
            this.segmentFrames = Math.Max(1, sampleRate / 10);
            this.channelWeight = channels == 1 ? 2.0 : 1.0;
            this.segmentEnergy = new double[channels];
            this.filters = new Biquad[channels][];
            for (int c = 0; c < channels; c++)
            {
                filters[c] = new[] { Biquad.HighShelf(sampleRate), Biquad.HighPass(sampleRate) };
            }
        }

        /// <summary>餵入交錯取樣；count 是「樣本數」（= 框數 × 聲道數）</summary>
        public void Add(float[] interleaved, int count)
        {
            int frames = count / channels;
            for (int f = 0; f < frames; f++)
            {
                for (int c = 0; c < channels; c++)
                {
                    double x = interleaved[f * channels + c];
                    double ax = Math.Abs(x);
                    if (ax > peak) { peak = ax; }
                    double y = filters[c][1].Process(filters[c][0].Process(x));
                    segmentEnergy[c] += y * y;
                }
                if (++segmentCount == segmentFrames)
                {
                    FlushSegment();
                }
            }
        }

        void FlushSegment()
        {
            double sum = 0;
            for (int c = 0; c < channels; c++)
            {
                sum += segmentEnergy[c];
                segmentEnergy[c] = 0;
            }
            segmentCount = 0;
            // 一個 100ms 片段的「各聲道加權平方和 / 片段框數」；400ms 區塊 = 連續 4 個片段的平均
            double segment = sum * channelWeight / segmentFrames;
            if (lastSegmentsFilled < 4)
            {
                lastSegments[lastSegmentsFilled++] = segment;
            }
            else
            {
                lastSegments[0] = lastSegments[1];
                lastSegments[1] = lastSegments[2];
                lastSegments[2] = lastSegments[3];
                lastSegments[3] = segment;
            }
            if (lastSegmentsFilled == 4)
            {
                blockEnergies.Add((lastSegments[0] + lastSegments[1] + lastSegments[2] + lastSegments[3]) / 4.0);
            }
        }

        public LoudnessResult Finish()
        {
            var result = new LoudnessResult { SamplePeak = peak };

            // 絕對閘門：區塊響度 > -70 LUFS
            double absEnergyThreshold = Math.Pow(10, (-70.0 + 0.691) / 10.0);
            double sum = 0;
            int n = 0;
            foreach (double z in blockEnergies)
            {
                if (z > absEnergyThreshold) { sum += z; n++; }
            }
            if (n == 0) { return result; }

            // 相對閘門：比「通過絕對閘門的區塊平均響度」低 10 LU 的區塊也排除
            double relLufs = -0.691 + 10 * Math.Log10(sum / n) - 10.0;
            double relEnergyThreshold = Math.Pow(10, (relLufs + 0.691) / 10.0);
            sum = 0;
            n = 0;
            foreach (double z in blockEnergies)
            {
                if (z > absEnergyThreshold && z > relEnergyThreshold) { sum += z; n++; }
            }
            if (n == 0) { return result; }
            result.IntegratedLufs = -0.691 + 10 * Math.Log10(sum / n);
            return result;
        }

        /// <summary>雙二階濾波器（direct form I，double 精度）</summary>
        sealed class Biquad
        {
            double b0, b1, b2, a1, a2;
            double x1, x2, y1, y2;

            public double Process(double x)
            {
                double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x;
                y2 = y1; y1 = y;
                return y;
            }

            // BS.1770 的 K-weighting 第一級（高頻擱架）；係數公式與 libebur128 相同，可適用任意取樣率
            public static Biquad HighShelf(double fs)
            {
                double f0 = 1681.974450955533;
                double G = 3.999843853973347;
                double Q = 0.7071752369554196;
                double K = Math.Tan(Math.PI * f0 / fs);
                double Vh = Math.Pow(10.0, G / 20.0);
                double Vb = Math.Pow(Vh, 0.4996667741545416);
                double a0 = 1.0 + K / Q + K * K;
                return new Biquad
                {
                    b0 = (Vh + Vb * K / Q + K * K) / a0,
                    b1 = 2.0 * (K * K - Vh) / a0,
                    b2 = (Vh - Vb * K / Q + K * K) / a0,
                    a1 = 2.0 * (K * K - 1.0) / a0,
                    a2 = (1.0 - K / Q + K * K) / a0
                };
            }

            // 第二級（RLB 高通）
            public static Biquad HighPass(double fs)
            {
                double f0 = 38.13547087602444;
                double Q = 0.5003270373238773;
                double K = Math.Tan(Math.PI * f0 / fs);
                double a0 = 1.0 + K / Q + K * K;
                return new Biquad
                {
                    b0 = 1.0,
                    b1 = -2.0,
                    b2 = 1.0,
                    a1 = 2.0 * (K * K - 1.0) / a0,
                    a2 = (1.0 - K / Q + K * K) / a0
                };
            }
        }
    }
}
