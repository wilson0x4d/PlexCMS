using Plex.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;

namespace Plex.WebSite.Areas.PlexAdmin.Controllers
{
    /* TODO: 
     * 
     * [workaround implemented] 1. there is no support for @model, @using nor @inherits directives
     * 
     * [workaround implemented] 2. 'body' should not necessarily have to be at the foot of the document, in default MVC pages it is typically located above "@section script {}" blocks. we need to accomodate this somehow.
     * 
     * [workaround implemented] 3. detecting @{ and detecting "the first '}\r\n'" can misbehave in several locations (where @{ follows a @section, for example)
     * 
     * Page 'text' is now normalized in the following ways (to workaround the above issues):
     * - addressed several bugs by normalizing all newlines to Environment.NewLine. your existing tools can continue butchering newlines if they need to.
     * - directives are now pushed to the top of the document in the order they appear
     * - non-body directives (such as @model) are always pushed above bodied directives (such as @section) so they appear first
     * - the body now returns all @{...} blocks (inluding the block referred to in other comments as the 'first code block')
     * 
     * * 4. A simple cshtml parser needs to be written/referenced
     *  Parse Directives (@model, @inherits, @using, @section, etc)
     *  Parse 'First' Code Block (e.g. @{...})
     *  Parse Body Block as the first 'non-code, non-whitespace' text line.
     *  Add support for detecting if a @section is managed or hand-edited, translate to user via tooling change (hand edited sections should appear as a raw textarea with no support for modules)
     * 
     * */
    [Authorize(Roles = "plx:admin")]
    public class PageController : ApiController
    {
        public Regex LayoutRegex { get; set; }
        public Regex PageSectionRegex { get; set; }
        public Regex PageModuleRegex { get; set; }
        public Regex TitleRegex { get; set; }

        public ControllerController Controllers { get; set; }
        public LayoutController Layouts { get; set; }

        private string viewsFilepath;

