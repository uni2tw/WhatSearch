using log4net;
using log4net.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using WhatSearch.Core;
using WhatSearch.Utility;

namespace WhatSearch.Controllers
{
    public class UploadController : Controller
    {
        
        static SystemConfig config = Ioc.GetConfig();
        static ILog logger = LogManager.GetLogger(typeof(UploadController));

        public long LimitMb
        {
            get
            {
                return config.Upload.LimitMb ?? 200;
            }
        }

        static UploadController()
        {
            if (CheckTempFolderAtWorkFolder())
            {
                string msg = string.Format("TempFolder不要設定到WorkFolder裏面 work={0}, temp={1}",
                    config.Upload.Folder,
                    config.Upload.TempFolder
                    );
                logger.Error(msg);
                throw new ArgumentException(msg);
            }
            //10分鐘清一次
            System.Timers.Timer timer = new System.Timers.Timer(1000 * 60 * 10);
            timer.AutoReset = false;            
            timer.Elapsed += delegate (object sender, System.Timers.ElapsedEventArgs e)
            {
                try
                {
                    RemoveOldFiles();
                }
                catch
                {

                }
                finally
                {
                    (sender as System.Timers.Timer).Start();
                }
            };
            timer.Start();
        }

        public static void RemoveOldFiles()
        {
            DirectoryInfo diTemp = GetWorkTempFolder(null);
            DateTime now = DateTime.Now;
            {                
                foreach (var fsi in diTemp.GetFileSystemInfos())
                {
                    var dirInfo = fsi as DirectoryInfo;
                    if (dirInfo != null)
                    {
                        foreach (var fi2 in dirInfo.GetFiles())
                        {
                            TimeSpan deleteAfter = TimeSpan.FromDays(3) - (now - fi2.LastWriteTime);
                            if (deleteAfter.TotalSeconds <= 0)
                            {
                                logger.InfoFormat("Delete(1) {0} - {1}",
                                    fi2.FullName, fi2.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                                fi2.Delete();
                            }
                        }
                        if (dirInfo.GetFileSystemInfos("*.*", SearchOption.AllDirectories).Length == 0)
                        {
                            logger.Info("Delete(2) " + dirInfo.FullName);
                            //dirInfo.Delete();
                        }
                    }
                    else
                    {
                        TimeSpan deleteAfter = TimeSpan.FromDays(3) - (now - fsi.LastWriteTime);
                        if (deleteAfter.TotalSeconds <= 0)
                        {
                            logger.InfoFormat("Delete(3) {0} - {1}", 
                                fsi.FullName, fsi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                            fsi.Delete();
                        }
                    }
                }
            }

            DirectoryInfo diWork = GetWorkFolder(null);
            {                
                foreach (var fsi in diWork.GetFileSystemInfos())
                {
                    var diWorkSub = fsi as DirectoryInfo;
                    if (diWorkSub.FullName == diTemp.FullName)
                    {
                        continue;
                    }
                    if (diWorkSub != null)
                    {
                        foreach (var fi2 in diWorkSub.GetFiles())
                        {
                            TimeSpan deleteAfter = TimeSpan.FromDays(3) - (now - fi2.LastWriteTime);
                            if (deleteAfter.TotalSeconds <= 0)
                            {
                                logger.Info("Delete(4) " + fi2.FullName);
                                fi2.Delete();
                            }
                        }
                        if (diWorkSub.GetFileSystemInfos("*.*", SearchOption.AllDirectories).Length == 0)
                        {
                            logger.Info("Delete(5) " + diWorkSub.FullName);
                            diWorkSub.Delete();
                        }
                    }
                    else
                    {
                        TimeSpan deleteAfter = TimeSpan.FromDays(3) - (now - fsi.LastWriteTime);
                        if (deleteAfter.TotalSeconds <= 0)
                        {
                            logger.Info("Delete(6) " + fsi.FullName);
                            fsi.Delete();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Route("{secret}/upload")]
        [Route("upload")]
        public IActionResult List([FromRoute]Guid? secret)
        {
            string errorMessage;
            if (IsEnabled(out errorMessage) == false)
            {
                return Content(errorMessage);
            }

            DirectoryInfo di = GetWorkFolder(secret);
            DateTime now = DateTime.Now;

            List<FileInfo> fiInfos = new List<FileInfo>();
            if (di.Exists)
            {
                fiInfos.AddRange(di.GetFiles().OrderByDescending(t => t.CreationTime));
            }
            List<FileDownloadInfoModel> files = new List<FileDownloadInfoModel>();
            foreach (var fi in fiInfos)
            {
                TimeSpan deleteAfter = TimeSpan.FromDays(3) - (now - fi.LastWriteTime);
                if (deleteAfter.TotalSeconds <= 0)
                {
                    fi.Delete();
                    continue;
                }
                files.Add(new FileDownloadInfoModel
                {
                    Title = fi.Name,
                    Id = fi.Name,
                    Size = Helper.GetReadableByteSize(fi.Length),
                    Time = fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    DeleteAfter = Helper.GetReadableTimeSpan(deleteAfter)
                });
            }
            
            ViewBag.LimitMb = LimitMb;
            ViewBag.Secret = secret?.ToString();
            ViewBag.Items = files;

            return View();
        }
        private static bool CheckTempFolderAtWorkFolder()
        {
            if (config.Upload.TempFolder.StartsWith(config.Upload.Folder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
        private static DirectoryInfo GetWorkFolder(Guid? secret)
        {
            if (secret == null)
            {
                return new DirectoryInfo(config.Upload.Folder);
            }
            string path = Path.Combine(config.Upload.Folder, secret.ToString());
            return new DirectoryInfo(path);
        }

        private static DirectoryInfo GetWorkTempFolder(Guid? secret)
        {
            if (secret == null)
            {
                return new DirectoryInfo(config.Upload.TempFolder); 
            }
            string path=Path.Combine(config.Upload.TempFolder, secret.ToString());
            return new DirectoryInfo(path);
        }

        [HttpGet]
        [Route("upload/secret")]
        public IActionResult CreateSecret()
        {
            return Redirect(string.Format("/{0}/upload", Guid.NewGuid().ToString()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file_name"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("upload/status")]
        [Route("upload/{secret}/status")]
        public dynamic Status(string file_name, [FromRoute]Guid? secret)
        {
            var tempFolder = GetWorkTempFolder(secret);
            if (tempFolder.Exists == false)
            {
                tempFolder.Create();
            }
            string filePath = Path.Combine(tempFolder.FullName, file_name);            
            if (System.IO.File.Exists(filePath))
            {
                return Ok(new
                {
                    file = file_name,
                    loc = "temp",
                    len = new FileInfo(filePath).Length,
                    message = string.Format("先前上傳的檔案 {0} 未完成，你想要續傳嗎 ?", file_name)
                });
            }
            var workFolder = GetWorkFolder(secret);
            if (workFolder.Exists == false)
            {
                workFolder.Create();
            }
            filePath = Path.Combine(workFolder.FullName, file_name);
            if (System.IO.File.Exists(filePath))
            {
                return Ok(new
                {
                    file = file_name,
                    loc = "folder",
                    len = new FileInfo(filePath).Length,
                    message = string.Format("檔案 {0} 已存在，你要重新上傳嗎 ?", file_name)
                });
            }            
            return Ok(new
            {
                file = file_name,
                loc = string.Empty,
                len = 0,
                message = string.Empty
            });
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="file"></param>
        /// <param name="file_name"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("upload/post")]
        [Route("upload/{secret}/post")]
        public dynamic PostFile(IFormFile file, bool is_start, bool is_end, string file_name,
            [FromRoute] Guid? secret)
        {
            string errorMessage;
            if (IsEnabled(out errorMessage) == false)
            {
                return Content(errorMessage);
            }
            try
            {
                string filePath = Path.Combine(GetWorkTempFolder(secret).FullName, file_name);
                FileStream fs;
                if (is_start)
                {
                    fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                }
                else
                {
                    fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
                }
                file.CopyTo(fs);
                long fileLen = fs.Length;
                fs.Close();
                if (fileLen > 1024 * 1024 * LimitMb)
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                    }
                    return BadRequest(string.Format("發生錯誤, 檔案超過限制 {0} MB", 
                        LimitMb));
                }
                if (is_end)
                {
                    string destFilePath = Path.Combine(GetWorkFolder(secret).FullName, file_name);
                    try
                    {
                        System.IO.File.Move(filePath, destFilePath, true);
                    } 
                    catch
                    {
                        try
                        {
                            System.IO.File.Delete(filePath);                            
                        }
                        catch
                        {                        
                        }
                        return BadRequest("檔案正在被使用，無法覆蓋");
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("upload/{secret}/file/{*pathInfo}")]
        [Route("upload/file/{*pathInfo}")]
        public dynamic GetUploadFile(string pathInfo, Guid? secret)
        {
            string errorMessage;
            if (IsEnabled(out errorMessage) == false)
            {
                return Content(errorMessage);
            }
            string targetPath = Path.Combine(GetWorkFolder(secret).FullName, pathInfo);
            if (System.IO.File.Exists(targetPath) == false)
            {
                return NotFound();
            }
            return this.PhysicalFile(targetPath, "application/octet-stream");
        }

        private bool IsEnabled(out string message)
        {
            message = string.Empty;
            if (config.Upload == null || config.Upload.Enabled == false)
            {
                message = "上傳功能未啟用";
                return false;
            }
            if (string.IsNullOrEmpty(config.Upload.Folder) || string.IsNullOrEmpty(config.Upload.TempFolder))
            {
                message = "路徑參數未設定正確 Folder, TempFolder";
                return false;
            }
            try
            {
                if (Directory.Exists(config.Upload.Folder) == false)
                {
                    Directory.CreateDirectory(config.Upload.Folder);
                }
                if (Directory.Exists(config.Upload.TempFolder) == false)
                {
                    Directory.CreateDirectory(config.Upload.TempFolder);
                }
                return true;
            } 
            catch
            {
                message = "路徑參數雖然設定，但無法正確建立";
                return false;
            }
        }

        public class FileDownloadInfoModel
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Size { get; set; }
            public string Time { get; set; }
            public string DeleteAfter { get; set; }
        }

    }
}
