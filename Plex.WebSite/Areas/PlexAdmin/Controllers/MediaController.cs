using Plex.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Plex.WebSite.Areas.PlexAdmin.Controllers
{
    [Authorize(Roles="plx:admin")]
    public class MediaController : ApiController
    {
        public string MediaFilepath { get; set; }

        public MediaController()
        {
            MediaFilepath = Path.GetFullPath(HttpRuntime.BinDirectory + @"..\Content\");
        }

        [ActionName("index")]
        public IEnumerable<MediaInfo> Index()
        {
            return Directory.GetFiles(MediaFilepath, "*.*", SearchOption.AllDirectories)
                .Select(filepath =>
                {
                    FileInfo fileInfo = new FileInfo(filepath);
                    var folderName = fileInfo.DirectoryName.Replace(MediaFilepath.Trim('\\'), "Content").Replace("\\", "/").Trim('/');
                    var media = new MediaInfo
                    {
                        Length = (int)fileInfo.Length,
                        FileName = fileInfo.Name,
                        FolderName = folderName,
                    };
                    return media;
                });
        }


        [ActionName("upload")]
        public Task<IEnumerable<MediaInfo>> MediaUpload()
        {
            // TODO: need support for folder selection at cllient
            var files = new List<MediaInfo>();

            if (Request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MediaControllerMultipartFormDataStreamProvider(MediaFilepath);
                var task = Request.Content.ReadAsMultipartAsync(streamProvider)
                    .ContinueWith<IEnumerable<MediaInfo>>(t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            throw new HttpResponseException(HttpStatusCode.InternalServerError);
                        }
                        return t.Result.FileData
                            .Select(mfd =>
                                {
                                    var folderName = Path.GetDirectoryName(mfd.LocalFileName).Trim(Path.DirectorySeparatorChar).Replace(MediaFilepath.Trim(Path.DirectorySeparatorChar), "Content\\").Replace("\\", "/").Trim('/');
                                    var fileName = Path.GetFileName(mfd.LocalFileName);
                                    var fileInfo = new FileInfo(mfd.LocalFileName);
                                    var length = (int)fileInfo.Length;
                                    return new MediaInfo
                                    {
                                        FolderName = folderName,
                                        FileName = fileName,
                                        Length = length
                                    };
                                })
                                .ToArray();
                    });
                return task;
            }
            else
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotAcceptable, "This request is not properly formatted"));
            }
        }

        [ActionName("remove")]
        public void MediaRemove(MediaInfo media)
        {
            var path = Path.Combine(MediaFilepath.Replace("Content", "").Trim('\\'), media.FolderName, media.FileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public class MediaControllerMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public MediaControllerMultipartFormDataStreamProvider(string path)
            : base(path)
        { }

        public override string GetLocalFileName(System.Net.Http.Headers.HttpContentHeaders headers)
        {
            var name = !string.IsNullOrWhiteSpace(headers.ContentDisposition.FileName) 
                ? headers.ContentDisposition.FileName 
                : "NoName";
            return name.Replace("\"", string.Empty); //this is here because Chrome submits files in quotation marks which get treated as part of the filename and get escaped
        }
    }
}
