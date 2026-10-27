using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatSearch.Utilities
{
    /// <summary>MKV 裡的一條影片或音訊軌：給人看的名稱，以及讓瀏覽器 canPlayType 判斷用的編碼字串</summary>
    public class MkvStreamInfo
    {
        /// <summary>例如 H.264 High 10 · 10-bit、H.265 Main 10、AC3</summary>
        public string Label { get; set; }
        /// <summary>HTML 媒體 MIME 的 codecs 參數（avc1.6E0028、hvc1.2.4.L93.B0、ac-3…）；不知道就是 null</summary>
        public string Mime { get; set; }
        /// <summary>瀏覽器一定不支援的編碼（例如 VFW 的 XviD／DivX），不必再問瀏覽器</summary>
        public bool AlwaysUnsupported { get; set; }
        public bool IsDefault { get; set; }
    }

    public class MkvMediaInfo
    {
        public List<MkvStreamInfo> Video { get; set; } = new List<MkvStreamInfo>();
        public List<MkvStreamInfo> Audio { get; set; } = new List<MkvStreamInfo>();
    }

    /// <summary>
    /// 讀 MKV 開頭的軌道資訊，說明影片與音訊用的編碼（H.264 的 profile 與位元深度、H.265、AC3／DTS…）。
    /// 伺服器只回報事實；「這個瀏覽器能不能播」由網頁用 canPlayType 自己判斷，所以每個裝置看到的提示都是自己的狀況。
    /// </summary>
    public static class MkvCodecInspector
    {
        const int VideoTrackType = 1;
        const int AudioTrackType = 2;

        public static MkvMediaInfo Inspect(string path)
        {
            MkvMediaInfo info = new MkvMediaInfo();
            foreach (MkvSubtitleTrack t in MatroskaSubtitleReader.ListAllTracks(path))
            {
                if (t.Type == VideoTrackType)
                {
                    info.Video.Add(DescribeVideo(t));
                }
                else if (t.Type == AudioTrackType)
                {
                    info.Audio.Add(DescribeAudio(t));
                }
            }
            return info;
        }

        static MkvStreamInfo DescribeVideo(MkvSubtitleTrack t)
        {
            string codec = t.CodecId ?? string.Empty;
            byte[] cp = t.CodecPrivate;
            MkvStreamInfo s = new MkvStreamInfo { IsDefault = t.IsDefault };
            if (codec == "V_MPEG4/ISO/AVC")
            {
                s.Label = "H.264";
                if (cp != null && cp.Length >= 4)
                {
                    int profile = cp[1];
                    s.Mime = string.Format("avc1.{0:X2}{1:X2}{2:X2}", cp[1], cp[2], cp[3]);
                    string name = AvcProfileName(profile);
                    int? depth = AvcBitDepth(cp, profile);
                    s.Label = "H.264 " + name + (depth.HasValue && depth.Value > 8 ? " · " + depth.Value + "-bit" : string.Empty);
                }
                else
                {
                    s.Mime = "avc1.640028";
                }
            }
            else if (codec == "V_MPEGH/ISO/HEVC")
            {
                s.Label = "H.265";
                if (cp != null && cp.Length >= 13)
                {
                    int profileIdc = cp[1] & 0x1F;
                    int bitDepth = cp.Length >= 18 ? (cp[17] & 0x07) + 8 : 8;
                    s.Mime = HevcMime(cp);
                    s.Label = "H.265 " + (profileIdc == 1 ? "Main" : profileIdc == 2 ? "Main 10" : "profile " + profileIdc) + (bitDepth > 8 ? " · " + bitDepth + "-bit" : string.Empty);
                }
                else
                {
                    s.Mime = "hvc1.1.6.L93.B0";
                }
            }
            else if (codec == "V_VP9") { s.Label = "VP9"; s.Mime = "vp09.00.10.08"; }
            else if (codec == "V_VP8") { s.Label = "VP8"; s.Mime = "vp8"; }
            else if (codec == "V_AV1") { s.Label = "AV1"; s.Mime = "av01.0.08M.08"; }
            else if (codec.StartsWith("V_MPEG4/ISO/", StringComparison.Ordinal)) { s.Label = "MPEG-4 (XviD/DivX)"; s.Mime = "mp4v.20.9"; }
            else if (codec.StartsWith("V_MPEG2", StringComparison.Ordinal)) { s.Label = "MPEG-2"; s.Mime = "mp2v"; }
            else if (codec == "V_MS/VFW/FOURCC") { s.Label = "舊式編碼（XviD／DivX／WMV 等）"; s.AlwaysUnsupported = true; }
            else if (codec.StartsWith("V_REAL", StringComparison.Ordinal)) { s.Label = "RealVideo"; s.AlwaysUnsupported = true; }
            else { s.Label = codec; }
            return s;
        }

        static MkvStreamInfo DescribeAudio(MkvSubtitleTrack t)
        {
            string codec = t.CodecId ?? string.Empty;
            MkvStreamInfo s = new MkvStreamInfo { IsDefault = t.IsDefault };
            if (codec.StartsWith("A_AAC", StringComparison.Ordinal)) { s.Label = "AAC"; s.Mime = "mp4a.40.2"; }
            else if (codec == "A_AC3") { s.Label = "AC3"; s.Mime = "ac-3"; }
            else if (codec == "A_EAC3") { s.Label = "E-AC3"; s.Mime = "ec-3"; }
            else if (codec.StartsWith("A_DTS", StringComparison.Ordinal)) { s.Label = "DTS"; s.Mime = "dtsc"; }
            else if (codec == "A_TRUEHD") { s.Label = "TrueHD"; s.Mime = "mlpa"; }
            else if (codec == "A_FLAC") { s.Label = "FLAC"; s.Mime = "flac"; }
            else if (codec == "A_OPUS") { s.Label = "Opus"; s.Mime = "opus"; }
            else if (codec == "A_VORBIS") { s.Label = "Vorbis"; s.Mime = "vorbis"; }
            else if (codec == "A_MPEG/L3") { s.Label = "MP3"; s.Mime = "mp4a.6B"; }
            else { s.Label = codec; }
            return s;
        }

        static string AvcProfileName(int profile)
        {
            switch (profile)
            {
                case 66: return "Baseline";
                case 77: return "Main";
                case 88: return "Extended";
                case 100: return "High";
                case 110: return "High 10";
                case 122: return "High 4:2:2";
                case 244: return "High 4:4:4";
                default: return "profile " + profile;
            }
        }

        // avcC：[5] 低 5 位是 SPS 個數，接著 2 位元組長度與 SPS 本體；從 SPS 讀出位元深度（High 10 以上才有這個欄位）
        static int? AvcBitDepth(byte[] avcc, int profile)
        {
            if (profile != 100 && profile != 110 && profile != 122 && profile != 244 && profile != 44 && profile != 83 && profile != 86 && profile != 118 && profile != 128)
            {
                return 8;
            }
            try
            {
                if (avcc.Length < 8 || (avcc[5] & 0x1F) < 1)
                {
                    return null;
                }
                int len = (avcc[6] << 8) | avcc[7];
                if (len < 5 || 8 + len > avcc.Length)
                {
                    return null;
                }
                // 去掉防競爭位元組（00 00 03）後，從 NAL header 之後（profile_idc、constraint、level_idc 共 3 位元組）開始讀
                List<byte> rbsp = new List<byte>();
                for (int i = 8 + 1; i < 8 + len; i++)
                {
                    if (rbsp.Count >= 2 && rbsp[rbsp.Count - 1] == 0 && rbsp[rbsp.Count - 2] == 0 && avcc[i] == 3)
                    {
                        continue;
                    }
                    rbsp.Add(avcc[i]);
                }
                BitReader br = new BitReader(rbsp.ToArray());
                br.Skip(24);               // profile_idc、constraint flags、level_idc
                br.ReadUe();               // seq_parameter_set_id
                int chroma = br.ReadUe();  // chroma_format_idc
                if (chroma == 3)
                {
                    br.Skip(1);            // separate_colour_plane_flag
                }
                return br.ReadUe() + 8;    // bit_depth_luma_minus8
            }
            catch (Exception)
            {
                return null;
            }
        }

        // hvcC → hvc1.<profile>.<相容旗標（位元反轉）>.<L|H><level>.<constraint>
        static string HevcMime(byte[] c)
        {
            int profileSpace = (c[1] >> 6) & 0x03;
            int tier = (c[1] >> 5) & 0x01;
            int profileIdc = c[1] & 0x1F;
            uint compat = ((uint)c[2] << 24) | ((uint)c[3] << 16) | ((uint)c[4] << 8) | c[5];
            uint reversed = 0;
            for (int i = 0; i < 32; i++)
            {
                if ((compat & (1u << i)) != 0)
                {
                    reversed |= 1u << (31 - i);
                }
            }
            int level = c[12];
            List<string> constraints = new List<string>();
            int last = 11;
            while (last > 6 && c[last] == 0)
            {
                last--;
            }
            for (int i = 6; i <= last; i++)
            {
                constraints.Add(c[i].ToString("X2"));
            }
            string space = profileSpace == 0 ? string.Empty : ((char)('A' + profileSpace - 1)).ToString();
            return "hvc1." + space + profileIdc + "." + reversed.ToString("X") + "." + (tier == 1 ? "H" : "L") + level + (constraints.Count > 0 ? "." + string.Join(".", constraints) : string.Empty);
        }

        class BitReader
        {
            readonly byte[] data;
            int pos;

            public BitReader(byte[] data)
            {
                this.data = data;
            }

            public int ReadBit()
            {
                if (pos >= data.Length * 8)
                {
                    throw new InvalidOperationException("end of data");
                }
                int bit = (data[pos >> 3] >> (7 - (pos & 7))) & 1;
                pos++;
                return bit;
            }

            public void Skip(int n)
            {
                for (int i = 0; i < n; i++)
                {
                    ReadBit();
                }
            }

            // 無號 Exp-Golomb
            public int ReadUe()
            {
                int zeros = 0;
                while (ReadBit() == 0)
                {
                    zeros++;
                    if (zeros > 31)
                    {
                        throw new InvalidOperationException("bad ue");
                    }
                }
                int value = 0;
                for (int i = 0; i < zeros; i++)
                {
                    value = (value << 1) | ReadBit();
                }
                return (1 << zeros) - 1 + value;
            }
        }
    }
}