        public PageController()
        {
            LayoutRegex = new Regex(@"^\s*Layout = "".*/_(?<layout>[^_\.]*)\.cshtml"";" + Environment.NewLine, RegexOptions.Compiled | RegexOptions.Multiline);
            PageSectionRegex = new Regex(@"@section[\s]*(?<section>[^\s]*)[\s\{]*(?<modules>[^\}]*)\}", RegexOptions.Compiled);
            PageModuleRegex = new Regex(@"Html.Partial\(""Modules/_(?<module>[^""]*)"".*\)", RegexOptions.Compiled);
            TitleRegex = new Regex(@"^\s*ViewBag\.Title\s*=\s*""(?<title>[^""]*)""\s*;" + Environment.NewLine, RegexOptions.Compiled | RegexOptions.Multiline);
            Controllers = new ControllerController();
            Layouts = new LayoutController();
            viewsFilepath = Path.GetFullPath(HttpRuntime.BinDirectory + @"..\Views\");
        }

        /// <summary>
        /// Gets an index of all pages.
        /// </summary>
        /// <returns></returns>
        [ActionName("index")]
        public IEnumerable<PageInfo> Index()
        {
            return Controllers.Index()
                .SelectMany(controller =>
                {
                    var controllerPath = Path.Combine(viewsFilepath, controller.ID);
                    return Directory.Exists(controllerPath)
                        ? Directory.GetFiles(controllerPath, "*.cshtml")
                            .Where(pagePath => !Path.GetFileName(pagePath).StartsWith("_"))
                            .Select(pagePath =>
                            {
                                var i = 0;

                                var pageID = Path.GetFileName(pagePath).Replace(".cshtml", "");
                                var text = GetTextFromFile(pagePath);

                                var layout = default(LayoutInfo);
                                if (!TryGetLayoutFromText(text, out layout))
                                {
                                    layout = Layouts.GetDefault();
                                }

                                var title = default(string);
                                {
                                    var match = TitleRegex.Match(text);
                                    if (match != null && match.Success)
                                    {
                                        title = match.Groups[1].Value;
                                    }
                                }

                                var sections = layout.Sections
                                    .Select(section =>
                                    {
                                        var matches = PageSectionRegex.Matches(text).OfType<Match>();
                                        var modules = default(IEnumerable<PageModuleInfo>);
                                        var m = 0;
                                        foreach (var match in matches)
                                        {
                                            modules = PageModuleRegex.Matches(match.Groups[2].Value).OfType<Match>()
                                                .Select(match2 => new PageModuleInfo
                                                {
                                                    ID = match2.Groups[1].Value,
                                                    ControllerID = controller.ID,
                                                    PageID = pageID,
                                                    SectionID = match.Groups[1].Value,
                                                    Ordinal = m++
                                                });
                                            if (section.ID.Equals(match.Groups[1].Value, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                return CreatePageSectionInfoIndirect(controller, pageID, i++, section, modules.ToArray());
                                            }
                                        }
                                        return CreatePageSectionInfoIndirect(controller, pageID, i++, section, null);
                                    })
                                    .ToList();

                                var body = GetPageBodyFromText(text);

                                return new PageInfo
                                {
                                    ID = pageID,
                                    ControllerID = controller.ID,
                                    Title = title,
                                    LayoutID = layout.ID,
                                    Sections = sections,
                                    Body = body
                                };
                            })
                        : null;
                });
        }

        #region Page

        [ActionName("upsert")]
        public PageInfo PageUpsert(PageInfo page)
        {
            // verify or create containing folder
            var folder = Path.Combine(viewsFilepath, page.ControllerID);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // try create file
            var path = Path.Combine(folder, page.ID + ".cshtml");
            if (!File.Exists(path))
            {
                var sb = new StringBuilder("");
                WriteTextToPath(sb, path);
                // first time page create process
                TitleSet(page);
                LayoutSet(page);
                // TODO: use layout info to create new pages (e.g. apply 'Default Modules')
                if (page.Sections != null)
                {
                    foreach (var section in page.Sections)
                    {
                        SectionAdd(section);
                    }
                }
            }
            else
            {
                // TODO: populate page object with data from disk and return to the client
            }

            var result = Index()
                .Where(p => p.ControllerID.Equals(page.ControllerID, StringComparison.InvariantCultureIgnoreCase) && p.ID.Equals(page.ID, StringComparison.InvariantCultureIgnoreCase))
                .First();
            return result;
        }

        public PageInfo PageMove(PageInfo source, PageInfo dest)
        {
            var pathS = Path.Combine(viewsFilepath, source.ControllerID, source.ID + ".cshtml");
            var text = GetTextFromFile(pathS);
            var pathD = Path.Combine(viewsFilepath, dest.ControllerID, dest.ID + ".cshtml");
            WriteTextToPath(new StringBuilder(text), pathD);
            PageRemove(source);
            return dest;
        }

        public PageInfo PageRemove(PageInfo page)
        {
            var path = Path.Combine(viewsFilepath, page.ControllerID, page.ID + ".cshtml");
            File.Delete(path);
            return page;
        }

        #endregion
        #region Title

        [ActionName("TitleSet")]
        public PageInfo TitleSet(PageInfo page)
        {
            var titleString = !string.IsNullOrEmpty(page.Title)
                 ? "\tViewBag.Title = \"" + page.Title + "\";" + Environment.NewLine
                 : "";
            var path = Path.Combine(viewsFilepath, page.ControllerID, page.ID + ".cshtml");
            var text = GetTextFromFile(path);
            var match = TitleRegex.Match(text);
            if (match != null && match.Success)
            {
                text = text
                    .Replace(match.Value, titleString);
            }
            else
            {

                if (text.Contains("@{" + Environment.NewLine))
                {
                    // if: starts with @{...} then: inject layout into existing @{...}
                    text = text
                        .Insert(text.IndexOf("@{") + 2 + Environment.NewLine.Length, titleString);
                }
                else
                {
                    // else: inject new @{...} with layout
                    // TODO: should follow 'model' and other directives
                    text = text
                        .Insert(0, "@{" + Environment.NewLine + titleString + "}" + Environment.NewLine);
                }
            }

            WriteTextToPath(new StringBuilder(text), path);

            return page;
        }

        #endregion
        #region Layout

        [ActionName("LayoutSet")]
        public PageInfo LayoutSet(PageInfo page)
        {
            var layoutString = !string.IsNullOrEmpty(page.LayoutID)
                ? "\tLayout = \"~/Views/Shared/Layouts/_" + page.LayoutID + ".cshtml\";" + Environment.NewLine
                : "";
            var path = Path.Combine(viewsFilepath, page.ControllerID, page.ID + ".cshtml");
            var text = GetTextFromFile(path);
            var match = LayoutRegex.Match(text);
            if (match != null && match.Success)
            {
                text = text
                    .Replace(match.Value, layoutString);
            }
            else if (!string.IsNullOrEmpty(layoutString))
            {
                if (text.Contains("@{" + Environment.NewLine))
                {
                    // if: starts with @{...} then: inject layout into existing @{...}
                    text = text
                        .Insert(text.IndexOf("@{") + 2 + (Environment.NewLine.Length), layoutString);
                }
                else
                {
                    // else: inject new @{...} with layout
                    // TODO: compensate for @model directive which must appear on first line
                    text = text
                        .Insert(0, "@{" + Environment.NewLine + layoutString + "}" + Environment.NewLine);
                }
            }

            WriteTextToPath(new StringBuilder(text), path);

            return page;
        }

        [ActionName("LayoutClear")]
        public PageInfo LayoutClear(PageInfo page)
        {
            page.LayoutID = null;
            return LayoutSet(page);
        }


        #endregion
        #region Page Sections

        /// <summary>
        /// Adds a section to a page.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        [ActionName("SectionAdd")]
        public PageSectionInfo SectionAdd(PageSectionInfo section)
        {
            // resolve physical file and text contents
            var path = Path.Combine(viewsFilepath, section.ControllerID, section.PageID + ".cshtml");
            var text = GetTextFromFile(path);

            // generate new section text
            // TODO: this needs to include default modules
            var sectionText = "@section " + section.ID + " {" + Environment.NewLine + "}" + Environment.NewLine;

            // parse existing sections
            var matches = PageSectionRegex.Matches(text).OfType<Match>();

            var sb = new StringBuilder(text);
            // if no sections parsed, insert at head of document
            var insertAt = 0;
            if (matches.Count() == 0)
            {
                // if head of document contains @{...} then insert after closure
                // (convention-based decision, non-conventional code will cause this to misbehave)
                if (text.Contains("@{" + Environment.NewLine))
                {
                    insertAt = text.IndexOf(Environment.NewLine + "}" + Environment.NewLine) + 1 + (Environment.NewLine.Length * 2);
                }
            }
            else
            {
                // otherwise, rewrite lastmost match to contain lastmost match and also new section text
                var last = matches.Last();
                insertAt = last.Index + last.Length + Environment.NewLine.Length;
            }
            sb.Insert(insertAt, sectionText);

            // rewrite file
            WriteTextToPath(sb, path);

            return section;
        }

        /// <summary>
        /// Removes a section from a page.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        [ActionName("SectionRemove")]
        public PageSectionInfo SectionRemove(PageSectionInfo section)
        {
            var path = Path.Combine(viewsFilepath, section.ControllerID, section.PageID + ".cshtml");
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    var sectionText = GetSectionText(section.ID, text);

                    if (!string.IsNullOrEmpty(sectionText))
                    {
                        text = text
                            .Replace(sectionText + Environment.NewLine, "");

                        stream.Seek(0, SeekOrigin.Begin);
                        stream.SetLength(0);
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write(text);
                            writer.Flush();
                        }
                    }
                }
            }
            return section;
        }

