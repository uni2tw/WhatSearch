using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WhatSearch.Core;

namespace WhatSearch.Controllers
{
    //[Route("[controller]")]
    public class UploadController : Controller
    {

        const string uploadPath = "c:\\temp2";
        SystemConfig config = Ioc.GetConfig();
        
        [Route("upload")]
        public IActionResult List(string pathInfo)
        {
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
        public dynamic PostFile([FromBody]PostFileModel model)
        {
            try
            {
                int pos = model.fileData.IndexOf("base64,");
                if (pos != 1)
                {
                    string base64Data = model.fileData.Substring(pos + 7);
                    byte[] postData = System.Convert.FromBase64String(base64Data);
                    string filePath = System.IO.Path.Combine(uploadPath, model.fileName);
                    System.IO.File.WriteAllBytes(filePath, postData);
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
            string targetPath = System.IO.Path.Combine(uploadPath, pathInfo);
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
