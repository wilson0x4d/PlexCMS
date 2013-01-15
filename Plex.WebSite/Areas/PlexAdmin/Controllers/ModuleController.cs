using Plex.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Plex.WebSite.Areas.PlexAdmin.Controllers
{
    [Authorize(Roles = "plx:admin")]
    public class ModuleController : ApiController
    {
        private string moduleFilepath;

        public ModuleController()
        {
            moduleFilepath = Path.GetFullPath(HttpRuntime.BinDirectory + @"..\Views\Shared\Modules\");
        }

        [ActionName("index")]
        public IEnumerable<ModuleInfo> Index()
        {
            return Directory.GetFiles(moduleFilepath, "_*.cshtml")
                .Select(path =>
                    {
                        using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                return new ModuleInfo
                                {
                                    ID = Path.GetFileNameWithoutExtension(path).Remove(0, 1),
                                    Text = reader.ReadToEnd()
                                };
                            }
                        }
                    });
        }

        [ActionName("upsert")]
        public ModuleInfo ModuleUpsert(ModuleInfo module)
        {
            var filepath = Path.Combine(moduleFilepath, "_" + module.ID + ".cshtml");
            using (var stream = File.Open(filepath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(module.Text);
                    writer.Flush();
                }
            }
            return module;
        }

        [ActionName("remove")]
        public ModuleInfo ModuleRemove(ModuleInfo module)
        {
            var filepath = Path.Combine(moduleFilepath, "_" + module.ID + ".cshtml");
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
            return module;
        }
    }
}