        #endregion
        #region Page Modules

        public PageModuleInfo ModuleAdd(PageModuleInfo module)
        {
            var path = Path.Combine(viewsFilepath, module.ControllerID, module.PageID + ".cshtml");
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(module.SectionID, text.ToString());
                    var matches = PageModuleRegex.Matches(sectionText);
                    var modules = matches.OfType<Match>()
                        .Select(match => match.Groups[1].Value)
                        .ToList();
                    if (!modules.Contains(module.ID))
                    {
                        var list = modules.ToList();
                        list.Add(module.ID);
                        text = text.Replace(
                            sectionText,
                            string.Format(@"@section {0} {{{1}{2}}}",
                                module.SectionID,
                                Environment.NewLine,
                                ModuleListToString(list)));

                        stream.Seek(0, SeekOrigin.Begin);
                        stream.SetLength(0);
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write(text.ToString());
                            writer.Flush();
                        }
                    }
                }
            }
            return module;
        }

        private string ModuleListToString(List<string> list)
        {
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append("\t@Html.Partial(\"Modules/_")
                  .Append(item)
                  .AppendLine("\")");
            }
            return sb.ToString();
        }

        public PageModuleInfo ModuleUp(PageModuleInfo module)
        {
            var path = Path.Combine(viewsFilepath, module.ControllerID, module.PageID + ".cshtml");
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(module.SectionID, text.ToString());
                    var matches = PageModuleRegex.Matches(sectionText);
                    var modules = matches.OfType<Match>()
                        .Select(match => match.Groups[1].Value)
                        .ToList();
                    for (int i = 0; i < modules.Count; i++)
                    {
                        if (modules[i].Equals(module.ID))
                        {
                            if (i > 0)
                            {
                                modules[i] = modules[i - 1];
                                modules[i - 1] = module.ID;
                                {
                                    text = text.Replace(
                                        sectionText,
                                        string.Format(@"@section {0} {{{1}{2}}}",
                                            module.SectionID,
                                            Environment.NewLine,
                                            ModuleListToString(modules)));

                                    stream.Seek(0, SeekOrigin.Begin);
                                    stream.SetLength(0);
                                    using (var writer = new StreamWriter(stream))
                                    {
                                        writer.Write(text.ToString());
                                        writer.Flush();
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return module;
        }

        public PageModuleInfo ModuleDown(PageModuleInfo module)
        {
            var path = Path.Combine(viewsFilepath, module.ControllerID, module.PageID + ".cshtml");
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(module.SectionID, text.ToString());
                    var matches = PageModuleRegex.Matches(sectionText);
                    var modules = matches.OfType<Match>()
                        .Select(match => match.Groups[1].Value)
                        .Reverse()
                        .ToList();
                    for (int i = 0; i < modules.Count; i++)
                    {
                        if (modules[i].Equals(module.ID))
                        {
                            if (i > 0)
                            {
                                modules[i] = modules[i - 1];
                                modules[i - 1] = module.ID;
                                modules = modules
                                    .Reverse<string>()
                                    .ToList();
                                {
                                    text = text.Replace(
                                        sectionText,
                                        string.Format(@"@section {0} {{{1}{2}}}",
                                            module.SectionID,
                                            Environment.NewLine,
                                            ModuleListToString(modules)));

                                    stream.Seek(0, SeekOrigin.Begin);
                                    stream.SetLength(0);
                                    using (var writer = new StreamWriter(stream))
                                    {
                                        writer.Write(text.ToString());
                                        writer.Flush();
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return module;
        }

        public PageModuleInfo ModuleRemove(PageModuleInfo module)
        {
            var path = Path.Combine(viewsFilepath, module.ControllerID, module.PageID + ".cshtml");
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(module.SectionID, text.ToString());
                    var matches = PageModuleRegex.Matches(sectionText);
                    var modules = matches.OfType<Match>()
                        .Select(match => match.Groups[1].Value)
                        .Reverse()
                        .ToList();
                    if (modules.Remove(module.ID))
                    {
                        text = text.Replace(
                            sectionText,
                            string.Format(@"@section {0} {{{1}{2}}}",
                                module.SectionID,
                                Environment.NewLine,
                                ModuleListToString(modules)));
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.SetLength(0);
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write(text.ToString());
                            writer.Flush();
                        }
                    }
                }
            }
            return module;
        }

        #endregion
        #region Body

        [ActionName("BodySet")]
        public PageInfo BodySet(PageInfo page)
        {
            var path = Path.Combine(viewsFilepath, page.ControllerID, page.ID + ".cshtml");
            var text = GetTextFromFile(path);
            var body = GetPageBodyFromText(text);
            page.Body = page.Body.Replace(Environment.NewLine, "\n").Replace("\n", Environment.NewLine);
            if (!string.IsNullOrWhiteSpace(body))
            {
                text = text.Replace(body, page.Body);
            }
            else
            {
                text = text + page.Body;
            }

            WriteTextToPath(new StringBuilder(text), path);

            return page;
        }

        #endregion
        #region helpers - expecting all dependent code to be refactored, and these factored out of the controller

        private string GetPageBodyFromText(string text)
        {
            var bodyStart = 0;
            var tail = PageSectionRegex.Matches(text).OfType<Match>()
                .LastOrDefault();
            if (tail != null)
            {
                bodyStart = tail.Index + tail.Length + Environment.NewLine.Length;
            }
            else if (text.Contains("@{" + Environment.NewLine))
            {
                bodyStart = text.IndexOf(Environment.NewLine + "}" + Environment.NewLine) + (Environment.NewLine + "}" + Environment.NewLine).Length;
            }
            return (bodyStart >= text.Length)
                ? ""
                : text.Substring(bodyStart);
        }

        /// <summary>
        /// <para>Gets the current text for the section id provided.</para>
        /// <para>Does not include trailing/leading whitespace, current consumer code is written to expect this behavior.</para>
        /// </summary>
        /// <param name="sectionID"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private string GetSectionText(string sectionID, string text)
        {
            var match = PageSectionRegex
                .Matches(text).OfType<Match>()
                .FirstOrDefault(m => m.Groups[1].Value.Equals(sectionID));
            return match != null
                ? match.Value
                : null;
        }

        private void WriteTextToPath(StringBuilder text, string path)
        {
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(text.ToString());
                }
            }
        }

        private static PageSectionInfo CreatePageSectionInfoIndirect(ControllerInfo controller, string pageID, int ordinal, LayoutSectionInfo section, PageModuleInfo[] modules)
        {
            return new PageSectionInfo
            {
                ID = section.ID,
                ControllerID = controller.ID,
                IsPresent = modules != null,
                Modules = modules,
                PageID = pageID,
                Ordinal = ordinal
            };
        }

        private static string GetTextFromFile(string path)
        {
            var text = default(string);
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }
            }
            return NormalizeText(text);
        }

        private static string NormalizeText(string text)
        {
            // newlines for 'current' platform (this only matters during edit)
            text = text
                .Replace(Environment.NewLine, "\n")
                .Replace("\n", Environment.NewLine);

            var magic = "\x1b\x1b\x1b";
            var sb = new StringBuilder(magic + text, text.Length * 2);
            var matches = default(IEnumerable<Match>);

            // 1. move all "directive lines with bodies" to top
            var directiveLineRegexWithBody = new Regex(@"^@\w+[^\{\r\n]*\{[^\}]*\}\r*\n+", RegexOptions.Multiline | RegexOptions.Compiled);

            matches = directiveLineRegexWithBody.Matches(text).OfType<Match>().Reverse();
            foreach (var match in matches)
            {
                sb
                    .Replace(match.Value, "")
                    .Replace(magic, magic + match.Value + Environment.NewLine);
            }

            // 2. move all "directive lines without bodies" to top
            var directiveLineRegexNoBody = new Regex(@"^@\w+[^\{\r\n]*\r*\n+", RegexOptions.Multiline | RegexOptions.Compiled);

            matches = directiveLineRegexNoBody.Matches(text).OfType<Match>().Reverse();
            foreach (var match in matches)
            {
                sb
                    .Replace(match.Value, "")
                    .Replace(magic, magic + match.Value);
            }

            // 3. clean up the result
            sb
                .Replace(magic, "")
                .Replace("}" + Environment.NewLine + Environment.NewLine + "@{", "}" + Environment.NewLine + "@{");

            if (!sb.ToString().Equals(text))
            {
                System.Diagnostics.Trace.WriteLine("======= Normalized Text: ");
                System.Diagnostics.Trace.WriteLine(sb.ToString());
            }

            // 3. refactor code which needs to know where body starts are located (e.g. should not look for trailing '}' but should instead use a combination of regex matches to determine where 'body' start position is.
            return sb.ToString();
        }

        private bool TryGetLayoutFromText(string text, out LayoutInfo layout)
        {
            layout = default(LayoutInfo);
            var match = LayoutRegex.Match(text);
            if (match != null && match.Groups.Count > 0)
            {
                var layoutID = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(layoutID))
                {
                    layout = Layouts.Get(layoutID);
                }
            }
            return layout != null;
        }

        #endregion
    }
}