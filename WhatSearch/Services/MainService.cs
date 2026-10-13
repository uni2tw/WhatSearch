using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using WhatSearch.Core;
using WhatSearch.Models;
using WhatSearch.Services.Interfaces;
using WhatSearch.Utilities;
using WhatSearch.Utility;

namespace WhatSearch.Services
{
    public class MainService : IMainService
    {        
        IFolderIdManager fimgr = Ioc.Get<IFolderIdManager>();
        SystemConfig config = Ioc.GetConfig();

        public List<FileInfoView> GetFileInfoViewsInTheFolder(string folderId)
        {
            string path = fimgr.GetPath(folderId);

            List<FileInfoView> result = new List<FileInfoView>();
            if (string.IsNullOrEmpty(path))
            {
                return result;
            }
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists == false)
            {
                return result;
            }

            foreach (var subDirInfo in dirInfo.GetDirectories())
            {
                string efid = fimgr.GetIdByFilePath(subDirInfo.FullName);
                result.Add(new FileInfoView
                {
                    Id = efid,
                    Title = subDirInfo.Name,
                    Modify = subDirInfo.LastWriteTime.ToString(),
                    Type = Helper.ConstStrings.Folder,
                    Size = string.Empty
                });
            }
            List<Tuple<FileInfoView, FileInfo>> videoJobs = new List<Tuple<FileInfoView, FileInfo>>();
            foreach (var subFileInfo in dirInfo.GetFiles())
            {
                if (subFileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    continue;
                }
                string subEfid = fimgr.GetIdByFilePath(subFileInfo.FullName);
                string relPath;
                if (PathUtility.TryGetRelPath(subFileInfo.FullName, out relPath) == false)
                {
                    throw new Exception("不預期的意外，" + subFileInfo.FullName);
                }
                FileInfoView fileView = new FileInfoView
                {
                    Id = subEfid,
                    GetUrl = PathUtility.ToGetUrl(relPath),
                    Title = subFileInfo.Name,
                    Modify = subFileInfo.LastWriteTime.ToString(),
                    Type = Helper.GetFileDocType(subFileInfo.Extension),
                    Size = Helper.GetReadableByteSize(subFileInfo.Length),
                    Bytes = subFileInfo.Length
                };
                ApplyMusicTags(fileView, subFileInfo.FullName);
                if (fileView.Type == Helper.ConstStrings.Video)
                {
                    videoJobs.Add(Tuple.Create(fileView, subFileInfo));
                }
                result.Add(fileView);
            }
            ApplyVideoDurations(videoJobs);

            return result;
        }

        const int MaxDurationReadsPerListing = 50;
        const int DurationReadParallelism = 4;

        class DurationEntry
        {
            public double? Seconds { get; set; }
        }

        /// <summary>
        /// 讀取影片長度（秒）：同一次列表最多讀前 50 部、平行 4 個；結果依「路徑＋大小＋修改時間」快取。
        /// 讀不到（格式不支援、檔案損毀或正在寫入）就是 null，不影響列表。
        /// </summary>
        private static void ApplyVideoDurations(List<Tuple<FileInfoView, FileInfo>> jobs)
        {
            if (jobs.Count == 0)
            {
                return;
            }
            Parallel.ForEach(jobs.Take(MaxDurationReadsPerListing),
                new ParallelOptions { MaxDegreeOfParallelism = DurationReadParallelism },
                job => { job.Item1.Duration = GetVideoDuration(job.Item2); });
        }

