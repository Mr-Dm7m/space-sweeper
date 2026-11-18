using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SpaceSweeper
{
    // ------------------------------------------------------------------
    // Model
    // ------------------------------------------------------------------

    public class FileEntry
    {
        public string FullPath;
        public long Length;
        public DateTime Modified;

        public string Name { get { return Path.GetFileName(FullPath); } }
        public string Folder { get { return Path.GetDirectoryName(FullPath); } }
        public string Ext { get { return Path.GetExtension(FullPath).ToLowerInvariant(); } }
    }

    public class DupeGroup
    {
        public int Index;
        public string Hash;
        public long Size;
        public List<FileEntry> Files = new List<FileEntry>();

        public long Wasted { get { return Size * (Files.Count - 1); } }
    }

    public class ScanOptions
    {
        public List<string> Roots = new List<string>();
        public long MinDupeSize = 4096;      // ignore noise below 4 KB
        public long LargeFileSize = 100L * 1024 * 1024; // 100 MB
        public bool IncludeJunk = true;
        public bool IncludeEmptyFolders = true;
        public int MaxFiles = 750000;
        public HashSet<string> ExcludeRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public class ScanResult
    {
        public List<FileEntry> AllFiles = new List<FileEntry>();
        public List<DupeGroup> Duplicates = new List<DupeGroup>();
        public List<FileEntry> LargeFiles = new List<FileEntry>();
        public List<string> EmptyFolders = new List<string>();
        public List<FileEntry> Junk = new List<FileEntry>();

        public long TotalBytes;
        public long DupeWasted;
        public int FilesScanned { get { return AllFiles.Count; } }
        public int DupeFileCount;
        public TimeSpan Elapsed;
        public bool Truncated;
    }

    public enum KeepStrategy { Oldest, Newest, ShortestPath, Shallowest }

    // ------------------------------------------------------------------
    // Scanner
    // ------------------------------------------------------------------

    public class Scanner
    {
        private readonly Action<int, string> _progress;   // percent, status
        private readonly CancellationToken _token;

        private static readonly HashSet<string> SkipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "$Recycle.Bin", "System Volume Information", "Recovery", "PerfLogs",
            "Windows", "WinSxS", "Installer", "node_modules", ".git", "AppData"
        };

        public Scanner(Action<int, string> progress, CancellationToken token)
        {
            _progress = progress;
            _token = token;
        }

        private void Report(int pct, string status)
        {
            if (_progress != null) _progress(pct, status);
        }

        public ScanResult Scan(ScanOptions opt)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new ScanResult();

            var files = new List<FileEntry>();
            var dirs = new List<string>();
            var rootSet = new HashSet<string>(
                opt.Roots.Where(Directory.Exists).Select(SafeFullPath), StringComparer.OrdinalIgnoreCase);

            int n = 0;
            foreach (string root in opt.Roots)
            {
                _token.ThrowIfCancellationRequested();
                if (!Directory.Exists(root)) continue;
                Walk(root, files, dirs, opt, ref n, rootSet);
                if (files.Count >= opt.MaxFiles) { result.Truncated = true; break; }
            }

            Report(35, "Grouping by size...");

            // ---- duplicates: size bucket -> quick hash -> full hash ----
            var bySize = new Dictionary<long, List<FileEntry>>();
            foreach (var f in files)
            {
                if (f.Length < opt.MinDupeSize) continue;
                List<FileEntry> bucket;
                if (!bySize.TryGetValue(f.Length, out bucket))
                {
                    bucket = new List<FileEntry>();
                    bySize[f.Length] = bucket;
                }
                bucket.Add(f);
            }

            var candidates = new List<FileEntry>();
            foreach (var kv in bySize)
                if (kv.Value.Count > 1) candidates.AddRange(kv.Value);

            long totalToHash = 0;
            foreach (var c in candidates) totalToHash += c.Length;
            if (totalToHash <= 0) totalToHash = 1;

            Report(40, string.Format("Hashing {0:N0} duplicate candidates...", candidates.Count));

            var quickMap = new Dictionary<string, List<FileEntry>>();
            long hashed = 0;
            foreach (var f in candidates)
            {
                _token.ThrowIfCancellationRequested();
                string h;
                try { h = QuickHash(f.FullPath, f.Length); }
                catch { continue; }
                List<FileEntry> list;
                if (!quickMap.TryGetValue(h, out list))
                {
                    list = new List<FileEntry>();
                    quickMap[h] = list;
                }
                list.Add(f);
                hashed += f.Length;
                if ((quickMap.Count & 63) == 0)
                    Report(40 + (int)(20.0 * hashed / totalToHash), "Filtering candidates...");
            }

            var fullCandidates = new List<FileEntry>();
            foreach (var kv in quickMap)
                if (kv.Value.Count > 1) fullCandidates.AddRange(kv.Value);

            var groups = new List<DupeGroup>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int gi = 0;

            // full hash only when quick-hash matched
            var byQuick = new List<List<FileEntry>>(quickMap.Values.Where(l => l.Count > 1));
            long fullTotal = 0;
            foreach (var l in byQuick) foreach (var f in l) fullTotal += f.Length;
            if (fullTotal <= 0) fullTotal = 1;
            long fullDone = 0;

            foreach (var bucket in byQuick)
            {
                _token.ThrowIfCancellationRequested();
                var byHash = new Dictionary<string, List<FileEntry>>();
                foreach (var f in bucket)
                {
                    string h;
                    try { h = FullHash(f.FullPath); }
                    catch { fullDone += f.Length; continue; }
                    List<FileEntry> l2;
                    if (!byHash.TryGetValue(h, out l2)) { l2 = new List<FileEntry>(); byHash[h] = l2; }
                    l2.Add(f);
                    fullDone += f.Length;
                }
                foreach (var kv in byHash)
                {
                    if (kv.Value.Count < 2) continue;
                    var g = new DupeGroup();
                    g.Index = ++gi;
                    g.Hash = kv.Key;
                    g.Size = kv.Value[0].Length;
                    g.Files = kv.Value;
                    groups.Add(g);
                }
                Report(60 + (int)(30.0 * fullDone / fullTotal), "Verifying duplicates...");
            }

            groups.Sort(delegate (DupeGroup a, DupeGroup b) { return b.Wasted.CompareTo(a.Wasted); });
            result.Duplicates = groups;
            foreach (var g in groups) result.DupeWasted += g.Wasted;

            int dupeFiles = 0;
            foreach (var g in groups) dupeFiles += g.Files.Count;
            result.DupeFileCount = dupeFiles;

            Report(92, "Collecting large files...");

            result.LargeFiles = files
                .Where(f => f.Length >= opt.LargeFileSize)
                .OrderByDescending(f => f.Length)
                .Take(500)
                .ToList();

            if (opt.IncludeJunk)
                result.Junk = files.Where(f => IsJunk(f)).OrderByDescending(f => f.Length).ToList();

            if (opt.IncludeEmptyFolders)
                result.EmptyFolders = FindEmptyDirs(dirs);

            result.AllFiles = files;
            foreach (var f in files) result.TotalBytes += f.Length;
            result.Elapsed = sw.Elapsed;

            Report(100, string.Format("Done. {0:N0} files in {1:0.0}s", files.Count, sw.Elapsed.TotalSeconds));
            return result;
        }

        private void Walk(string root, List<FileEntry> files, List<string> dirs, ScanOptions opt, ref int n, HashSet<string> roots)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                _token.ThrowIfCancellationRequested();
                string d = stack.Pop();
                dirs.Add(d);

                try
                {
                    foreach (string sub in Directory.GetDirectories(d))
                    {
                        _token.ThrowIfCancellationRequested();
                        string nm = Path.GetFileName(sub);
                        if (SkipDirs.Contains(nm)) continue;
                        bool inRoots = false;
                        foreach (var r in roots)
                            if (string.Equals(r, sub, StringComparison.OrdinalIgnoreCase)) { inRoots = true; break; }
                        if (!inRoots)
                        {
                            try
                            {
                                if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue;
                            }
                            catch { continue; }
                        }
                        if (opt.ExcludeRoots.Contains(SafeFullPath(sub))) continue;
                        stack.Push(sub);
                    }
                }
                catch { }

                try
                {
                    foreach (string fp in Directory.GetFiles(d))
                    {
                        _token.ThrowIfCancellationRequested();
                        try
                        {
                            var fi = new FileInfo(fp);
                            if ((fi.Attributes & (FileAttributes.ReparsePoint | FileAttributes.Offline)) != 0) continue;
                            var e = new FileEntry();
                            e.FullPath = fp;
                            e.Length = fi.Length;
                            e.Modified = fi.LastWriteTime;
                            files.Add(e);
                        }
                        catch { }

                        n++;
                        if ((n & 255) == 0)
                            Report(5 + (int)(30.0 * Math.Min(1.0, (double)files.Count / 60000.0)),
                                string.Format("Scanning... {0:N0} files", files.Count));
                        if (files.Count >= opt.MaxFiles) return;
                    }
                }
                catch { }
            }
        }

        private static string SafeFullPath(string p)
        {
            try { return Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar); }
            catch { return p; }
        }

        // ---- hashing ----

        private const int QuickBytes = 64 * 1024;

        private static string QuickHash(string path, long len)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 16, FileOptions.SequentialScan))
            {
                int n = (int)Math.Min(QuickBytes, len);
                byte[] buf = new byte[n];
                int read = 0;
                while (read < n)
                {
                    int r = fs.Read(buf, read, n - read);
                    if (r <= 0) break;
                    read += r;
                }
                using (var sha = SHA256.Create())
                    return ToHex(sha.ComputeHash(buf, 0, read));
            }
        }

        private static string FullHash(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.SequentialScan))
            using (var sha = SHA256.Create())
            {
                byte[] buf = new byte[1 << 20];
                int r;
                while ((r = fs.Read(buf, 0, buf.Length)) > 0)
                    sha.TransformBlock(buf, 0, r, null, 0);
                sha.TransformFinalBlock(new byte[0], 0, 0);
                return ToHex(sha.Hash);
            }
        }

        private static string ToHex(byte[] b)
        {
            var sb = new StringBuilder(b.Length * 2);
            for (int i = 0; i < b.Length; i++) sb.Append(b[i].ToString("x2"));
            return sb.ToString();
        }

        // ---- junk ----

        public static bool IsJunk(FileEntry f)
        {
            string ext = f.Ext;
            string name = f.Name.ToLowerInvariant();

            if (ext == ".tmp" || ext == ".crdownload" || ext == ".part" || ext == ".partial" || ext == ".dmp")
                return true;
            if (name == "thumbs.db" || name == ".ds_store")
                return true;
            if (name.StartsWith("~$") && (ext == ".docx" || ext == ".xlsx" || ext == ".pptx" || ext == ".doc"))
                return true;
            return false;
        }

        // ---- empty folders ----

        private static List<string> FindEmptyDirs(List<string> dirs)
        {
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hasFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string d in dirs)
            {
                try
                {
                    if (Directory.GetFiles(d).Length > 0) hasFiles.Add(d);
                }
                catch { hasFiles.Add(d); }
            }

            // process deepest first so a parent can become empty
            var ordered = new List<string>(dirs);
            ordered.Sort(delegate (string a, string b) { return b.Length.CompareTo(a.Length); });

            foreach (string d in ordered)
            {
                if (hasFiles.Contains(d)) { live.Add(d); continue; }
                bool childLive = false;
                try
                {
                    foreach (string sub in Directory.GetDirectories(d))
                        if (live.Contains(SafeFullPath(sub))) { childLive = true; break; }
                }
                catch { childLive = true; }
                if (childLive) live.Add(d);
            }

            var empty = new List<string>();
            foreach (string d in ordered)
                if (!live.Contains(d)) empty.Add(d);
            return empty;
        }
    }

    // ------------------------------------------------------------------
    // Safe delete (Recycle Bin)
    // ------------------------------------------------------------------

    public static class FileOps
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_SILENT = 0x0004;
        private const ushort FOF_NOERRORUI = 0x0400;
        private const ushort FOF_NOCONFIRMMKDIR = 0x0200;

        /// <summary>Moves files to the Recycle Bin. Returns number that no longer exist.</summary>
        public static int ToRecycleBin(IList<string> paths, IntPtr owner)
        {
            int ok = 0;
            const int batch = 400;
            for (int i = 0; i < paths.Count; i += batch)
            {
                var slice = new List<string>();
                for (int j = i; j < Math.Min(i + batch, paths.Count); j++)
                    if (File.Exists(paths[j]) || Directory.Exists(paths[j])) slice.Add(paths[j]);
                if (slice.Count == 0) continue;

                var sb = new StringBuilder();
                foreach (string p in slice) { sb.Append(p); sb.Append('\0'); }
                sb.Append('\0');

                var op = new SHFILEOPSTRUCT();
                op.hwnd = owner;
                op.wFunc = FO_DELETE;
                op.pFrom = sb.ToString();
                op.fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI);

                try { SHFileOperation(ref op); }
                catch { }

                foreach (string p in slice)
                    if (!File.Exists(p) && !Directory.Exists(p)) ok++;
            }
            return ok;
        }
    }

    // ------------------------------------------------------------------
    // Formatting helpers
    // ------------------------------------------------------------------

    public static class Fmt
    {
        public static string Size(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024 * 1024 * 1024)).ToString("0.00") + " TB";
            if (bytes >= 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024 * 1024)).ToString("0.00") + " GB";
            if (bytes >= 1024L * 1024) return (bytes / (1024.0 * 1024)).ToString("0.0") + " MB";
            if (bytes >= 1024) return (bytes / 1024.0).ToString("0") + " KB";
            return bytes + " B";
        }

        public static string Date(DateTime d)
        {
            return d.ToString("yyyy-MM-dd");
        }
    }
}
