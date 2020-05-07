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
            if (config.Upload == null || config.Upload.Enabled == false || string.IsNullOrEmpty(config.Upload.Folder))
            {
                return Content("上傳功能未啟用");
            }
            if (Directory.Exists(config.Upload.Folder))
            {
                Directory.CreateDirectory(config.Upload.Folder);
            }
            string uploadPath = config.Upload.Folder;
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(uploadPath);
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

        [HttpPost]
        [Route("upload/post")]
        public dynamic PostFile(IFormFile file, int state , string file_name)
        {
            if (config.Upload == null || config.Upload.Enabled == false || string.IsNullOrEmpty(config.Upload.Folder))
            {
                return Content("上傳功能未啟用");
            }
            try
            {
                string filePath = System.IO.Path.Combine(config.Upload.Folder, file_name);
                FileStream fs;
                if (state == 0)
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
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("upload/file/{*pathInfo}")]
        public dynamic GetUploadFile(string pathInfo)
        {
            if (config.Upload == null || config.Upload.Enabled == false || string.IsNullOrEmpty(config.Upload.Folder))
            {
                return Content("上傳功能未啟用");
            }
            string targetPath = System.IO.Path.Combine(config.Upload.Folder, pathInfo);
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

        public class PostFileModel
        {
            public string fileData { get; set; }
            public string fileName { get; set; }
        }

    }
}
