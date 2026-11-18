using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpaceSweeper
{
    // ==================================================================
    // Theme
    // ==================================================================

    public static class T
    {
        public static readonly Color Bg = Color.FromArgb(15, 17, 21);
        public static readonly Color Panel = Color.FromArgb(23, 26, 32);
        public static readonly Color Panel2 = Color.FromArgb(29, 33, 40);
        public static readonly Color Panel3 = Color.FromArgb(35, 40, 48);
        public static readonly Color Border = Color.FromArgb(42, 47, 56);
        public static readonly Color Text = Color.FromArgb(232, 236, 243);
        public static readonly Color Muted = Color.FromArgb(138, 147, 163);
        public static readonly Color Accent = Color.FromArgb(108, 140, 255);
        public static readonly Color AccentDark = Color.FromArgb(72, 100, 210);
        public static readonly Color Success = Color.FromArgb(56, 211, 159);
        public static readonly Color Danger = Color.FromArgb(240, 96, 96);
        public static readonly Color Warn = Color.FromArgb(240, 186, 84);

        public static Font F(float size, FontStyle style)
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }
        public static Font F(float size) { return F(size, FontStyle.Regular); }
    }

    public static class Gfx
    {
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color c)
        {
            using (var p = Round(r, radius))
            using (var b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        public static void StrokeRound(Graphics g, Rectangle r, int radius, Color c)
        {
            using (var p = Round(r, radius))
            using (var pen = new Pen(c))
                g.DrawPath(pen, p);
        }
    }

    // ==================================================================
    // Controls
    // ==================================================================

    public class FlatButton : Control
    {
        public bool Primary;
        public bool Danger;
        public bool Ghost;
        private bool _hover, _down;

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = T.F(9.75f, FontStyle.Bold);
            ForeColor = T.Text;
            Height = 38;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color fill, stroke = Color.Transparent, txt = ForeColor;

            if (Ghost)
            {
                fill = _hover ? T.Panel3 : T.Panel2;
                stroke = T.Border;
            }
            else if (Danger)
            {
                fill = _hover ? Color.FromArgb(196, 66, 66) : T.Danger;
                txt = Color.White;
            }
            else if (Primary)
            {
                fill = _down ? T.AccentDark : (_hover ? Color.FromArgb(126, 155, 255) : T.Accent);
                txt = Color.White;
            }
            else
            {
                fill = _down ? T.Panel2 : (_hover ? T.Panel3 : T.Panel2);
                stroke = T.Border;
            }

            if (!Enabled) { fill = T.Panel2; txt = T.Muted; }

            Gfx.FillRound(g, r, 8, fill);
            if (stroke != Color.Transparent) Gfx.StrokeRound(g, r, 8, stroke);

            TextRenderer.DrawText(g, Text, Font, r, txt,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    public class CheckToggle : Control
    {
        private bool _checked;
        public string Label = "";
        public string Sub = "";

        public event EventHandler Changed;

        public bool Checked
        {
            get { return _checked; }
            set { _checked = value; Invalidate(); }
        }

        public CheckToggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Panel;
            Font = T.F(9.5f);
            Height = 34;
            Cursor = Cursors.Hand;
        }

        protected override void OnClick(EventArgs e) { SetChecked(!_checked, true); base.OnClick(e); }

        public void SetChecked(bool v, bool fire)
        {
            if (_checked == v) return;
            _checked = v;
            Invalidate();
            if (fire && Changed != null) Changed(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Panel);

            int bs = 17;
            var box = new Rectangle(2, (Height - bs) / 2, bs, bs);
            var r = new Rectangle(box.X, box.Y, bs - 1, bs - 1);

            if (_checked)
            {
                Gfx.FillRound(g, r, 5, T.Accent);
                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                    g.DrawLines(pen, new Point[] {
                        new Point(box.X + 4, box.Y + 9),
                        new Point(box.X + 7, box.Y + 12),
                        new Point(box.X + 13, box.Y + 5) });
                }
            }
            else
            {
                Gfx.FillRound(g, r, 5, T.Panel2);
                Gfx.StrokeRound(g, r, 5, T.Border);
            }

            var tr = new Rectangle(box.Right + 9, 0, Width - box.Right - 11, Height);
            TextRenderer.DrawText(g, Label, Font, tr, T.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    public class DropdownField : Control
    {
        public readonly List<string> Items = new List<string>();
        private int _sel;
        private Form _popup;
        private int _hover = -1;
        private const int RowH = 34;

        public event EventHandler SelectionChanged;

        public int SelectedIndex
        {
            get { return _sel; }
            set { _sel = value; Invalidate(); }
        }

        public DropdownField()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Panel;
            Font = T.F(9.25f);
            Height = 34;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Panel);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Gfx.FillRound(g, r, 7, _popup != null ? T.Panel3 : T.Panel2);
            Gfx.StrokeRound(g, r, 7, _popup != null ? T.Accent : T.Border);

            string txt = (_sel >= 0 && _sel < Items.Count) ? Items[_sel] : "";
            TextRenderer.DrawText(g, txt, Font, new Rectangle(12, 0, Width - 40, Height), T.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            int cx = Width - 19, cy = Height / 2 - 1;
            using (var pen = new Pen(T.Muted, 1.6f))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                g.DrawLines(pen, new Point[] {
                    new Point(cx - 4, cy - 2),
                    new Point(cx, cy + 2),
                    new Point(cx + 4, cy - 2) });
            }
        }

        protected override void OnClick(EventArgs e)
        {
            if (_popup != null) { ClosePopup(); return; }
            OpenPopup();
            base.OnClick(e);
        }

        private void OpenPopup()
        {
            var f = new Form();
            f.FormBorderStyle = FormBorderStyle.None;
            f.BackColor = T.Panel2;
            f.StartPosition = FormStartPosition.Manual;
            f.ShowInTaskbar = false;
            f.TopMost = true;
            f.Size = new Size(Width, Items.Count * RowH + 8);
            f.Location = PointToScreen(new Point(0, Height + 2));

            f.Paint += delegate (object s, PaintEventArgs pe)
            {
                var g = pe.Graphics;
                g.Clear(T.Panel2);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                for (int i = 0; i < Items.Count; i++)
                {
                    var row = new Rectangle(1, 4 + i * RowH, f.Width - 2, RowH);
                    if (i == _hover)
                        Gfx.FillRound(g, new Rectangle(4, row.Y + 1, row.Width - 8, row.Height - 2), 6, T.Panel3);
                    TextRenderer.DrawText(g, Items[i], Font, new Rectangle(14, row.Y, row.Width - 26, row.Height),
                        i == _sel ? T.Accent : T.Text,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
                using (var pen = new Pen(T.Border)) g.DrawRectangle(pen, 0, 0, f.Width - 1, f.Height - 1);
            };

            f.MouseMove += delegate (object s, MouseEventArgs me)
            {
                int idx = (me.Y - 4) / RowH;
                if (idx < 0 || idx >= Items.Count) idx = -1;
                if (idx != _hover) { _hover = idx; f.Invalidate(); }
            };

            f.MouseClick += delegate (object s, MouseEventArgs me)
            {
                int idx = (me.Y - 4) / RowH;
                if (idx >= 0 && idx < Items.Count)
                {
                    SelectedIndex = idx;
                    if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
                }
                ClosePopup();
            };

            f.Deactivate += delegate { ClosePopup(); };

            _popup = f;
            f.Show(FindForm());
            Invalidate();
        }

        private void ClosePopup()
        {
            if (_popup == null) return;
            var p = _popup;
            _popup = null;
            _hover = -1;
            try { p.Close(); p.Dispose(); } catch { }
            Invalidate();
        }
    }

    public class StorageBar : Control
    {
        public double UsedPct;
        public string Title = "";
        public string Sub = "";

        public StorageBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Bg);

            TextRenderer.DrawText(g, Title, T.F(10.5f, FontStyle.Bold), new Rectangle(0, 4, Width, 20), T.Text,
                TextFormatFlags.Left);
            TextRenderer.DrawText(g, Sub, T.F(9f), new Rectangle(0, 24, Width, 18), T.Muted,
                TextFormatFlags.Left);

            var bar = new Rectangle(0, 52, Width, 12);
            Gfx.FillRound(g, bar, 6, T.Panel2);

            int w = (int)(bar.Width * Math.Max(0, Math.Min(1, UsedPct)));
            if (w > 4)
            {
                var usedBar = new Rectangle(bar.X, bar.Y, w, bar.Height);
                using (var br = new LinearGradientBrush(new Rectangle(0, bar.Y, Math.Max(w, 8), bar.Height),
                    T.Accent, T.Success, LinearGradientMode.Horizontal))
                    g.FillPath(br, Gfx.Round(usedBar, 6));
            }
        }
    }

    public class TabHeader : Control
    {
        public bool Active;
        public string Badge = "";

        public TabHeader()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Font = T.F(9.75f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Height = 40;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(T.Bg);

            Color c = Active ? T.Text : T.Muted;
            var r = new Rectangle(0, 0, Width, Height - 3);
            TextRenderer.DrawText(g, Text, Font, r, c,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            if (!string.IsNullOrEmpty(Badge))
            {
                var sz = TextRenderer.MeasureText(Badge, T.F(7.5f, FontStyle.Bold));
                var br = new Rectangle(Width / 2 + TextRenderer.MeasureText(Text, Font).Width / 2 + 8, 10, sz.Width + 10, 19);
                Gfx.FillRound(g, br, 9, Active ? T.Accent : T.Panel3);
                TextRenderer.DrawText(g, Badge, T.F(7.5f, FontStyle.Bold), br,
                    Active ? Color.White : T.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            if (Active)
                Gfx.FillRound(g, new Rectangle(6, Height - 3, Width - 12, 3), 2, T.Accent);
        }
    }

    public class DarkGrid : DataGridView
    {
        public DarkGrid()
        {
            DoubleBuffered = true;
            BackgroundColor = T.Bg;
            BorderStyle = BorderStyle.None;
            GridColor = T.Panel2;
            RowHeadersVisible = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            ReadOnly = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = true;
            EnableHeadersVisualStyles = false;
            ColumnHeadersHeight = 34;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            RowTemplate.Height = 30;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = T.Panel,
                ForeColor = T.Muted,
                Font = T.F(8.5f, FontStyle.Bold),
                SelectionBackColor = T.Panel,
                SelectionForeColor = T.Muted,
                Padding = new Padding(6, 0, 0, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = T.Bg,
                ForeColor = T.Text,
                SelectionBackColor = T.Panel3,
                SelectionForeColor = T.Text,
                Font = T.F(9.25f),
                Padding = new Padding(6, 0, 0, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(18, 20, 25),
                ForeColor = T.Text,
                SelectionBackColor = T.Panel3,
                SelectionForeColor = T.Text
            };
        }
    }

    // ==================================================================
    // Dark dialog
    // ==================================================================

    public class DarkDialog : Form
    {
        public static bool Show(IWin32Window owner, string title, string message, string okText, bool danger)
        {
            using (var d = new DarkDialog(title, message, okText, danger))
                return d.ShowDialog(owner) == DialogResult.OK;
        }

        private DarkDialog(string title, string message, string okText, bool danger)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = T.Panel;
            Size = new Size(520, 230);
            ShowInTaskbar = false;
            Font = T.F(9.75f);

            var t = new Label();
            t.Text = title;
            t.Font = T.F(13f, FontStyle.Bold);
            t.ForeColor = T.Text;
            t.BackColor = T.Panel;
            t.SetBounds(28, 24, 464, 30);

            var m = new Label();
            m.Text = message;
            m.Font = T.F(9.75f);
            m.ForeColor = T.Muted;
            m.BackColor = T.Panel;
            m.SetBounds(28, 64, 464, 96);

            var cancel = new FlatButton();
            cancel.Text = "Cancel";
            cancel.Ghost = true;
            cancel.SetBounds(268, 168, 108, 38);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            var ok = new FlatButton();
            ok.Text = okText;
            ok.Danger = danger;
            ok.Primary = !danger;
            ok.SetBounds(384, 168, 108, 38);
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };

            Controls.Add(t); Controls.Add(m); Controls.Add(cancel); Controls.Add(ok);

            Paint += delegate (object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(T.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                Gfx.FillRound(e.Graphics, new Rectangle(0, 0, 6, Height), 0, danger ? T.Danger : T.Accent);
            };
        }
    }

    // ==================================================================
    // Main window
    // ==================================================================

    public class MainForm : Form
    {
        private const int HEADER_H = 64;
        private const int SIDEBAR_W = 304;
        private const int FOOTER_H = 66;

        private List<string> _roots = new List<string>();
        private List<CheckToggle> _rootToggles = new List<CheckToggle>();
        private CheckToggle _optJunk, _optEmpty;
        private DropdownField _strategy;
        private FlatButton _scanBtn, _stopBtn, _cleanBtn, _exportBtn;
        private StorageBar _storage;
        private Panel _progressTrack;
        private Label _status, _selInfo;
        private TabHeader[] _tabs;
        private Panel[] _pages;
        private DarkGrid _gridDupes, _gridLarge, _gridEmpty, _gridJunk;
        private int _activeTab;

        private ScanResult _result;
        private CancellationTokenSource _cts;
        private bool _scanning;
        private List<DupeGroup> _shownGroups = new List<DupeGroup>();

        public MainForm()
        {
            Text = "SpaceSweeper";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1240, 800);
            DoubleBuffered = true;
            BackColor = T.Bg;
            Font = T.F(9.75f);
            KeyPreview = true;

            BuildChrome();
            BuildSidebar();
            BuildMain();
            LoadDefaults();
        }

        // ---------------- chrome ----------------

        private void BuildChrome()
        {
            var header = new Panel();
            header.SetBounds(0, 0, ClientSize.Width, HEADER_H);
            header.BackColor = T.Bg;
            header.MouseDown += DragWindow;
            Controls.Add(header);

            var logo = new LogoMark();
            logo.SetBounds(24, 16, 32, 32);
            header.Controls.Add(logo);

            var name = new Label();
            name.Text = "SpaceSweeper";
            name.Font = T.F(13f, FontStyle.Bold);
            name.ForeColor = T.Text;
            name.BackColor = T.Bg;
            name.SetBounds(68, 17, 240, 22);
            name.MouseDown += DragWindow;
            header.Controls.Add(name);

            var tag = new Label();
            tag.Text = "Duplicate and junk file scanner";
            tag.Font = T.F(8.5f);
            tag.ForeColor = T.Muted;
            tag.BackColor = T.Bg;
            tag.SetBounds(69, 38, 320, 16);
            tag.MouseDown += DragWindow;
            header.Controls.Add(tag);

            var min = new FlatButton();
            min.Text = "\u2013";
            min.Ghost = true;
            min.Font = T.F(11f, FontStyle.Bold);
            min.SetBounds(ClientSize.Width - 100, 16, 36, 32);
            min.Click += delegate { WindowState = FormWindowState.Minimized; };
            header.Controls.Add(min);

            var close = new FlatButton();
            close.Text = "\u2715";
            close.Ghost = true;
            close.SetBounds(ClientSize.Width - 56, 16, 36, 32);
            close.Click += delegate { Close(); };
            header.Controls.Add(close);

            var sep = new Panel();
            sep.SetBounds(0, HEADER_H - 1, ClientSize.Width, 1);
            sep.BackColor = T.Border;
            header.Controls.Add(sep);
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Native.ReleaseCapture();
            Native.SendMessage(Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
        }

        // ---------------- sidebar ----------------

        private void BuildSidebar()
        {
            var side = new Panel();
            side.SetBounds(0, HEADER_H, SIDEBAR_W, ClientSize.Height - HEADER_H);
            side.BackColor = T.Panel;
            Controls.Add(side);

            var edge = new Panel();
            edge.SetBounds(SIDEBAR_W - 1, 0, 1, side.Height);
            edge.BackColor = T.Border;
            side.Controls.Add(edge);

            var lbl = new Label();
            lbl.Text = "SCAN LOCATIONS";
            lbl.Font = T.F(8f, FontStyle.Bold);
            lbl.ForeColor = T.Muted;
            lbl.BackColor = T.Panel;
            lbl.SetBounds(24, 22, 200, 16);
            side.Controls.Add(lbl);

            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] names = { "Downloads", "Desktop", "Documents", "Pictures", "Videos", "Music" };
            string[] paths = {
                Path.Combine(profile, "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (!Directory.Exists(path)) continue;

                var tg = new CheckToggle();
                tg.Label = names[i];
                tg.Tag = path;
                tg.SetBounds(16, 48 + i * 34, SIDEBAR_W - 32, 34);
                tg.Checked = names[i] == "Downloads" || names[i] == "Desktop";
                tg.Changed += delegate (object s, EventArgs e) { EvaluateScanButton(); };
                side.Controls.Add(tg);
                _rootToggles.Add(tg);
            }

            int y = 48 + paths.Length * 34 + 10;

            var addBtn = new FlatButton();
            addBtn.Text = "+  Add folder\u2026";
            addBtn.Ghost = true;
            addBtn.Font = T.F(9f, FontStyle.Bold);
            addBtn.SetBounds(16, y, SIDEBAR_W - 32, 34);
            addBtn.Click += delegate { AddFolder(); };
            side.Controls.Add(addBtn);
            y += 34 + 8;

            _customRootsPanel = new Panel();
            _customRootsPanel.SetBounds(16, y, SIDEBAR_W - 32, 0);
            _customRootsPanel.BackColor = T.Panel;
            side.Controls.Add(_customRootsPanel);

            int oy = y + 18;
            var lbl2 = new Label();
            lbl2.Text = "OPTIONS";
            lbl2.Font = T.F(8f, FontStyle.Bold);
            lbl2.ForeColor = T.Muted;
            lbl2.BackColor = T.Panel;
            lbl2.SetBounds(24, oy, 200, 16);
            side.Controls.Add(lbl2);
            oy += 22;

            _optJunk = new CheckToggle();
            _optJunk.Label = "Find temp / junk files";
            _optJunk.Checked = true;
            _optJunk.SetBounds(16, oy, SIDEBAR_W - 32, 34);
            side.Controls.Add(_optJunk);
            oy += 34;

            _optEmpty = new CheckToggle();
            _optEmpty.Label = "Find empty folders";
            _optEmpty.Checked = true;
            _optEmpty.SetBounds(16, oy, SIDEBAR_W - 32, 34);
            side.Controls.Add(_optEmpty);
            oy += 34 + 14;

            var lbl3 = new Label();
            lbl3.Text = "WHEN DUPLICATES EXIST, KEEP";
            lbl3.Font = T.F(8f, FontStyle.Bold);
            lbl3.ForeColor = T.Muted;
            lbl3.BackColor = T.Panel;
            lbl3.SetBounds(24, oy, SIDEBAR_W - 32, 16);
            side.Controls.Add(lbl3);
            oy += 22;

            _strategy = new DropdownField();
            _strategy.BackColor = T.Panel;
            _strategy.SetBounds(16, oy, SIDEBAR_W - 32, 34);
            _strategy.Items.AddRange(new string[] { "The oldest file", "The newest file", "The shortest path", "The shallowest folder" });
            _strategy.SelectedIndex = 0;
            _strategy.SelectionChanged += delegate { if (_result != null) AutoSelect(); };
            side.Controls.Add(_strategy);
            oy += 34;

            var tip = new Label();
            tip.Text = "Everything goes to the Recycle Bin.\r\nNothing is ever deleted permanently.";
            tip.Font = T.F(8.25f);
            tip.ForeColor = T.Muted;
            tip.BackColor = T.Panel;
            tip.SetBounds(24, side.Height - 74, SIDEBAR_W - 40, 54);
            side.Controls.Add(tip);
        }

        private Panel _customRootsPanel;

        private void AddFolder()
        {
            using (var d = new FolderBrowserDialog())
            {
                d.Description = "Add a folder to scan";
                d.ShowNewFolderButton = false;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                _roots.Add(d.SelectedPath);
                var tg = new CheckToggle();
                tg.Label = Shorten(d.SelectedPath, 26);
                tg.Tag = d.SelectedPath;
                tg.Checked = true;
                tg.SetBounds(0, _customRootsPanel.Controls.Count * 32, _customRootsPanel.Width, 32);
                tg.Changed += delegate { EvaluateScanButton(); };
                _customRootsPanel.Controls.Add(tg);
                _rootToggles.Add(tg);
                _customRootsPanel.Height = _customRootsPanel.Controls.Count * 32;
                EvaluateScanButton();
            }
        }

        private static string Shorten(string s, int max)
        {
            if (s.Length <= max) return s;
            return "\u2026" + s.Substring(s.Length - (max - 1));
        }

        // ---------------- main ----------------

        private void BuildMain()
        {
            int mx = SIDEBAR_W + 32;
            int mw = ClientSize.Width - mx - 36;

            _storage = new StorageBar();
            _storage.SetBounds(mx, HEADER_H + 22, mw - 250, 74);
            Controls.Add(_storage);
            UpdateStorage();

            _scanBtn = new FlatButton();
            _scanBtn.Text = "Scan now";
            _scanBtn.Primary = true;
            _scanBtn.SetBounds(ClientSize.Width - 32 - 190, HEADER_H + 30, 190, 44);
            _scanBtn.Click += delegate { StartScan(); };
            Controls.Add(_scanBtn);

            _stopBtn = new FlatButton();
            _stopBtn.Text = "Stop";
            _stopBtn.Danger = true;
            _stopBtn.Visible = false;
            _stopBtn.SetBounds(ClientSize.Width - 32 - 190, HEADER_H + 30, 190, 44);
            _stopBtn.Click += delegate { if (_cts != null) _cts.Cancel(); };
            Controls.Add(_stopBtn);

            _progressTrack = new Panel();
            _progressTrack.SetBounds(mx, HEADER_H + 104, mw, 3);
            _progressTrack.BackColor = T.Panel2;
            Controls.Add(_progressTrack);

            _status = new Label();
            _status.Text = "Ready.";
            _status.Font = T.F(8.75f);
            _status.ForeColor = T.Muted;
            _status.BackColor = T.Bg;
            _status.SetBounds(mx, HEADER_H + 112, mw, 18);
            Controls.Add(_status);

            string[] titles = { "Duplicates", "Large files", "Empty folders", "Junk" };
            _tabs = new TabHeader[titles.Length];
            int tw = Math.Max(120, mw / titles.Length);
            for (int i = 0; i < titles.Length; i++)
            {
                var t = new TabHeader();
                t.Text = titles[i];
                t.SetBounds(mx + i * tw, HEADER_H + 140, tw, 40);
                int idx = i;
                t.Click += delegate { SetTab(idx); };
                Controls.Add(t);
                _tabs[i] = t;
            }

            int gy = HEADER_H + 180;
            int gh = ClientSize.Height - gy - FOOTER_H - 16;

            _pages = new Panel[4];
            for (int i = 0; i < 4; i++)
            {
                var p = new Panel();
                p.SetBounds(mx, gy, mw, gh);
                p.BackColor = T.Panel2;
                p.Padding = new Padding(1);
                p.Visible = i == 0;
                Controls.Add(p);
                _pages[i] = p;
            }

            _gridDupes = MakeGrid(new[] { "SEL", "GROUP", "FILE NAME", "SIZE", "MODIFIED", "FOLDER" },
                                  new[] { 0.045f, 0.09f, 0.33f, 0.10f, 0.12f, 0.315f });
            _gridLarge = MakeGrid(new[] { "SEL", "FILE NAME", "SIZE", "MODIFIED", "FOLDER" },
                                  new[] { 0.045f, 0.35f, 0.10f, 0.12f, 0.385f });
            _gridEmpty = MakeGrid(new[] { "SEL", "EMPTY FOLDER" },
                                  new[] { 0.06f, 0.94f });
            _gridJunk = MakeGrid(new[] { "SEL", "FILE NAME", "SIZE", "MODIFIED", "FOLDER" },
                                 new[] { 0.045f, 0.35f, 0.10f, 0.12f, 0.385f });

            _pages[0].Controls.Add(_gridDupes);
            _pages[1].Controls.Add(_gridLarge);
            _pages[2].Controls.Add(_gridEmpty);
            _pages[3].Controls.Add(_gridJunk);

            HookGrid(_gridDupes);
            HookGrid(_gridLarge);
            HookGrid(_gridEmpty);
            HookGrid(_gridJunk);

            // footer
            var fp = new Panel();
            fp.SetBounds(SIDEBAR_W, ClientSize.Height - FOOTER_H, ClientSize.Width - SIDEBAR_W, FOOTER_H);
            fp.BackColor = T.Bg;
            Controls.Add(fp);

            var fline = new Panel();
            fline.SetBounds(0, 0, fp.Width, 1);
            fline.BackColor = T.Border;
            fp.Controls.Add(fline);

            _selInfo = new Label();
            _selInfo.Text = "Nothing selected";
            _selInfo.Font = T.F(9.75f, FontStyle.Bold);
            _selInfo.ForeColor = T.Muted;
            _selInfo.BackColor = T.Bg;
            _selInfo.SetBounds(32, 20, 520, 24);
            fp.Controls.Add(_selInfo);

            _exportBtn = new FlatButton();
            _exportBtn.Text = "Export report";
            _exportBtn.Ghost = true;
            _exportBtn.Font = T.F(9f, FontStyle.Bold);
            _exportBtn.Size = new Size(130, 38);
            _exportBtn.Location = new Point(fp.Width - 32 - 130 - 12 - 210, 14);
            _exportBtn.Click += delegate { Export(); };
            fp.Controls.Add(_exportBtn);

            _cleanBtn = new FlatButton();
            _cleanBtn.Text = "Clean selected";
            _cleanBtn.Primary = true;
            _cleanBtn.SetBounds(fp.Width - 32 - 210, 14, 210, 38);
            _cleanBtn.Click += delegate { Clean(); };
            fp.Controls.Add(_cleanBtn);

            SetTab(0);
        }

        private DarkGrid MakeGrid(string[] headers, float[] weights)
        {
            var g = new DarkGrid();
            g.Dock = DockStyle.Fill;
            g.Parent = null;

            foreach (string h in headers)
            {
                DataGridViewColumn c;
                if (h == "SEL")
                {
                    var cb = new DataGridViewCheckBoxColumn();
                    cb.HeaderText = "";
                    cb.Name = "SEL";
                    cb.FillWeight = weights[0] * 100;
                    cb.Resizable = DataGridViewTriState.False;
                    cb.SortMode = DataGridViewColumnSortMode.NotSortable;
                    c = cb;
                }
                else
                {
                    c = new DataGridViewTextBoxColumn();
                    c.Name = h;
                    c.HeaderText = h;
                    c.ReadOnly = true;
                    c.SortMode = DataGridViewColumnSortMode.NotSortable;
                    c.FillWeight = 100f / headers.Length * 1f;
                }
                g.Columns.Add(c);
            }

            // apply weights in order
            for (int i = 0; i < g.Columns.Count; i++)
                g.Columns[i].FillWeight = weights[i] * 100f;

            return g;
        }

        private void HookGrid(DarkGrid g)
        {
            g.CellPainting += delegate (object s, DataGridViewCellPaintingEventArgs e)
            {
                if (e.ColumnIndex != 0 || e.RowIndex < 0) return;

                e.PaintBackground(e.CellBounds, true);

                bool val = e.Value is bool && (bool)e.Value;
                int size = 17;
                int x = e.CellBounds.X + (e.CellBounds.Width - size) / 2;
                int y = e.CellBounds.Y + (e.CellBounds.Height - size) / 2;
                var r = new Rectangle(x, y, size - 1, size - 1);

                var gfx = e.Graphics;
                gfx.SmoothingMode = SmoothingMode.AntiAlias;

                if (val)
                {
                    Gfx.FillRound(gfx, r, 5, T.Accent);
                    using (var pen = new Pen(Color.White, 2f))
                    {
                        pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                        gfx.DrawLines(pen, new Point[] {
                            new Point(x + 4, y + 9),
                            new Point(x + 7, y + 12),
                            new Point(x + 13, y + 5) });
                    }
                }
                else
                {
                    Gfx.FillRound(gfx, r, 5, T.Panel2);
                    Gfx.StrokeRound(gfx, r, 5, Color.FromArgb(74, 82, 96));
                }
                e.Handled = true;
            };

            g.CellValueChanged += delegate (object s, DataGridViewCellEventArgs e)
            {
                if (e.ColumnIndex == 0) BeginInvoke(new Action(UpdateSelectionInfo));
            };
            g.CurrentCellDirtyStateChanged += delegate (object s, EventArgs e)
            {
                if (g.IsCurrentCellDirty) g.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            g.CellDoubleClick += delegate (object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0) return;
                var fe = g.Rows[e.RowIndex].Tag as FileEntry;
                string path = fe != null ? fe.FullPath : g.Rows[e.RowIndex].Tag as string;
                if (path != null) RevealInExplorer(path);
            };
        }

        private void SetTab(int idx)
        {
            _activeTab = idx;
            for (int i = 0; i < _tabs.Length; i++) _tabs[i].Active = i == idx;
            for (int i = 0; i < _pages.Length; i++) _pages[i].Visible = i == idx;
            for (int i = 0; i < _tabs.Length; i++) _tabs[i].Invalidate();
            UpdateSelectionInfo();
        }

        // ---------------- defaults ----------------

        private void LoadDefaults()
        {
            EvaluateScanButton();
        }

        private void EvaluateScanButton()
        {
            bool any = _rootToggles.Any(t => t.Checked);
            if (_scanBtn != null) _scanBtn.Enabled = any;
        }

        private void UpdateStorage()
        {
            try
            {
                string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                var di = new DriveInfo(root);
                double used = 1.0 - (double)di.AvailableFreeSpace / di.TotalSize;
                _storage.UsedPct = used;
                _storage.Title = string.Format("{0:N0} GB free of {1:N0} GB on {2}",
                    di.AvailableFreeSpace / (1024L * 1024 * 1024),
                    di.TotalSize / (1024L * 1024 * 1024),
                    root.TrimEnd('\\'));
                _storage.Sub = "SpaceSweeper scans locally. Nothing ever leaves this PC.";
            }
            catch
            {
                _storage.Title = "Storage";
                _storage.Sub = "SpaceSweeper scans locally. Nothing leaves this PC.";
            }
            _storage.Invalidate();
        }

        // ---------------- scanning ----------------

        private void StartScan()
        {
            if (_scanning) return;

            var opt = new ScanOptions();
            opt.IncludeJunk = _optJunk.Checked;
            opt.IncludeEmptyFolders = _optEmpty.Checked;
            foreach (var t in _rootToggles)
                if (t.Checked) opt.Roots.Add((string)t.Tag);

            if (opt.Roots.Count == 0) return;

            _scanning = true;
            _cts = new CancellationTokenSource();
            _scanBtn.Visible = false;
            _stopBtn.Visible = true;
            _status.Text = "Scanning...";
            _cleanBtn.Enabled = false;
            _exportBtn.Enabled = false;
            ClearGrids();

            var token = _cts.Token;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Task.Run(delegate
            {
                try
                {
                    var sc = new Scanner(delegate (int pct, string msg)
                    {
                        if (IsDisposed) return;
                        try { BeginInvoke(new Action(delegate { SetProgress(pct, msg); })); }
                        catch { }
                    }, token);

                    var res = sc.Scan(opt);
                    if (IsDisposed) return;
                    try { BeginInvoke(new Action(delegate { FinishScan(res); })); }
                    catch { }
                }
                catch (OperationCanceledException)
                {
                    try { BeginInvoke(new Action(delegate { AbortScan("Scan cancelled."); })); }
                    catch { }
                }
                catch (Exception ex)
                {
                    try { BeginInvoke(new Action(delegate { AbortScan("Error: " + ex.Message); })); }
                    catch { }
                }
            });
        }

        private Panel _progressFill;

        private void SetProgress(int pct, string msg)
        {
            if (_progressFill == null)
            {
                _progressFill = new Panel();
                _progressFill.BackColor = T.Accent;
                _progressFill.Height = 3;
            }
            if (_progressFill.Parent == null) _progressTrack.Controls.Add(_progressFill);
            int w = (int)(_progressTrack.Width * (pct / 100.0));
            _progressFill.SetBounds(0, 0, Math.Max(0, w), 3);
            _status.Text = msg;
        }

        private void AbortScan(string msg)
        {
            _scanning = false;
            _scanBtn.Visible = true;
            _stopBtn.Visible = false;
            _status.Text = msg;
            _cleanBtn.Enabled = true;
            _exportBtn.Enabled = true;
            if (_progressFill != null) _progressFill.Width = 0;
        }

        private void ClearGrids()
        {
            _gridDupes.Rows.Clear();
            _gridLarge.Rows.Clear();
            _gridEmpty.Rows.Clear();
            _gridJunk.Rows.Clear();
            _shownGroups.Clear();
            _result = null;
            UpdateSelectionInfo();
        }

        private void FinishScan(ScanResult res)
        {
            _scanning = false;
            _scanBtn.Visible = true;
            _stopBtn.Visible = false;
            _cleanBtn.Enabled = true;
            _exportBtn.Enabled = true;
            if (_progressFill != null) _progressFill.Width = 0;
            _result = res;

            _status.Text = string.Format("Scanned {0:N0} files ({1}) in {2:0.0}s  \u2022  found {3:N0} wasted in duplicates",
                res.FilesScanned, Fmt.Size(res.TotalBytes), res.Elapsed.TotalSeconds, Fmt.Size(res.DupeWasted));

            FillDuplicates(res);
            FillLarge(res);
            FillEmpty(res);
            FillJunk(res);

            _tabs[0].Badge = res.Duplicates.Count.ToString();
            _tabs[1].Badge = res.LargeFiles.Count.ToString();
            _tabs[2].Badge = res.EmptyFolders.Count.ToString();
            _tabs[3].Badge = res.Junk.Count.ToString();
            foreach (var t in _tabs) t.Invalidate();

            UpdateSelectionInfo();
        }

        private static void SetTips(DataGridViewRow row, string path)
        {
            foreach (DataGridViewCell c in row.Cells) c.ToolTipText = path;
        }

        private void FillDuplicates(ScanResult res)
        {
            _gridDupes.SuspendLayout();
            _shownGroups.Clear();
            foreach (var g in res.Duplicates.Take(4000)) _shownGroups.Add(g);

            foreach (var g in _shownGroups)
            {
                foreach (var f in g.Files)
                {
                    int i = _gridDupes.Rows.Add(false, "#" + g.Index, f.Name, Fmt.Size(f.Length), Fmt.Date(f.Modified), f.Folder);
                    _gridDupes.Rows[i].Tag = f;
                    SetTips(_gridDupes.Rows[i], f.FullPath);
                    _gridDupes.Rows[i].Cells[0].Value = false;
                }
            }
            _gridDupes.ResumeLayout();
            AutoSelect();
        }

        private void FillLarge(ScanResult res)
        {
            _gridLarge.SuspendLayout();
            foreach (var f in res.LargeFiles)
            {
                int i = _gridLarge.Rows.Add(false, f.Name, Fmt.Size(f.Length), Fmt.Date(f.Modified), f.Folder);
                _gridLarge.Rows[i].Tag = f;
                SetTips(_gridLarge.Rows[i], f.FullPath);
            }
            _gridLarge.ResumeLayout();
        }

        private void FillEmpty(ScanResult res)
        {
            _gridEmpty.SuspendLayout();
            foreach (var d in res.EmptyFolders)
            {
                int i = _gridEmpty.Rows.Add(false, d);
                _gridEmpty.Rows[i].Tag = d;
                SetTips(_gridEmpty.Rows[i], d);
            }
            _gridEmpty.ResumeLayout();
        }

        private void FillJunk(ScanResult res)
        {
            _gridJunk.SuspendLayout();
            foreach (var f in res.Junk)
            {
                int i = _gridJunk.Rows.Add(true, f.Name, Fmt.Size(f.Length), Fmt.Date(f.Modified), f.Folder);
                _gridJunk.Rows[i].Tag = f;
                SetTips(_gridJunk.Rows[i], f.FullPath);
            }
            _gridJunk.ResumeLayout();
        }

        // ---------------- selection ----------------

        private KeepStrategy CurrentStrategy()
        {
            int i = _strategy.SelectedIndex;
            if (i == 1) return KeepStrategy.Newest;
            if (i == 2) return KeepStrategy.ShortestPath;
            if (i == 3) return KeepStrategy.Shallowest;
            return KeepStrategy.Oldest;
        }

        private void AutoSelect()
        {
            if (_shownGroups.Count == 0) { UpdateSelectionInfo(); return; }

            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var strat = CurrentStrategy();
            foreach (var g in _shownGroups)
            {
                FileEntry best = g.Files[0];
                foreach (var f in g.Files)
                    if (Better(f, best, strat)) best = f;
                keep.Add(best.FullPath);
            }

            _gridDupes.SuspendLayout();
            foreach (DataGridViewRow row in _gridDupes.Rows)
            {
                var f = row.Tag as FileEntry;
                if (f == null) continue;
                row.Cells[0].Value = !keep.Contains(f.FullPath);
            }
            _gridDupes.ResumeLayout();
            UpdateSelectionInfo();
        }

        private static bool Better(FileEntry a, FileEntry b, KeepStrategy s)
        {
            switch (s)
            {
                case KeepStrategy.Newest:
                    return a.Modified > b.Modified;
                case KeepStrategy.ShortestPath:
                    if (a.FullPath.Length != b.FullPath.Length) return a.FullPath.Length < b.FullPath.Length;
                    return a.Modified < b.Modified;
                case KeepStrategy.Shallowest:
                    int da = Depth(a.FullPath), db = Depth(b.FullPath);
                    if (da != db) return da < db;
                    return a.Modified < b.Modified;
                default:
                    return a.Modified < b.Modified;
            }
        }

        private static int Depth(string p)
        {
            int n = 0;
            foreach (char c in p) if (c == '\\') n++;
            return n;
        }

        private DarkGrid ActiveGrid()
        {
            switch (_activeTab)
            {
                case 1: return _gridLarge;
                case 2: return _gridEmpty;
                case 3: return _gridJunk;
                default: return _gridDupes;
            }
        }

        private List<string> CheckedPaths(out long bytes)
        {
            var paths = new List<string>();
            bytes = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var g in new[] { _gridDupes, _gridLarge, _gridEmpty, _gridJunk })
            {
                foreach (DataGridViewRow row in g.Rows)
                {
                    if (!(row.Cells[0].Value is bool) || !(bool)row.Cells[0].Value) continue;
                    var fe = row.Tag as FileEntry;
                    if (fe != null)
                    {
                        if (seen.Add(fe.FullPath)) { paths.Add(fe.FullPath); bytes += fe.Length; }
                    }
                    else
                    {
                        var dp = row.Tag as string;
                        if (dp != null && seen.Add(dp)) paths.Add(dp);
                    }
                }
            }
            return paths;
        }

        private void UpdateSelectionInfo()
        {
            if (_selInfo == null) return;
            long bytes;
            var any = false;
            foreach (var g in new[] { _gridDupes, _gridLarge, _gridEmpty, _gridJunk })
                foreach (DataGridViewRow row in g.Rows)
                    if (row.Cells[0].Value is bool && (bool)row.Cells[0].Value) { any = true; break; }

            var paths = CheckedPaths(out bytes);
            if (!any || paths.Count == 0)
            {
                _selInfo.Text = "Nothing selected";
                _selInfo.ForeColor = T.Muted;
                _cleanBtn.Text = "Clean selected";
                _cleanBtn.Enabled = _result != null;
            }
            else
            {
                _selInfo.Text = string.Format("{0:N0} selected  \u2022  {1} recoverable", paths.Count, Fmt.Size(bytes));
                _selInfo.ForeColor = T.Success;
                _cleanBtn.Text = string.Format("Clean {0:N0} items", paths.Count);
                _cleanBtn.Enabled = true;
            }
        }

        // ---------------- actions ----------------

        private void Clean()
        {
            long bytes;
            var paths = CheckedPaths(out bytes);
            if (paths.Count == 0) return;

            string msg = string.Format(
                "{0:N0} items will be sent to the Recycle Bin, freeing about {1}.\r\n\r\nYou can restore any of them from the Recycle Bin later.",
                paths.Count, Fmt.Size(bytes));

            if (!DarkDialog.Show(this, "Clean up " + Fmt.Size(bytes) + "?", msg, "Move to Recycle Bin", true))
                return;

            _cleanBtn.Enabled = false;
            _status.Text = "Moving to Recycle Bin...";
            Application.DoEvents();

            int ok = FileOps.ToRecycleBin(paths, Handle);

            // remove gone rows
            var gone = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string p in paths)
                if (!File.Exists(p) && !Directory.Exists(p)) gone.Add(p);

            foreach (var g in new[] { _gridDupes, _gridLarge, _gridEmpty, _gridJunk })
            {
                for (int i = g.Rows.Count - 1; i >= 0; i--)
                {
                    var fe = g.Rows[i].Tag as FileEntry;
                    string key = fe != null ? fe.FullPath : g.Rows[i].Tag as string;
                    if (key != null && gone.Contains(key)) g.Rows.RemoveAt(i);
                }
            }

            _status.Text = string.Format("Moved {0:N0} items to the Recycle Bin \u2014 about {1} recovered.", ok, Fmt.Size(bytes));
            UpdateStorage();
            UpdateSelectionInfo();
        }

        private void Export()
        {
            if (_result == null) return;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "HTML report (*.html)|*.html";
                sfd.FileName = "SpaceSweeper-report-" + DateTime.Now.ToString("yyyyMMdd") + ".html";
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(sfd.FileName, Report.BuildHtml(_result));
                    _status.Text = "Report saved to " + sfd.FileName;
                    RevealInExplorer(sfd.FileName);
                }
                catch (Exception ex)
                {
                    _status.Text = "Export failed: " + ex.Message;
                }
            }
        }

        private void RevealInExplorer(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", "\"" + path + "\"");
                else
                    System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F)) { StartScan(); return true; }
            if (keyData == Keys.Escape) { if (_cts != null && _scanning) _cts.Cancel(); return true; }
            if (keyData == (Keys.Control | Keys.A))
            {
                var g = ActiveGrid();
                foreach (DataGridViewRow r in g.Rows) if (r.Visible) r.Cells[0].Value = true;
                UpdateSelectionInfo();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    public class LogoMark : Control
    {
        public LogoMark()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), T.Accent, Color.FromArgb(139, 92, 246), 45f))
                g.FillPath(br, Gfx.Round(r, 9));

            using (var pen = new Pen(Color.White, 2.4f))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                // broom / sweep arc
                g.DrawArc(pen, 7, 7, 17, 17, 200, 250);
                g.DrawLine(pen, 16, 16, 23, 23);
            }
        }
    }

    public static class Native
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
    }
}