        private static double? GetVideoDuration(FileInfo fi)
        {
            MemoryCache cache = Ioc.GetCache();
            string key = string.Format("duration://{0}|{1}|{2}", fi.FullName, fi.Length, fi.LastWriteTimeUtc.Ticks);
            DurationEntry entry = cache.Get<DurationEntry>(key);
            if (entry != null)
            {
                return entry.Seconds;
            }
            double? seconds = null;
            try
            {
                if (fi.Extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase))
                {
                    seconds = MatroskaSubtitleReader.GetDurationSeconds(fi.FullName);
                }
                if (seconds == null)
                {
                    using (TagLib.File tagFile = TagLib.File.Create(fi.FullName))
                    {
                        double s = tagFile.Properties.Duration.TotalSeconds;
                        if (s > 0)
                        {
                            seconds = s;
                        }
                    }
                }
            }
            catch
            {
            }
            cache.Set(key, new DurationEntry { Seconds = seconds },
                seconds.HasValue ? TimeSpan.FromHours(12) : TimeSpan.FromMinutes(2));
            return seconds;
        }

        /// <summary>
        /// 讀取 mp3 的 ID3 tag（歌手/專輯），讀不到就略過不影響列表顯示
        /// </summary>
        private static void ApplyMusicTags(FileInfoView view, string fullPath)
        {
            if (view.Type != Helper.ConstStrings.Music)
            {
                return;
            }
            try
            {
                using (TagLib.File tagFile = TagLib.File.Create(fullPath))
                {
                    TagLib.Tag tag = tagFile.Tag;
                    if (string.IsNullOrEmpty(tag.JoinedPerformers) == false)
                    {
                        view.Artist = tag.JoinedPerformers;
                    }
                    if (string.IsNullOrEmpty(tag.Album) == false)
                    {
                        view.Album = tag.Album;
                    }
                    if (string.IsNullOrEmpty(tag.Title) == false)
                    {
                        view.TrackTitle = tag.Title;
                    }
                    if (tag.Track > 0)
                    {
                        view.Track = tag.Track;
                    }
                    if (tag.Year > 0)
                    {
                        view.Year = tag.Year;
                    }
                    if (string.IsNullOrEmpty(tag.FirstAlbumArtist) == false)
                    {
                        view.AlbumArtist = tag.FirstAlbumArtist;
                    }
                    // 檔案已經開著，順便讀長度（供音樂資料夾版面顯示每首與總長）
                    double seconds = tagFile.Properties.Duration.TotalSeconds;
                    if (seconds > 0)
                    {
                        view.Duration = seconds;
                    }
                }
            }
            catch
            {
            }
        }

        public List<FileInfoView> GetRootShareFolders()
        {
            List<FileInfoView> result = new List<FileInfoView>();
            foreach (var folder in config.Folders)
            {
                DirectoryInfo di = new DirectoryInfo(folder.Path);
                if (di.Exists == false)
                {
                    continue;
                }                
                result.Add(GetFileInfoView(di, folder.Title));
            }
            return result;
        }

        public List<FileInfoView> GetBreadcrumbs(string efid)
        {
            List<FileInfoView> result = new List<FileInfoView>();

            if (efid == string.Empty)
            {
                result.Add(new FileInfoView
                {
                    Id = string.Empty,
                    Title = "我的分享",
                    Type = "檔案資料夾",
                });
                return result;
            }

            string dirPath = fimgr.GetPath(efid);
            if (string.IsNullOrEmpty(dirPath))
            {
                return result;
            }
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            do
            {
                if (dirInfo == null)
                {
                    break;
                }
                result.Add(GetFileInfoView(dirInfo));
                bool atTop = config.Folders.Any(t => t.Path.Equals(dirInfo.FullName, StringComparison.OrdinalIgnoreCase));
                if (atTop)
                {
                    break;
                }
                dirInfo = dirInfo.Parent;
            } while (true);
            result.Add(new FileInfoView
            {
                Id = string.Empty,
                Title = "我的分享",
                Type = "檔案資料夾",
            });
            result.Reverse();
            return result;
        }

        public FileInfoView GetFileInfoView(DirectoryInfo di, string preferenceTitle = null)
        {
            if (di == null || di.Exists == false)
            {
                return null;
            }
            string efid = fimgr.GetIdByFilePath(di.FullName);

            return new FileInfoView
            {
                Id = efid,
                Title = preferenceTitle ?? di.Name,
                Modify = di.CreationTime.ToString(),
                Type = Helper.ConstStrings.Folder,
                Size = string.Empty
            };
        }


        public List<FileInfoView> GetMusicItems(string folderId)
        {
            string path = fimgr.GetPath(folderId);

            List<FileInfoView> result = new List<FileInfoView>();
            if (string.IsNullOrEmpty(path))
            {
                return result;
            }
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists == false)
            {
                return result;
            }

            foreach (var fi in dirInfo.GetFiles())
            {
                if (fi.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    continue;
                }
                if (Helper.GetFileDocType(fi.Extension) != Helper.ConstStrings.Music)
                {
                    continue;
                }
                string subEfid = fimgr.GetIdByFilePath(fi.FullName);
                string relPath;
                if (PathUtility.TryGetRelPath(fi.FullName, out relPath) == false)
                {
                    throw new Exception("不預期的意外，" + fi.FullName);
                }
                FileInfoView fileView = new FileInfoView
                {
                    Id = subEfid,
                    GetUrl = PathUtility.ToGetUrl(relPath),
                    Title = fi.Name,
                    Modify = fi.LastWriteTime.ToString(),
                    Type = Helper.GetFileDocType(fi.Extension),
                    Size = Helper.GetReadableByteSize(fi.Length)
                };
                ApplyMusicTags(fileView, fi.FullName);
                result.Add(fileView);
            }

            return result;
        }
    }
}
