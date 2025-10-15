using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

class Program
{
    static readonly string BaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnimeQuotes");
    static readonly string BackgroundsDir = Path.Combine(BaseDir, "backgrounds");
    static readonly string QuotesJson = Path.Combine(BaseDir, "quotes.json");
    static readonly string OutPng = Path.Combine(BaseDir, "current.png");

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
    const int SPI_SETDESKWALLPAPER = 0x0014;
    const int SPIF_UPDATEINIFILE = 0x01;
    const int SPIF_SENDWININICHANGE = 0x02;

    static void Main()
    {
        try
        {
            Console.WriteLine("🎌 AnimeQuoteWall - Starting wallpaper generation...");
            InitializeDirectories();
            EnsureQuotesFile();
            var quotes = LoadQuotes(QuotesJson);
            if (quotes.Count == 0) throw new InvalidOperationException("Your quotes.json is empty!");
            Console.WriteLine($"📚 Loaded {quotes.Count} quotes");
            var rng = new Random();
            var selectedQuote = quotes[rng.Next(quotes.Count)];
            Console.WriteLine($"✨ Selected: {selectedQuote.Character} ({selectedQuote.Anime})");
            string? backgroundPath = GetRandomBackgroundImage(rng);
            using var wallpaperBitmap = CreateWallpaperImage(backgroundPath, selectedQuote);
            wallpaperBitmap.Save(OutPng, ImageFormat.Png);
            Console.WriteLine($"💾 Saved: {OutPng}");
            bool success = SetDesktopWallpaper(OutPng);
            Console.WriteLine(success ? "🖼️ Wallpaper set!" : "❌ Failed to set wallpaper");
            try { GenerateQuoteFrames(backgroundPath, selectedQuote); }
            catch (Exception ex) { Console.WriteLine($"⚠️ Frame gen failed: {ex.Message}"); }
        }
        catch (Exception ex) { Console.WriteLine($"❌ Error: {ex.Message}"); Environment.Exit(1); }
    }

    static void InitializeDirectories() 
    { 
        Directory.CreateDirectory(BaseDir); 
        Directory.CreateDirectory(BackgroundsDir); 
        Console.WriteLine($"📁 Dirs: {BaseDir}"); 
    }
    
    static void EnsureQuotesFile() 
    { 
        if (!File.Exists(QuotesJson)) 
        { 
            var seed = new List<Quote> 
            { 
                new Quote{ Anime="Naruto", Character="Itachi", Text="People live bound by what they accept as true." }, 
                new Quote{ Anime="Attack on Titan", Character="Levi", Text="The only thing we're allowed to do is believe." }, 
                new Quote{ Anime="FMA Brotherhood", Character="Edward", Text="A lesson without pain is meaningless." } 
            }; 
            File.WriteAllText(QuotesJson, JsonSerializer.Serialize(seed, new JsonSerializerOptions{ WriteIndented=true })); 
            Console.WriteLine($"✅ Created quotes"); 
        } 
    }
    
