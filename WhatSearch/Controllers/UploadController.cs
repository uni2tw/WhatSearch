using log4net;
using log4net.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading;
using WhatSearch.Core;
using WhatSearch.Utility;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using System.Net.Sockets;

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
                foreach (var dirInfo in diTemp.GetDirectories())
                {
                    if (dirInfo != null)
                    {
                        foreach (var fi2 in dirInfo.GetFiles())
                        {
                            DateTime lastWriteTime;
                            if (UploadUtil.CheckFileIsOld(fi2, out lastWriteTime))
                            {
                                logger.InfoFormat("Delete(1) {0} - {1}",
                                    fi2.FullName, lastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                                fi2.Delete();
                            }
                        }
                        if (dirInfo.GetFileSystemInfos("*.*", SearchOption.AllDirectories).Length == 0)
                        {
                            logger.Info("Delete(2) " + dirInfo.FullName);
                            //dirInfo.Delete();
                        }
                    }
                }
            }

            DirectoryInfo diWork = GetWorkFolder(null);
            {                
                foreach (var fsi in diWork.GetFileSystemInfos())
                {
                    var diWorkSub = fsi as DirectoryInfo;
                    if (diWorkSub != null)
                    {
                        foreach (var fi2 in diWorkSub.GetFiles())
                        {
                            DateTime lastWriteTime;
                            if (UploadUtil.CheckFileIsOld(fi2, out lastWriteTime))
                            {
                                logger.Info("Delete(4) " + fi2.FullName);
                                fi2.Delete();
                            }
                        }
                        if (diWorkSub.GetFileSystemInfos("*.*", SearchOption.AllDirectories).Length == 0)
                        {
                            logger.Info("Delete(5) " + diWorkSub.FullName);
                            //diWorkSub.Delete();
                        }
                    }
                    else
                    {
                        DateTime lastWriteTime;
                        if (UploadUtil.CheckFileIsOld(fsi, out lastWriteTime))
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
        [HttpGet]
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
                TimeSpan deleteAfter;
                if (UploadUtil.CheckFileIsOld(fi, out deleteAfter))
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
        
        private static DirectoryInfo GetWorkFolder(Guid? secret)
        {
            DirectoryInfo result;
            if (secret == null)
            {
                result = new DirectoryInfo(Path.Combine(config.Upload.Folder, "work"));
            }
            else
            {
                result = new DirectoryInfo(Path.Combine(config.Upload.Folder, "work", secret.ToString()));
            }
            if (result.Exists == false)
            {
                result.Create();
            }
            return result;
        }

        private static DirectoryInfo GetWorkTempFolder(Guid? secret)
        {
            DirectoryInfo result;
            string tempRootPath = Path.Combine(config.Upload.Folder, "temp");
            if (secret == null)
            {
                result = new DirectoryInfo(Path.Combine(config.Upload.Folder, "temp"));
            }
            else
            {
                result = new DirectoryInfo(Path.Combine(config.Upload.Folder, "temp",secret.ToString()));
            }
            if (result.Exists == false)
            {
                result.Create();
            }
            return result;
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
                        logger.InfoFormat("檔案搬動中 from={0} to={1}",
                                filePath,
                                destFilePath);
                        System.IO.File.Move(filePath, destFilePath, true);
                    } 
                    catch(Exception ex)
                    {
                        try
                        {
                            logger.WarnFormat("檔案因異外被刪除了 from={0} to={1}, ex={2}", 
                                filePath,
                                destFilePath,
                                ex);
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

        [HttpPut]
        [Route("upload/{file_name}")]
        public async Task<IActionResult> PutFileAsync([FromRoute] string file_name)
        {
            if (string.IsNullOrEmpty(file_name))
            {
                return NotFound();
            }
            string filePath = Path.Combine(GetWorkTempFolder(null).FullName, file_name);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await this.Request.BodyReader.CopyToAsync(fs);
            }

            string destFilePath = Path.Combine(GetWorkFolder(null).FullName, file_name);
            try
            {
                logger.InfoFormat("檔案搬動中 from={0} to={1}",
                        filePath,
                        destFilePath);
                System.IO.File.Move(filePath, destFilePath, true);
            }
            catch (Exception ex)
            {
                try
                {
                    logger.WarnFormat("檔案因異外被刪除了 from={0} to={1}, ex={2}",
                        filePath,
                        destFilePath,
                        ex);
                    System.IO.File.Delete(filePath);
                }
                catch
                {
                }
                return BadRequest("檔案正在被使用，無法覆蓋");
            }
            return Ok();
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
            string extension = Path.GetExtension(targetPath).ToLower();
            string mimeType = null;
            if (extension == ".png")
            {
                mimeType = "image/png";
            }
            else if (extension == ".jpg" || extension == ".jepg")
            {
                mimeType = "image/jpeg";
            }
            else
            {
                mimeType = "application/octet-stream";
            }
            return this.PhysicalFile(targetPath, mimeType);
        }

        private bool IsEnabled(out string message)
        {
            message = string.Empty;
            if (config.Upload == null || config.Upload.Enabled == false)
            {
                message = "上傳功能未啟用";
                return false;
            }
            if (string.IsNullOrEmpty(config.Upload.Folder) || Directory.Exists(config.Upload.Folder) == false)
            {
                message = "路徑參數未設定正確 Folder, TempFolder";
                return false;
            }
            return true;
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

    public class UploadUtil
    {
        public static bool CheckFileIsOld(FileSystemInfo fi, out DateTime lastWriteTime)
        {
            lastWriteTime = fi.CreationTime > fi.LastWriteTime ? fi.CreationTime : fi.LastWriteTime;
            return (DateTime.Now - lastWriteTime).TotalDays >= 3;
        }

        public static bool CheckFileIsOld(FileSystemInfo fi, out TimeSpan delteAfter)
        {
            DateTime lastWriteTime = fi.CreationTime > fi.LastWriteTime ? fi.CreationTime : fi.LastWriteTime;
            delteAfter = DateTime.Now - lastWriteTime;
            return delteAfter.TotalDays >= 3;
        }
    }
}
