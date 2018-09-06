using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using WhatSearch.Services.Interfaces;

namespace WhatSearch.Services
{
    public class RssService : IRssService
    {
        public string GetFolderRss(string targetPath)
        {
            XDocument doc = new XDocument();
            XElement rss = new XElement("rss");
            rss.Add(new XAttribute("version", "2.0"));
            doc.Add(rss);

            XElement channel = new XElement("channel",
                new XElement("title"),
                new XElement("description"),
                new XElement("link"),
                new XElement("lastBuildDate"),
                new XElement("pubDate"),
                new XElement("ttl")
                );
            rss.Add(channel);
            DirectoryInfo di = null;
            if (string.IsNullOrEmpty(targetPath) == false)
            {
                di = new DirectoryInfo(targetPath);
            }
            if (di != null && di.Exists)
            {
                channel.Element("title").Value = di.Name;
                channel.Element("lastBuildDate").Value = di.CreationTime.ToString("r");
                channel.Element("pubDate").Value = di.LastWriteTime.ToString("r");
                foreach (var fi in di.GetFiles("*.*", SearchOption.AllDirectories)
                    .OrderByDescending(f => f.LastWriteTime).Take(30))
                {
                    if (fi.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }
                    XElement item = new XElement("item",
                        new XElement("title"),
                        new XElement("description"),
                        new XElement("link"),
                        new XElement("guid"),
                        new XElement("pubDate")
                        );

                    item.Element("title").Value = string.Format("【{0}】{1}", fi.Directory.Name, fi.Name);
                    item.Element("guid").Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(fi.Name)).GetHashCode() + "-" + fi.Length;
                    item.Element("pubDate").Value = fi.CreationTime.ToString("r");
                    channel.Add(item);
                }
            }
            else
            {
                string targetName = Path.GetDirectoryName(targetPath);
                channel.Element("title").Value = targetName + " 不存在";
                channel.Element("lastBuildDate").Value = DateTime.Now.ToString("r");
                channel.Element("pubDate").Value = DateTime.Now.ToString("r");
            }

            return doc.ToString();
        }
    }
}