    static string? GetRandomBackgroundImage(Random rng) 
    { 
        if (!Directory.Exists(BackgroundsDir)) return null; 
        var ext = new[] { ".jpg", ".jpeg", ".png", ".bmp" }; 
        var bg = Directory.EnumerateFiles(BackgroundsDir).Where(f => ext.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase))).OrderBy(_ => rng.Next()).FirstOrDefault(); 
        if (bg != null) Console.WriteLine($"🖼️ BG: {Path.GetFileName(bg)}"); 
        else Console.WriteLine("🎨 No BG found"); 
        return bg; 
    }
    
    static Bitmap CreateWallpaperImage(string? backgroundPath, Quote quote) 
    { 
        Bitmap bitmap = LoadBackgroundBitmap(backgroundPath, 2560, 1440); 
        DrawQuote(bitmap, $"{quote.Text}\n— {quote.Character} ({quote.Anime})"); 
        return bitmap; 
    }
    
    static Bitmap LoadBackgroundBitmap(string? path, int w, int h) 
    { 
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) 
        { 
            try { return new Bitmap(path); } 
            catch { Console.WriteLine("⚠️ BG load failed"); } 
        } 
        var bmp = new Bitmap(w, h); 
        using var g = Graphics.FromImage(bmp); 
        g.Clear(Color.FromArgb(24, 26, 31)); 
        return bmp; 
    }
    
    static bool SetDesktopWallpaper(string path) 
    { 
        if (!File.Exists(path)) return false; 
        return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE); 
    }
    
    static List<Quote> LoadQuotes(string path) 
    { 
        var json = File.ReadAllText(path); 
        var list = JsonSerializer.Deserialize<List<Quote>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Quote>(); 
        return list.Where(q => !string.IsNullOrWhiteSpace(q.Text) && !string.IsNullOrWhiteSpace(q.Character)).ToList(); 
    }
    
    static void DrawQuote(Bitmap bmp, string txt, float prog = 0f) 
    { 
        using var g = Graphics.FromImage(bmp); 
        g.SmoothingMode = SmoothingMode.AntiAlias; 
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; 
        float sf = Math.Min(bmp.Width / 2560f, bmp.Height / 1440f); 
        int pad = (int)(60 * sf), tpad = (int)(28 * sf), cr = (int)(28 * sf), pw = (int)(bmp.Width * 0.65f); 
        float fs = Math.Max(bmp.Height / 30f, 24f); 
        using var fnt = CreateAnimeFont(fs); 
        var lines = WrapText(txt, fnt, pw - (tpad * 2), g); 
        int lh = (int)(fnt.GetHeight(g) * 1.35f), th = lines.Count * lh + (tpad * 2); 
        float p = MathF.Sin(prog * MathF.PI * 2f); 
        int px = bmp.Width - pw - pad; 
        int bpy = th < bmp.Height / 2 ? (bmp.Height - th) / 2 : pad; 
        int py = Math.Clamp(bpy + (int)(p * sf * 12f), 0, Math.Max(0, bmp.Height - th)); 
        var pr = new Rectangle(px, py, pw, th); 
        using var pp = CreateRoundedRectangle(pr, cr); 
        var c1 = Color.FromArgb(Clamp(200 + (int)(30 * p)), 10, 10, 25); 
        var c2 = Color.FromArgb(Clamp(180 + (int)(30 * p)), 20, 20, 40); 
        using (var gb = new LinearGradientBrush(pr, c1, c2, LinearGradientMode.Vertical)) g.FillPath(gb, pp); 
        using (var bp = new Pen(Color.FromArgb(Clamp(100 + (int)(20 * p)), 255, 255, 255), sf * 2)) g.DrawPath(bp, pp); 
        int ty = py + tpad; 
        float ot = Math.Max(3, (6f + p) * sf); 
        foreach (var ln in lines) 
        { 
            using var tp = new GraphicsPath(); 
            tp.AddString(ln, fnt.FontFamily, (int)fnt.Style, fnt.Size, new Point(px + tpad, ty), StringFormat.GenericTypographic); 
            using (var gp = new Pen(Color.FromArgb(150, 0, 0, 0), ot + 2) { LineJoin = LineJoin.Round }) g.DrawPath(gp, tp); 
            using (var op = new Pen(Color.FromArgb(Clamp(220 + (int)(15 * p)), 30, 30, 60), ot) { LineJoin = LineJoin.Round }) g.DrawPath(op, tp); 
            var tc1 = Color.FromArgb(255, 255, 255, 255); 
            var tc2 = Color.FromArgb(Clamp(240 + (int)(15 * p)), 240, 245, 255); 
            using (var tb = new LinearGradientBrush(new Rectangle(px + tpad, ty, pw, lh), tc1, tc2, LinearGradientMode.Vertical)) g.FillPath(tb, tp); 
            ty += lh; 
        } 
    }
    
    static List<string> WrapText(string txt, Font fnt, int mw, Graphics g) 
    { 
        var ws = txt.Split(' ', StringSplitOptions.RemoveEmptyEntries); 
        var ls = new List<string>(); 
        var cl = ""; 
        foreach (var w in ws) 
        { 
            var tl = string.IsNullOrEmpty(cl) ? w : cl + " " + w; 
            if (g.MeasureString(tl, fnt).Width > mw && !string.IsNullOrEmpty(cl)) 
            { 
                ls.Add(cl); 
                cl = w; 
            } 
            else cl = tl; 
        } 
        if (!string.IsNullOrEmpty(cl)) ls.Add(cl); 
        return ls; 
    }
    
    static GraphicsPath CreateRoundedRectangle(Rectangle r, int rad) 
    { 
        var p = new GraphicsPath(); 
        rad = Math.Min(rad, Math.Min(r.Width / 2, r.Height / 2)); 
        int d = rad * 2; 
        p.AddArc(r.Left, r.Top, d, d, 180, 90); 
        p.AddArc(r.Right - d, r.Top, d, d, 270, 90); 
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); 
        p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90); 
        p.CloseFigure(); 
        return p; 
    }
    
    static int Clamp(int v) => Math.Clamp(v, 0, 255);
    
    static void GenerateQuoteFrames(string? bgp, Quote q, int fc = 16) 
    { 
        if (fc <= 0) return; 
        string fd = Path.Combine(BaseDir, "frames", DateTime.Now.ToString("yyyyMMdd_HHmmss")); 
        Directory.CreateDirectory(fd); 
        using var bg = LoadBackgroundBitmap(bgp, 2560, 1440); 
        string t = $"{q.Text}\n— {q.Character} ({q.Anime})"; 
        for (int i = 0; i < fc; i++) 
        { 
            using var f = new Bitmap(bg); 
            DrawQuote(f, t, i / (float)fc); 
            f.Save(Path.Combine(fd, $"frame_{i:D3}.png"), ImageFormat.Png); 
        } 
        Console.WriteLine($"🎞️ {fc} frames: {fd}"); 
    }
    
    static Font CreateAnimeFont(float sz) 
    { 
        var fs = new[] { "Comic Sans MS", "Trebuchet MS", "Verdana", "Segoe UI", "Calibri" }; 
        foreach (var fn in fs) 
        { 
            try 
            { 
                using var tf = new Font(fn, sz, FontStyle.Bold, GraphicsUnit.Pixel); 
                if (tf.Name == fn) return new Font(fn, sz, FontStyle.Bold, GraphicsUnit.Pixel); 
            } 
            catch { } 
        } 
        return new Font(FontFamily.GenericSansSerif, sz, FontStyle.Bold, GraphicsUnit.Pixel); 
    }
}

public class Quote 
{ 
    public string Text { get; set; } = ""; 
    public string Anime { get; set; } = ""; 
    public string Character { get; set; } = ""; 
}
