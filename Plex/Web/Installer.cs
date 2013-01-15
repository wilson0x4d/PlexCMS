using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Plex.Web
{
    public static class Installer
    {
        private static Regex namespaceMatchRegex = new Regex(@"namespace[\s]*(?<namespace>[^:\s]*)[\s{]*", RegexOptions.Compiled);

        public static void Install(string installPath, string projectPath)
        {
            var projectNamespace = GetProjectNamespace(projectPath, @"Global.asax.cs");

            // enumerate package contents looking for .cs files which need corrections
            var contentPath = Path.Combine(installPath, @"content");
            var files = new Stack<string>(
                    Directory.GetFiles(contentPath, "*.cs", SearchOption.AllDirectories)
                );

            // apply corrections to files
            while (files.Count > 0)
            {
                var filename = files
                    .Pop()
                    .Replace(contentPath, "");
                SetProjectNamespace(projectPath, filename, projectNamespace);
            }
        }

        private static string GetProjectNamespace(string projectPath, string filename)
        {
            var filepath = Path.Combine(projectPath, filename);
            if (File.Exists(filepath))
            {
                using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var text = reader.ReadToEnd();
                        var match = namespaceMatchRegex.Match(text);
                        if (match != null && match.Groups.Count > 0)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
            }
            return null;
        }

        private static void SetProjectNamespace(string projectPath, string filename, string projectNamespace)
        {
            var filepath = projectPath + filename;
            using (var stream = File.Open(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader
                        .ReadToEnd()
                        .Replace(@"Plex.WebSite", projectNamespace);
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(0);
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(text);
                        writer.Flush();
                    }
                }
            }
            Trace.WriteLine("Namespace Correction: " + filepath);
        }
    }
}
