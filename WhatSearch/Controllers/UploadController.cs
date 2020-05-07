using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WhatSearch.Core;
using WhatSearch.Utility;

namespace WhatSearch.Controllers
{
    //[Route("[controller]")]
    public class UploadController : Controller
    {
        
        SystemConfig config = Ioc.GetConfig();
        
        [Route("upload")]
        public IActionResult List()
        {
            if (IsEnabled() == false)
            {
                return Content("上傳功能未啟用");
            }
            string uploadPath = config.Upload.Folder;
            DirectoryInfo di = new DirectoryInfo(uploadPath);
            List<FileDownloadInfoModel> files = new List<FileDownloadInfoModel>();
            DateTime now = DateTime.Now;
            foreach (var fi in di.GetFiles().OrderByDescending(t=>t.CreationTime))
            {
                TimeSpan deleteAfter = now.AddHours(72) - fi.CreationTime;
                if (deleteAfter.TotalHours < 0)
                {
                    fi.Delete();
                    continue;
                }
                files.Add(new FileDownloadInfoModel
                {
                    Title = fi.Name,
                    Id = fi.Name,
                    //Size =  fi.Length.ToString("N0"),
                    Size = Helper.GetReadableByteSize(fi.Length),
                    Time = fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    DeleteAfter = Helper.GetReadableTimeSpan(deleteAfter)
                });                
            }
            ViewBag.Items = files;
         
            return View();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="file_name"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("upload/post")]
        public dynamic PostFile(IFormFile file, bool is_start, bool is_end, string file_name)
        {
            if (IsEnabled() == false)
            {
                return Content("上傳功能未啟用");
            }
            try
            {
                string filePath = Path.Combine(config.Upload.TempFolder, file_name);
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
                if (fileLen > 1024 * 1024 * 200)
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                    }
                    return BadRequest("發生錯誤, 檔案超過限制 200 MB.");
                }
                if (is_end)
                {
                    string destFilePath = Path.Combine(config.Upload.Folder, file_name);
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
                        return BadRequest("檔案正在被使用，無法複蓋.");
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private bool IsEnabled()
        {
            if (config.Upload == null || config.Upload.Enabled == false
               || string.IsNullOrEmpty(config.Upload.Folder) || string.IsNullOrEmpty(config.Upload.TempFolder))
            {
                return false;
            }
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


        [HttpGet]
        [Route("upload/file/{*pathInfo}")]
        public dynamic GetUploadFile(string pathInfo)
        {
            if (IsEnabled() == false)
            {
                return Content("上傳功能未啟用");
            }
            string targetPath = Path.Combine(config.Upload.Folder, pathInfo);
            if (System.IO.File.Exists(targetPath) == false)
            {
                return NotFound();
            }
            return this.PhysicalFile(targetPath, "application/octet-stream");
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
