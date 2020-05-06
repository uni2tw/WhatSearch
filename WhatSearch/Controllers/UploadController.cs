using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WhatSearch.Core;

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
            foreach(var fi in di.GetFiles().OrderByDescending(t=>t.CreationTime))
            {
                files.Add(new FileDownloadInfoModel
                {
                    Title = fi.Name,
                    Id = fi.Name,
                    Size =  fi.Length.ToString("N0"),
                    Time = fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            ViewBag.Items = files;
         
            return View();
        }

        [HttpPost]
        [Route("upload/post")]
        [RequestFormLimits(BufferBodyLengthLimit = 209715200, MultipartBodyLengthLimit = 209715200)]
        [RequestSizeLimit(209715200)]
        public dynamic PostFile(IFormFile file, string fileName)
        {
            if (config.Upload == null || config.Upload.Enabled == false || string.IsNullOrEmpty(config.Upload.Folder))
            {
                return Content("上傳功能未啟用");
            }
            try
            {
                string filePath = System.IO.Path.Combine(config.Upload.Folder, fileName);
                FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                file.CopyTo(fs);
                fs.Close();
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
        }

        public class PostFileModel
        {
            public string fileData { get; set; }
            public string fileName { get; set; }
        }
    }
}
