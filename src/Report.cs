using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaceSweeper
{
    public static class Report
    {
        public static string BuildHtml(ScanResult r)
        {
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html><head><meta charset=\"utf-8\">");
            sb.Append("<title>SpaceSweeper report</title><style>");
            sb.Append("body{margin:0;background:#0f1115;color:#e8ecf3;font:14px/1.5 'Segoe UI',system-ui,sans-serif;padding:40px}");
            sb.Append("h1{font-size:26px;margin:0 0 4px}h2{font-size:16px;margin:34px 0 10px;color:#aab3c5}");
            sb.Append(".muted{color:#8a93a3;font-size:12px}");
            sb.Append(".cards{display:flex;gap:14px;margin:24px 0 8px;flex-wrap:wrap}");
            sb.Append(".card{background:#171a20;border:1px solid #2a2f38;border-radius:12px;padding:16px 20px;min-width:170px}");
            sb.Append(".card b{display:block;font-size:24px;color:#38d39f}");
            sb.Append(".card span{font-size:11px;letter-spacing:.08em;text-transform:uppercase;color:#8a93a3}");
            sb.Append("table{width:100%;border-collapse:collapse;font-size:12.5px;background:#14161b;border-radius:10px;overflow:hidden}");
            sb.Append("th{text-align:left;background:#1d2128;color:#8a93a3;font-size:11px;text-transform:uppercase;letter-spacing:.06em;padding:9px 12px}");
            sb.Append("td{padding:8px 12px;border-top:1px solid #22262e;color:#c9d1df}");
            sb.Append("td.num{text-align:right;color:#e8ecf3;white-space:nowrap}");
            sb.Append("tr:hover td{background:#191c22}");
            sb.Append(".g{color:#6c8cff;font-weight:600}");
            sb.Append("</style></head><body>");

            sb.Append("<h1>SpaceSweeper report</h1>");
            sb.Append("<div class=\"muted\">Generated ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Append("</div>");

            sb.Append("<div class=\"cards\">");
            Card(sb, Fmt.Size(r.DupeWasted), "Recoverable duplicates");
            Card(sb, r.Duplicates.Count.ToString("N0"), "Duplicate groups");
            Card(sb, Fmt.Size(r.TotalBytes), "Total scanned");
            Card(sb, r.FilesScanned.ToString("N0"), "Files scanned");
            Card(sb, Fmt.Size(r.Junk.Sum(f => f.Length)), "Junk files");
            sb.Append("</div>");

            sb.Append("<h2>Largest duplicate groups</h2><table><tr><th>Group</th><th>Size each</th><th>Copies</th><th>Wasted</th><th>Example path</th></tr>");
            foreach (var g in r.Duplicates.Take(150))
            {
                sb.Append("<tr><td class=\"g\">#").Append(g.Index).Append("</td><td class=\"num\">")
                  .Append(Fmt.Size(g.Size)).Append("</td><td class=\"num\">").Append(g.Files.Count)
                  .Append("</td><td class=\"num\">").Append(Fmt.Size(g.Wasted)).Append("</td><td>")
                  .Append(Esc(g.Files[0].FullPath)).Append("</td></tr>");
            }
            sb.Append("</table>");

            if (r.LargeFiles.Count > 0)
            {
                sb.Append("<h2>Largest files</h2><table><tr><th>File</th><th>Size</th><th>Modified</th><th>Folder</th></tr>");
                foreach (var f in r.LargeFiles.Take(100))
                {
                    sb.Append("<tr><td>").Append(Esc(f.Name)).Append("</td><td class=\"num\">")
                      .Append(Fmt.Size(f.Length)).Append("</td><td>").Append(Fmt.Date(f.Modified))
                      .Append("</td><td>").Append(Esc(f.Folder)).Append("</td></tr>");
                }
                sb.Append("</table>");
            }

            if (r.Junk.Count > 0)
            {
                sb.Append("<h2>Junk and temp files</h2><table><tr><th>File</th><th>Size</th><th>Folder</th></tr>");
                foreach (var f in r.Junk.Take(100))
                {
                    sb.Append("<tr><td>").Append(Esc(f.Name)).Append("</td><td class=\"num\">")
                      .Append(Fmt.Size(f.Length)).Append("</td><td>").Append(Esc(f.Folder)).Append("</td></tr>");
                }
                sb.Append("</table>");
            }

            if (r.EmptyFolders.Count > 0)
            {
                sb.Append("<h2>Empty folders</h2><table><tr><th>Folder</th></tr>");
                foreach (var d in r.EmptyFolders.Take(200))
                    sb.Append("<tr><td>").Append(Esc(d)).Append("</td></tr>");
                sb.Append("</table>");
            }

            sb.Append("<p class=\"muted\" style=\"margin-top:30px\">SpaceSweeper scans locally. Nothing was uploaded.</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        private static void Card(StringBuilder sb, string value, string label)
        {
            sb.Append("<div class=\"card\"><b>").Append(Esc(value)).Append("</b><span>").Append(Esc(label)).Append("</span></div>");
        }

        private static string Esc(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
