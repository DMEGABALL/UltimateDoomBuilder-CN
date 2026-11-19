// =====================================================================
//  ManualBuilder —— 把 src\help\zh 的参考手册打包成单文件手机版 HTML
//
//  用法: ManualBuilder.exe <手册目录> <输出HTML>
//  默认: F:\项目\udb\src\help\zh  ->  F:\项目\udb\docs\index.html
//
//  特性:
//   - 解析 Contents.hhc 生成目录树
//   - 所有页面内联到单个 HTML，图片转 base64（超过 1000px 的按比例缩小）
//   - 响应式布局：手机抽屉式目录、表格横向滚动、图片自适应
//   - 支持 #/页面@锚点 路由、上一页/下一页、目录搜索、记住上次阅读位置
// =====================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class ManualBuilder
{
    static string Root;
    static string OutFile;
    static readonly Dictionary<string, string> ImageCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> PageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static long OriginalImageBytes;
    static long OutputImageBytes;

    class TocNode
    {
        public string Name = "";
        public string Local = "";
        public readonly List<TocNode> Children = new List<TocNode>();
    }

    static void Main(string[] args)
    {
        Root = args.Length > 0 ? args[0] : @"F:\项目\udb\src\help\zh";
        OutFile = args.Length > 1 ? args[1] : @"F:\项目\udb\docs\index.html";

        if (!Directory.Exists(Root))
        {
            Console.WriteLine("手册目录不存在: " + Root);
            Environment.Exit(1);
        }

        // ---------- 1. 目录树 ----------
        string hhc = Path.Combine(Root, "Contents.hhc");
        List<TocNode> toc = File.Exists(hhc) ? ParseToc(hhc) : new List<TocNode>();

        // 展平，得到唯一页面顺序
        var order = new List<string>();
        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CollectPages(toc, order, titles);
        foreach (string page in AllHtmlPages())
        {
            if (!order.Contains(page)) order.Add(page);
        }
        foreach (string page in order) PageIds.Add(PageId(page));

        // ---------- 2. 逐页生成 ----------
        var sb = new StringBuilder(4 * 1024 * 1024);
        foreach (string rel in order)
        {
            string title;
            titles.TryGetValue(rel, out title);
            sb.Append(BuildPage(rel, title));
        }

        // ---------- 3. 目录 HTML ----------
        var tocHtml = new StringBuilder();
        RenderToc(toc, tocHtml, titles);
        if (tocHtml.Length == 0)
        {
            foreach (string page in order)
                tocHtml.AppendFormat("<li><a href=\"#/{0}\">{1}</a></li>", PageId(page), Esc(TitleOf(page, titles)));
        }

        // ---------- 4. 拼装 ----------
        string first = PageId(order[0]);
        string html = Template
            .Replace("{{TOC}}", tocHtml.ToString())
            .Replace("{{PAGES}}", sb.ToString())
            .Replace("{{FIRST}}", first)
            .Replace("{{COUNT}}", order.Count.ToString())
            .Replace("{{ORDER}}", string.Join(",", order.Select(PageId)));

        Directory.CreateDirectory(Path.GetDirectoryName(OutFile));
        File.WriteAllText(OutFile, html, new UTF8Encoding(false));

        Console.WriteLine("pages   : " + order.Count);
        Console.WriteLine("images  : " + ImageCache.Count + "  (原图 " + (OriginalImageBytes / 1024 / 1024) + " MB -> " + (OutputImageBytes / 1024 / 1024) + " MB)");
        Console.WriteLine("output  : " + OutFile + "  (" + (new FileInfo(OutFile).Length / 1024.0 / 1024.0).ToString("0.00") + " MB)");
    }

    // ------------------------------------------------------------------
    //  目录解析
    // ------------------------------------------------------------------
    static List<TocNode> ParseToc(string path)
    {
        // Contents.hhc 以系统 ANSI（GBK）保存
        string text = File.ReadAllText(path, Encoding.GetEncoding(936));
        var root = new List<TocNode>();
        var stack = new Stack<List<TocNode>>();
        stack.Push(root);

        var rx = new Regex(@"(?is)<UL>|</UL>|<OBJECT\b.*?</OBJECT>");
        foreach (Match m in rx.Matches(text))
        {
            string token = m.Value;
            if (token.Equals("<UL>", StringComparison.OrdinalIgnoreCase))
            {
                stack.Push(new List<TocNode>());
            }
            else if (token.Equals("</UL>", StringComparison.OrdinalIgnoreCase))
            {
                if (stack.Count <= 1) continue;
                var children = stack.Pop();
                var parentList = stack.Peek();
                if (parentList.Count > 0 && children.Count > 0)
                    parentList[parentList.Count - 1].Children.AddRange(children);
                else
                    parentList.AddRange(children);
            }
            else
            {
                string name = Attr(token, "Name");
                string local = Attr(token, "Local");
                if (string.IsNullOrEmpty(name)) continue;
                stack.Peek().Add(new TocNode { Name = Clean(name), Local = local ?? "" });
            }
        }
        return root;
    }

    static string Attr(string s, string name)
    {
        var m = Regex.Match(s, "name=\"" + name + "\"\\s+value=\"([^\"]*)\"", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    static string Clean(string s)
    {
        return s.Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Trim();
    }

    static void CollectPages(List<TocNode> nodes, List<string> order, Dictionary<string, string> titles)
    {
        foreach (var n in nodes)
        {
            if (!string.IsNullOrEmpty(n.Local) && n.Local.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                string rel = n.Local.Replace('\\', '/');
                if (!order.Contains(rel))
                {
                    order.Add(rel);
                    if (!titles.ContainsKey(rel)) titles[rel] = n.Name;
                }
            }
            CollectPages(n.Children, order, titles);
        }
    }

    static void RenderToc(List<TocNode> nodes, StringBuilder sb, Dictionary<string, string> titles)
    {
        if (nodes.Count == 0) return;
        sb.Append("<ul>");
        foreach (var n in nodes)
        {
            sb.Append("<li>");
            bool hasPage = !string.IsNullOrEmpty(n.Local) && n.Local.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
            if (hasPage)
                sb.AppendFormat("<a class=\"toc-link\" href=\"#/{0}\">{1}</a>", PageId(n.Local), Esc(n.Name));
            else
                sb.AppendFormat("<span class=\"toc-group\">{0}</span>", Esc(n.Name));

            if (n.Children.Count > 0)
            {
                sb.Append("<button class=\"toc-toggle\" type=\"button\" aria-label=\"展开/收起\"></button>");
                sb.Append("<div class=\"toc-children\">");
                RenderToc(n.Children, sb, titles);
                sb.Append("</div>");
            }
            sb.Append("</li>");
        }
        sb.Append("</ul>");
    }

    // ------------------------------------------------------------------
    //  页面生成
    // ------------------------------------------------------------------
    static string BuildPage(string rel, string title)
    {
        string full = Path.Combine(Root, rel.Replace('/', '\\'));
        if (!File.Exists(full)) return "";
        string html = File.ReadAllText(full, Encoding.UTF8);
        string dir = Path.GetDirectoryName(full);

        string body = ExtractContents(html);
        if (body == null)
        {
            var bm = Regex.Match(html, @"(?is)<body[^>]*>(.*)</body>");
            body = bm.Success ? bm.Groups[1].Value : html;
        }

        // 标题
        string pageTitle = title;
        if (string.IsNullOrEmpty(pageTitle))
            pageTitle = TitleOf(rel, null);

        // iframe -> 内联内容
        body = Regex.Replace(body, @"(?is)<iframe\b[^>]*src=""([^""]+)""[^>]*>\s*</iframe>",
            m => InlineFrame(m.Groups[1].Value, dir));

        // 去掉剧本、折叠按钮；展开被折叠的截图
        body = Regex.Replace(body, @"(?is)<script\b.*?</script>", "");
        body = Regex.Replace(body, @"(?is)<input\b[^>]*spoilerbutton[^>]*>", "");
        body = Regex.Replace(body, @"display\s*:\s*none\s*;?", "");

        // 图片内联（去掉空的 src）
        body = Regex.Replace(body, @"(?is)<img\b[^>]*src\s*=\s*""""[^>]*>", "");
        body = Regex.Replace(body, @"(<img\b[^>]*?)src=""([^""]+)""", m =>
        {
            string src = m.Groups[2].Value;
            string uri = ResolveImage(src, dir);
            return uri == null ? m.Value : m.Groups[1].Value + "src=\"" + uri + "\" loading=\"lazy\" decoding=\"async\"";
        });

        // 链接重写
        string selfId = PageId(rel);
        body = Regex.Replace(body, @"href=""([^""]+)""", m => RewriteLink(m.Groups[1].Value, selfId, dir));

        // 修正未闭合标签（原版手册中部分页面缺少 </table>，会把后续页面吞进表格）
        string leftover;
        body = BalanceTags(body, out leftover);
        if (leftover.Length > 0)
            Console.WriteLine("  补全标签: " + rel + " -> " + leftover);

        return "<section class=\"page\" id=\"p-" + selfId + "\"><h1 class=\"page-title\">" + Esc(pageTitle) + "</h1>" + body + "</section>\n";
    }

    static readonly HashSet<string> VoidTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "area","base","br","col","embed","hr","img","input","link","meta","param","source","track","wbr"
    };

    // 逐页平衡标签：丢弃多余结束标签，补全缺失的结束标签
    static string BalanceTags(string html, out string leftoverTags)
    {
        var stack = new List<string>();
        var sb = new StringBuilder(html.Length + 64);
        var rx = new Regex(@"<[^>]*>");
        int last = 0;

        foreach (Match m in rx.Matches(html))
        {
            sb.Append(html, last, m.Index - last);
            last = m.Index + m.Length;

            string tag = m.Value;
            if (tag.StartsWith("<!--"))
            {
                sb.Append(tag);
                continue;
            }

            var mm = Regex.Match(tag, @"^<\s*(/?)\s*([a-zA-Z][a-zA-Z0-9]*)");
            if (!mm.Success)
            {
                sb.Append(tag);
                continue;
            }

            bool closing = mm.Groups[1].Value == "/";
            string name = mm.Groups[2].Value.ToLowerInvariant();

            if (!closing)
            {
                sb.Append(tag);
                bool selfClosed = tag.TrimEnd().EndsWith("/>") || VoidTags.Contains(name);
                if (!selfClosed) stack.Add(name);
            }
            else
            {
                int idx = stack.LastIndexOf(name);
                if (idx < 0) continue; // 多余的结束标签，直接丢弃
                while (stack.Count - 1 > idx)
                {
                    sb.Append("</" + stack[stack.Count - 1] + ">");
                    stack.RemoveAt(stack.Count - 1);
                }
                sb.Append(tag);
                stack.RemoveAt(stack.Count - 1);
            }
        }

        sb.Append(html, last, html.Length - last);

        var missing = new List<string>();
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            sb.Append("</" + stack[i] + ">");
            missing.Add(stack[i]);
        }
        leftoverTags = string.Join(",", missing);
        return sb.ToString();
    }

    static string InlineFrame(string src, string dir)
    {
        try
        {
            if (src.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return "";
            string full = Path.GetFullPath(Path.Combine(dir, src.Replace('/', '\\')));
            if (!File.Exists(full)) return "";
            string h = File.ReadAllText(full, Encoding.UTF8);
            return ExtractContents(h) ?? "";
        }
        catch { return ""; }
    }

    static string ResolveImage(string src, string dir)
    {
        try
        {
            if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return src;
            if (src.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return null;
            string clean = Uri.UnescapeDataString(src.Split('?')[0].Split('#')[0]);
            string full = Path.GetFullPath(Path.Combine(dir, clean.Replace('/', '\\')));
            if (!File.Exists(full)) return null;
            if (ImageCache.ContainsKey(full)) return ImageCache[full];

            string ext = Path.GetExtension(full).ToLowerInvariant();
            byte[] raw = File.ReadAllBytes(full);
            OriginalImageBytes += raw.Length;
            byte[] outBytes = raw;
            string mime;

            if (ext == ".gif" || ext == ".svg" || ext == ".ico")
            {
                mime = ext == ".gif" ? "image/gif" : ext == ".svg" ? "image/svg+xml" : "image/x-icon";
            }
            else
            {
                using (var fs = new FileStream(full, FileMode.Open, FileAccess.Read))
                using (var img = Image.FromStream(fs))
                {
                    const int maxW = 1000;
                    Bitmap scaled = null;
                    try
                    {
                        Image target = img;
                        if (img.Width > maxW)
                        {
                            int w = maxW;
                            int h = (int)Math.Round(img.Height * (double)maxW / img.Width);
                            scaled = new Bitmap(w, h);
                            using (var g = Graphics.FromImage(scaled))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.DrawImage(img, 0, 0, w, h);
                            }
                            target = scaled;
                        }
                        using (var ms = new MemoryStream())
                        {
                            if (ext == ".png")
                            {
                                target.Save(ms, ImageFormat.Png);
                                mime = "image/png";
                            }
                            else
                            {
                                var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                                var ps = new EncoderParameters(1);
                                ps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 82L);
                                target.Save(ms, codec, ps);
                                mime = "image/jpeg";
                            }
                            outBytes = ms.ToArray();
                        }
                    }
                    finally
                    {
                        if (scaled != null) scaled.Dispose();
                    }
                }
            }

            OutputImageBytes += outBytes.Length;
            string uri = "data:" + mime + ";base64," + Convert.ToBase64String(outBytes);
            ImageCache[full] = uri;
            return uri;
        }
        catch { return null; }
    }

    static string RewriteLink(string href, string selfId, string dir)
    {
        if (href.StartsWith("#"))
        {
            string anchor = href.Substring(1);
            return "href=\"#/" + selfId + (anchor.Length > 0 ? "@" + anchor : "") + "\"";
        }
        if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase))
        {
            if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) return "href=\"#\"";
            return "href=\"" + href + "\" target=\"_blank\" rel=\"noopener\"";
        }

        string clean = href;
        string anchor2 = "";
        int h = clean.IndexOf('#');
        if (h >= 0) { anchor2 = clean.Substring(h + 1); clean = clean.Substring(0, h); }

        if (clean.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            string rel;
            if (clean.StartsWith("/"))
            {
                rel = clean.TrimStart('/');
            }
            else
            {
                rel = RelPath(dir, clean);
            }
            string id = PageId(rel);
            if (PageIds.Contains(id))
                return "href=\"#/" + id + (anchor2.Length > 0 ? "@" + anchor2 : "") + "\"";
            // 目标页面在原版手册中已不存在：去掉链接，避免死链
            return "data-broken-link=\"1\"";
        }
        return "href=\"" + href + "\"";
    }

    static string RelPath(string dir, string target)
    {
        try
        {
            string full = Path.GetFullPath(Path.Combine(dir, target.Replace('/', '\\')));
            string basePath = Path.GetFullPath(Root);
            if (!full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) return target;
            return full.Substring(basePath.Length).TrimStart('\\').Replace('\\', '/');
        }
        catch { return target; }
    }

    static string PageId(string rel)
    {
        string s = rel.Replace('\\', '/');
        if (s.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - 5);
        return s.Replace('/', '_').Replace('.', '_').Replace(' ', '_').ToLowerInvariant();
    }

    static string TitleOf(string rel, Dictionary<string, string> titles)
    {
        if (titles != null && titles.ContainsKey(rel)) return titles[rel];
        return Path.GetFileNameWithoutExtension(rel);
    }

    static IEnumerable<string> AllHtmlPages()
    {
        return Directory.GetFiles(Root, "*.html", SearchOption.AllDirectories)
            .Select(f => f.Substring(Root.Length).TrimStart('\\').Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
    }

    // 取 <div id="contents"> 的内部（处理嵌套 div）
    static string ExtractContents(string html)
    {
        int i = html.IndexOf("<div id=\"contents\"", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        int openEnd = html.IndexOf('>', i);
        if (openEnd < 0) return null;
        int depth = 1;
        int p = openEnd + 1;
        while (p < html.Length)
        {
            int nextOpen = html.IndexOf("<div", p, StringComparison.OrdinalIgnoreCase);
            int nextClose = html.IndexOf("</div>", p, StringComparison.OrdinalIgnoreCase);
            if (nextClose < 0) break;
            if (nextOpen >= 0 && nextOpen < nextClose)
            {
                depth++;
                p = nextOpen + 4;
            }
            else
            {
                depth--;
                if (depth == 0) return html.Substring(openEnd + 1, nextClose - openEnd - 1);
                p = nextClose + 6;
            }
        }
        return null;
    }

    static string Esc(string s)
    {
        if (s == null) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    // ------------------------------------------------------------------
    //  模板
    // ------------------------------------------------------------------
    const string Template = @"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1, viewport-fit=cover"">
<meta name=""theme-color"" content=""#1f2937"">
<title>Ultimate Doom Builder 参考手册（简体中文）</title>
<style>
:root{--bg:#f5f6f8;--card:#fff;--fg:#1f2328;--muted:#6b7280;--line:#e5e7eb;--accent:#2563eb;--bar:#1f2937;--barfg:#f9fafb}
*{box-sizing:border-box}
html{-webkit-text-size-adjust:100%}
body{margin:0;background:var(--bg);color:var(--fg);font:16px/1.75 -apple-system,BlinkMacSystemFont,'Segoe UI','Microsoft YaHei',sans-serif}
a{color:var(--accent);text-decoration:none}
a[data-broken-link]{color:inherit;cursor:default}
a:active{opacity:.7}
header.topbar{position:sticky;top:0;z-index:30;display:flex;align-items:center;gap:10px;background:var(--bar);color:var(--barfg);padding:10px 12px;padding-top:calc(10px + env(safe-area-inset-top))}
header .brand{font-weight:600;font-size:15px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;flex:1}
#menuBtn{background:none;border:0;color:var(--barfg);font-size:20px;line-height:1;padding:4px 6px;cursor:pointer}
#search{flex:0 0 40%;max-width:220px;padding:6px 10px;border-radius:8px;border:0;font-size:13px;background:#374151;color:#fff;outline:none}
#search::placeholder{color:#9ca3af}
#overlay{position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:35;display:none}
#overlay.on{display:block}
nav#toc{position:fixed;top:0;bottom:0;left:0;width:min(84vw,330px);background:var(--card);z-index:40;transform:translateX(-102%);transition:transform .22s ease;overflow:auto;-webkit-overflow-scrolling:touch;padding:12px 10px 40px;border-right:1px solid var(--line)}
nav#toc.open{transform:none}
nav#toc .toc-title{font-weight:700;font-size:14px;color:var(--muted);padding:6px 8px 10px;letter-spacing:.05em}
nav#toc ul{list-style:none;margin:0;padding-left:0}
nav#toc ul ul{padding-left:14px}
nav#toc li{position:relative}
nav#toc a,nav#toc .toc-group{display:block;padding:8px 10px;border-radius:8px;font-size:14.5px;line-height:1.5;color:var(--fg)}
nav#toc .toc-group{font-weight:600;color:var(--muted);font-size:13px;padding-bottom:4px}
nav#toc a:hover{background:#f3f4f6}
nav#toc a.active{background:#e8efff;color:#1d4ed8;font-weight:600}
nav#toc .toc-toggle{position:absolute;right:2px;top:4px;width:28px;height:28px;border:0;background:none;cursor:pointer}
nav#toc .toc-toggle::after{content:"""";display:block;width:7px;height:7px;margin:8px auto 0;border-right:2px solid #9ca3af;border-bottom:2px solid #9ca3af;transform:rotate(45deg);transition:transform .15s}
nav#toc li.collapsed>.toc-children{display:none}
nav#toc li.collapsed>.toc-toggle::after{transform:rotate(-45deg)}
main{max-width:820px;margin:0 auto;padding:14px 14px 90px}
section.page{background:var(--card);border-radius:14px;padding:16px 16px 22px;border:1px solid var(--line)}
section.page[hidden]{display:none}
h1.page-title{font-size:21px;line-height:1.4;margin:2px 0 16px;padding-bottom:10px;border-bottom:1px solid var(--line)}
.page h1{font-size:20px;margin:22px 0 10px}
.page h2{font-size:17.5px;margin:22px 0 8px;line-height:1.45}
.page h3{font-size:16px;margin:18px 0 6px}
.page p{margin:10px 0}
.page ul,.page ol{padding-left:22px;margin:10px 0}
.page li{margin:5px 0}
.page img{max-width:100%;height:auto;border-radius:8px;border:1px solid var(--line);margin:8px 0;display:block}
.page table{display:block;overflow-x:auto;-webkit-overflow-scrolling:touch;border-collapse:collapse;margin:12px 0;max-width:100%}
.page th,.page td{border:1px solid var(--line);padding:6px 9px;font-size:14px;vertical-align:top}
.page pre,.page code{font-family:Consolas,'Courier New',monospace}
.page pre{background:#f6f8fa;border:1px solid var(--line);border-radius:8px;padding:10px;overflow-x:auto;font-size:13px;line-height:1.5}
.page blockquote{margin:10px 0;padding:8px 12px;border-left:3px solid var(--accent);background:#f8fafc;color:#374151}
.page hr{border:0;border-top:1px solid var(--line);margin:18px 0}
.red{color:#dc2626}
.style1{color:#b91c1c;font-weight:600}
.style3{color:#15803d;font-weight:600}
.style7{color:#6d28d9}
.style9{color:#065f46}
footer.pager{position:fixed;left:0;right:0;bottom:0;z-index:25;display:flex;align-items:center;justify-content:space-between;gap:10px;background:rgba(255,255,255,.96);border-top:1px solid var(--line);padding:8px 12px;padding-bottom:calc(8px + env(safe-area-inset-bottom));backdrop-filter:blur(6px)}
footer.pager button{border:1px solid var(--line);background:#fff;border-radius:9px;padding:8px 14px;font-size:14px;color:var(--fg)}
footer.pager button:disabled{opacity:.35}
footer .pos{font-size:13px;color:var(--muted)}
@media(min-width:1000px){
  #menuBtn{display:none}
  header.topbar{margin-left:300px}
  nav#toc{transform:none;width:300px;border-right:1px solid var(--line);padding-top:14px}
  main{margin-left:300px;max-width:900px}
  #overlay{display:none!important}
  #search{max-width:260px;flex:0 0 260px}
}
</style>
</head>
<body>
<header class=""topbar"">
  <button id=""menuBtn"" type=""button"" aria-label=""目录"">☰</button>
  <div class=""brand"">UDB 参考手册</div>
  <input id=""search"" type=""search"" placeholder=""搜索页面…"" autocomplete=""off"">
</header>
<div id=""overlay""></div>
<nav id=""toc"" aria-label=""目录""><div class=""toc-title"">目录</div>{{TOC}}</nav>
<main id=""main"">
{{PAGES}}
</main>
<footer class=""pager"">
  <button id=""prev"" type=""button"">← 上一页</button>
  <span class=""pos"" id=""pos"">1 / {{COUNT}}</span>
  <button id=""next"" type=""button"">下一页 →</button>
</footer>
<script>
(function(){
  var ORDER = '{{ORDER}}'.split(',');
  var pages = {};
  document.querySelectorAll('section.page').forEach(function(p){ pages[p.id.slice(2)] = p; });
  var toc = document.getElementById('toc');
  var overlay = document.getElementById('overlay');
  var pos = document.getElementById('pos');
  var btnPrev = document.getElementById('prev');
  var btnNext = document.getElementById('next');
  var current = null;

  function closeToc(){ toc.classList.remove('open'); overlay.classList.remove('on'); }
  function openToc(){ toc.classList.add('open'); overlay.classList.add('on'); }
  document.getElementById('menuBtn').onclick = function(){ toc.classList.contains('open') ? closeToc() : openToc(); };
  overlay.onclick = closeToc;

  function parseHash(){
    var h = location.hash || '';
    if(h.indexOf('#/') !== 0) return { id: null, anchor: '' };
    h = h.substring(2);
    var at = h.indexOf('@');
    if(at >= 0) return { id: h.substring(0, at), anchor: h.substring(at + 1) };
    return { id: h, anchor: '' };
  }

  function show(id, anchor){
    if(!id || !pages[id]) id = ORDER[0];
    current = id;
    Object.keys(pages).forEach(function(k){ pages[k].hidden = (k !== id); });
    document.querySelectorAll('#toc a.toc-link').forEach(function(a){
      a.classList.toggle('active', a.getAttribute('href') === '#/' + id);
    });
    var idx = ORDER.indexOf(id);
    pos.textContent = (idx + 1) + ' / ' + ORDER.length;
    btnPrev.disabled = idx <= 0;
    btnNext.disabled = idx >= ORDER.length - 1;
    document.title = (pages[id].querySelector('.page-title') || {}).textContent + ' · UDB 参考手册';
    window.scrollTo(0, 0);
    if(anchor){
      var el = document.getElementById(anchor);
      if(el) el.scrollIntoView({ block: 'start' });
    }
    try { localStorage.setItem('udbman:last', id); } catch(e) {}
    closeToc();
  }

  function route(){ var r = parseHash(); show(r.id, r.anchor); }
  window.addEventListener('hashchange', route);

  btnPrev.onclick = function(){ var i = ORDER.indexOf(current); if(i > 0) location.hash = '#/' + ORDER[i-1]; };
  btnNext.onclick = function(){ var i = ORDER.indexOf(current); if(i >= 0 && i < ORDER.length-1) location.hash = '#/' + ORDER[i+1]; };

  // 目录折叠
  document.querySelectorAll('#toc .toc-toggle').forEach(function(b){
    b.onclick = function(e){
      e.preventDefault(); e.stopPropagation();
      b.parentNode.classList.toggle('collapsed');
    };
  });

  // 搜索
  var search = document.getElementById('search');
  search.addEventListener('input', function(){
    var q = search.value.trim().toLowerCase();
    document.querySelectorAll('#toc li').forEach(function(li){ li.style.display = ''; });
    if(!q) return;
    var links = document.querySelectorAll('#toc a.toc-link');
    links.forEach(function(a){ a.parentNode.style.display = 'none'; });
    links.forEach(function(a){
      if(a.textContent.toLowerCase().indexOf(q) >= 0){
        a.parentNode.style.display = '';
        var p = a.parentNode.parentNode;
        while(p && p !== toc){ if(p.tagName === 'LI'){ p.style.display = ''; p.classList.remove('collapsed'); } p = p.parentNode; }
      }
    });
    document.querySelectorAll('#toc li').forEach(function(li){
      if(li.style.display === 'none' && li.querySelector('a.toc-link')) return;
      if(li.querySelector('a.toc-link') && li.style.display === 'none') return;
    });
  });

  // 启动
  if(location.hash.indexOf('#/') === 0){
    route();
  } else {
    var last = null;
    try { last = localStorage.getItem('udbman:last'); } catch(e) {}
    location.hash = '#/' + (last && pages[last] ? last : '{{FIRST}}');
  }
})();
</script>
</body>
</html>";
}
