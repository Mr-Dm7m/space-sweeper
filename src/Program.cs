using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SpaceSweeper
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Headless mode: SpaceSweeper.exe --scan "C:\path"
            if (args.Length >= 2 && args[0] == "--scan")
            {
                ConsoleMode(args[1]);
                return;
            }

            Application.ThreadException += delegate (object s, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show(e.Exception.Message, "SpaceSweeper", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.Run(new MainForm());
        }

        private static void ConsoleMode(string path)
        {
            var opt = new ScanOptions();
            opt.Roots.Add(path);
            var sc = new Scanner(null, System.Threading.CancellationToken.None);

            var r = sc.Scan(opt);
            var sb = new StringBuilder();
            sb.AppendFormat("files         : {0:N0}\r\n", r.FilesScanned);
            sb.AppendFormat("total         : {0}\r\n", Fmt.Size(r.TotalBytes));
            sb.AppendFormat("dupe groups   : {0:N0}\r\n", r.Duplicates.Count);
            sb.AppendFormat("dupe files    : {0:N0}\r\n", r.DupeFileCount);
            sb.AppendFormat("dupe wasted   : {0}\r\n", Fmt.Size(r.DupeWasted));
            sb.AppendFormat("large files   : {0:N0}\r\n", r.LargeFiles.Count);
            sb.AppendFormat("junk files    : {0:N0} ({1})\r\n", r.Junk.Count, Fmt.Size(r.Junk.Sum(f => f.Length)));
            sb.AppendFormat("empty folders : {0:N0}\r\n", r.EmptyFolders.Count);
            sb.AppendFormat("elapsed       : {0:0.00}s\r\n\r\n", r.Elapsed.TotalSeconds);

            foreach (var g in r.Duplicates.Take(12))
                sb.AppendFormat("  {0,10}  x{1,-2}  {2}\r\n", Fmt.Size(g.Size), g.Files.Count, g.Files[0].FullPath);
            foreach (var f in r.LargeFiles.Take(8))
                sb.AppendFormat("  LARGE {0,10}  {1}\r\n", Fmt.Size(f.Length), f.FullPath);

            string outp = Path.Combine(Path.GetTempPath(), "spacesweeper-scan.txt");
            File.WriteAllText(outp, sb.ToString());
            Console.WriteLine(sb.ToString());
        }
    }
}
