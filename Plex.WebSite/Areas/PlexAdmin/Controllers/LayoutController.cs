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
    [Authorize(Roles = "plx:admin")]
    public class LayoutController : ApiController
    {
        public Regex LayoutSectionRegex { get; set; }
        public Regex DefaultModulesRegex { get; set; }

        private string layoutFilepath;

        public LayoutController()
        {
            layoutFilepath = Path.GetFullPath(HttpRuntime.BinDirectory + @"..\Views\Shared\Layouts\");
            LayoutSectionRegex = new Regex(@"RenderSection\(""(?<section>[^""]*)"".*\)", RegexOptions.Compiled);
            DefaultModulesRegex = new Regex(@"RenderSection\(""(?<section>[^""]*)"".*\)[^\<]*\<!--plx:modules:(?<modules>[^-]*)--\>", RegexOptions.Compiled);
            ViewStartLayoutMatchRegex = new Regex(@"Layout = "".*/_(?<layout>[^_\.]*)\.cshtml"";", RegexOptions.Compiled);
        }

        /// <summary>
        /// Gets an index of all layouts.
        /// </summary>
        /// <returns></returns>
        [ActionName("index")]
        public IEnumerable<LayoutInfo> Index()
        {
            return Directory.GetFiles(layoutFilepath, "_*.cshtml")
                .Select(path =>
                {
                    var text = default(string);
                    using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            text = reader.ReadToEnd();
                        }
                    }

                    var layoutID = Path.GetFileNameWithoutExtension(path).Remove(0, 1);

                    var i = 0;
                    var sections = LayoutSectionRegex.Matches(text).OfType<Match>()
                        .Select(match =>
                        {
                            var m = 0;
                            return new LayoutSectionInfo
                            {
                                ID = match.Groups[1].Value,
                                LayoutID = layoutID,
                                Ordinal = i++,
                                Modules = DefaultModulesRegex.Matches(text).OfType<Match>()
                                    .Where(match2 => match2.Groups["section"].Value.Equals(match.Groups[1].Value))
                                    .SelectMany(match2 => match2.Groups["modules"].Value.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                                    .Select(moduleID => new LayoutModuleInfo
                                    {
                                        ID = moduleID,
                                        LayoutID = layoutID,
                                        SectionID = match.Groups[1].Value,
                                        Ordinal = m++
                                    })
                            };
                        });

                    return new LayoutInfo
                    {
                        ID = layoutID,
                        Sections = sections
                    };
                });
        }

        public LayoutInfo Get(string layoutId)
        {
            return Index()
                .First(layout => layout.ID.Equals(layoutId, StringComparison.InvariantCultureIgnoreCase));
        }

        public Regex ViewStartLayoutMatchRegex { get; set; }

        /// <summary>
        /// Gets a LayoutInfo object representing the 'default' layout.
        /// </summary>
        /// <returns></returns>
        [ActionName("GetDefault"), HttpPost]
        public LayoutInfo GetDefault()
        {
            var path = Path.Combine(Path.GetFullPath(Path.Combine(layoutFilepath, @"..\..")), "_ViewStart.cshtml");
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    var layoutID = ViewStartLayoutMatchRegex.Matches(text).OfType<Match>()
                        .First()
                        .Groups[1].Value;
                    return Index().First(layout => layout.ID.Equals(layoutID, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        /// <summary>
        /// Sets the 'default' layout using info in a LayoutInfo object.
        /// </summary>
        /// <param name="layout"></param>
        [ActionName("SetDefault")]
        public LayoutInfo SetDefault(LayoutInfo layout)
        {
            var path = Path.Combine(Path.GetFullPath(Path.Combine(layoutFilepath, @"..\..")), "_ViewStart.cshtml");
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    text = ViewStartLayoutMatchRegex.Replace(text, @"Layout = ""~/Views/Shared/Layouts/_" + layout.ID + @".cshtml"";");
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(0);
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(text);
                        writer.Flush();
                    }
                }
            }
            return layout;
        }

        [ActionName("SectionAdd")]
        public LayoutSectionInfo SectionAdd(LayoutSectionInfo section)
        {
            // TODO: if section already exists, fail
            var layout = Get(section.LayoutID);
            var prior = layout.Sections.LastOrDefault();
            using (var stream = File.Open(Path.Combine(layoutFilepath, "_" + layout.ID + ".cshtml"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    text = text.Replace(
                        @"</body>",
                        Environment.NewLine + @"@RenderSection(""" + section.ID + @""". required: false)" +
                        Environment.NewLine + @"</body>");
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(0);
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(text);
                        writer.Flush();
                    }
                }
            }

            UpdateDependentPages(section.LayoutID, (pageController, page) =>
            {
                pageController.SectionAdd(new PageSectionInfo
                {
                    ControllerID = page.ControllerID,
                    ID = section.ID,
                    Ordinal = section.Ordinal,
                    PageID = page.ID,
                });
            });

            return section;
        }

        /// <summary>
        /// Helper Method to perform an action on pages which depend on a specified Layout
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="action"></param>
        private static void UpdateDependentPages(string layoutId, Action<PageController, PageInfo> action)
        {
            var pageController = new PageController();
            foreach (var page in pageController.Index())
            {
                if (page.LayoutID.Equals(layoutId, StringComparison.InvariantCultureIgnoreCase))
                {
                    action(pageController, page);
                }
            }
        }

        [ActionName("SectionUp")]
        public LayoutSectionInfo SectionUp(LayoutSectionInfo section)
        {
            var layout = Get(section.LayoutID);
            var prior = layout.Sections
                .TakeWhile(s => !s.ID.Equals(section.ID, StringComparison.InvariantCultureIgnoreCase))
                .Reverse()
                .FirstOrDefault();
            if (prior != null)
            {
                using (var stream = File.Open(Path.Combine(layoutFilepath, "_" + layout.ID + ".cshtml"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var text = new StringBuilder(reader.ReadToEnd());
                        var sectionText = GetSectionText(section.ID, text);
                        var priorText = GetSectionText(prior.ID, text);
                        var subst = Guid.NewGuid().ToString();
                        text = text
                            .Replace(priorText, subst)
                            .Replace(sectionText, priorText)
                            .Replace(subst, sectionText);

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
            // NOTE: this requires no page-level changes
            return section;
        }
        [ActionName("SectionDown")]
        public LayoutSectionInfo SectionDown(LayoutSectionInfo section)
        {
            var layout = Get(section.LayoutID);
            var next = layout.Sections
                .Reverse()
                .TakeWhile(s => !s.ID.Equals(section.ID, StringComparison.InvariantCultureIgnoreCase))
                .LastOrDefault();
            if (next != null)
            {
                SectionUp(next);
            }
            // NOTE: this requires no page-level changes
            return section;
        }
        [ActionName("SectionRemove")]
        public LayoutSectionInfo SectionRemove(LayoutSectionInfo section)
        {
            var layout = Get(section.LayoutID);
            using (var stream = File.Open(Path.Combine(layoutFilepath, "_" + layout.ID + ".cshtml"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(section.ID, text);

                    text = text
                        .Replace("@" + sectionText, "");

                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(0);
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(text.ToString());
                        writer.Flush();
                    }
                }
            }

            UpdateDependentPages(section.LayoutID, (pageController, page) =>
            {
                pageController.SectionRemove(new PageSectionInfo
                {
                    ControllerID = page.ControllerID,
                    ID = section.ID,
                    Ordinal = section.Ordinal,
                    PageID = page.ID,
                });
            });

            return section;
        }

        /// <summary>
        /// <para>Gets the current text for the section id provided.</para>
        /// <para>This call will return decorators, such as 'default modules', if they exist.</para>
        /// </summary>
        /// <param name="sectionID"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private string GetSectionText(string sectionID, StringBuilder text)
        {
            var layoutSections = LayoutSectionRegex.Matches(text.ToString()).OfType<Match>();
            var defaultModulesSections = DefaultModulesRegex.Matches(text.ToString()).OfType<Match>();

            var match = defaultModulesSections
                .FirstOrDefault(m => m.Groups[1].Value.Equals(sectionID));

            return (match != null)
                ? match.Groups[0].Value
                : layoutSections
                    .First(m => m.Groups[1].Value.Equals(sectionID))
                    .Groups[0].Value;
        }

        public LayoutModuleInfo ModuleAdd(LayoutModuleInfo module)
        {
            var layout = Get(module.LayoutID);
            using (var stream = File.Open(Path.Combine(layoutFilepath, "_" + layout.ID + ".cshtml"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(module.SectionID, text);
                    var matches = DefaultModulesRegex.Matches(sectionText);
                    var modules = matches.Count > 0
                        ? matches[0].Groups[2].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        : new string[0];
                    if (!modules.Contains(module.ID))
                    {
                        var list = modules.ToList();
                        list.Add(module.ID);
                        text = text.Replace(
                            sectionText,
                            string.Format(@"RenderSection(""{0}"", required: false){1}<!--plx:modules:{2}-->",
                                module.SectionID,
                                Environment.NewLine,
                                string.Join(",", list.ToArray())));

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
            UpdateDependentPages(layout.ID, (pageController, page) =>
            {
                pageController.ModuleAdd(new PageModuleInfo
                {
                    ControllerID = page.ControllerID,
                    ID = module.ID,
                    Ordinal = module.Ordinal,
                    PageID = page.ID,
                    SectionID = module.SectionID,
                });
            });
            return module;
        }

        public LayoutModuleInfo ModuleUp(LayoutModuleInfo module)
        {
            var layout = Get(module.LayoutID);
            using (var stream = File.Open(Path.Combine(layoutFilepath, "_" + layout.ID + ".cshtml"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(module.SectionID, text);
                    var modules = DefaultModulesRegex.Matches(sectionText)[0].Groups[2].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                    for (int i = 0; i < modules.Count; i++)
                    {
                        if (modules[i].Equals(module.ID))
                        {
                            if (i > 0)
                            {
                                modules[i] = modules[i - 1];
                                modules[i - 1] = module.ID;
                            }
                            break;
                        }
                    }
                    text = text.Replace(
                        sectionText,
                        string.Format(@"RenderSection(""{0}"", required: false){1}<!--plx:modules:{2}-->",
                            module.SectionID,
                            Environment.NewLine,
                            string.Join(",", modules)));

                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(0);
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(text.ToString());
                        writer.Flush();
                    }
                }
            }

            UpdateDependentPages(layout.ID, (pageController, page) =>
            {
                pageController.ModuleUp(new PageModuleInfo
                {
                    ControllerID = page.ControllerID,
                    ID = module.ID,
                    Ordinal = module.Ordinal,
                    PageID = page.ID,
                    SectionID = module.SectionID,
                });
            });

            return module;
        }

        public LayoutModuleInfo ModuleDown(LayoutModuleInfo module)
        {
            var layout = Get(module.LayoutID);
            using (var stream = File.Open(Path.Combine(layoutFilepath, "_" + layout.ID + ".cshtml"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(module.SectionID, text);
                    var modules = DefaultModulesRegex.Matches(sectionText)[0].Groups[2].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
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
                            }
                            break;
                        }
                    }
                    modules = modules
                        .Reverse<string>()
                        .ToList();
                    text = text.Replace(
                        sectionText,
                        string.Format(@"RenderSection(""{0}"", required: false){1}<!--plx:modules:{2}-->",
                            module.SectionID,
                            Environment.NewLine,
                            string.Join(",", modules)));

                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(0);
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(text.ToString());
                        writer.Flush();
                    }
                }
            }

            UpdateDependentPages(layout.ID, (pageController, page) =>
            {
                pageController.ModuleDown(new PageModuleInfo
                {
                    ControllerID = page.ControllerID,
                    ID = module.ID,
                    Ordinal = module.Ordinal,
                    PageID = page.ID,
                    SectionID = module.SectionID,
                });
            });

            return module;
        }

        public LayoutModuleInfo ModuleRemove(LayoutModuleInfo module)
        {
            var layout = Get(module.LayoutID);
            using (var stream = File.Open(Path.Combine(layoutFilepath, "_" + layout.ID + ".cshtml"), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = new StringBuilder(reader.ReadToEnd());
                    var sectionText = GetSectionText(module.SectionID, text);
                    var modules = DefaultModulesRegex.Matches(sectionText)[0].Groups[2].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(m => !m.Equals(module.ID));
                    text = text.Replace(
                        sectionText,
                        string.Format(@"RenderSection(""{0}"", required: false){1}<!--plx:modules:{2}-->",
                            module.SectionID,
                            Environment.NewLine,
                            string.Join(",", modules)));

                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(0);
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(text.ToString());
                        writer.Flush();
                    }
                }
            }

            UpdateDependentPages(layout.ID, (pageController, page) =>
            {
                pageController.ModuleRemove(new PageModuleInfo
                {
                    ControllerID = page.ControllerID,
                    ID = module.ID,
                    Ordinal = module.Ordinal,
                    PageID = page.ID,
                    SectionID = module.SectionID,
                });
            });

            return module;
        }
    }
}