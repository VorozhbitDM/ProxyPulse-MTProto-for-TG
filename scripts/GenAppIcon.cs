using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class GenAppIcon
{
    static readonly int[] Sizes = { 16, 24, 32, 48, 64, 128, 256 };
    const double DefaultIconScale = 0.78;

    static void Main(string[] args)
    {
        var root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
        var sourcePath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(root, "src", "ProxyPulse", "app.png");
        var outPath = args.Length > 1
            ? Path.GetFullPath(args[1])
            : Path.Combine(root, "src", "ProxyPulse", "app.ico");
        var scale = DefaultIconScale;
        if (args.Length > 2)
        {
            double parsed;
            if (double.TryParse(args[2], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out parsed)
                && parsed > 0.1 && parsed <= 1.0)
            {
                scale = parsed;
            }
        }

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Icon source not found: " + sourcePath);

        var pngs = new List<PngFrame>();
        using (var src = Image.FromFile(sourcePath))
        {
            foreach (var size in Sizes)
            {
                using (var bmp = RenderSquareIcon(src, size, scale))
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    pngs.Add(new PngFrame(ms.ToArray(), size, size));
                }
            }
        }

        WritePngIco(outPath, pngs);
        Console.WriteLine("Source: " + sourcePath);
        Console.WriteLine("Scale:  " + scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        Console.WriteLine("Wrote:  " + outPath + " (" + new FileInfo(outPath).Length + " bytes, " + pngs.Count + " sizes)");
    }

    static Bitmap RenderSquareIcon(Image src, int size, double iconScale)
    {
        var crop = GetCenterSquare(src);
        var drawSize = Math.Max(1, (int)Math.Round(size * iconScale));
        var offset = (size - drawSize) / 2;
        var cornerRadius = Math.Max(2, (int)Math.Round(size * 0.18));
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            using (var clip = CreateRoundedRectPath(0, 0, size, size, cornerRadius))
            {
                g.SetClip(clip);
                g.DrawImage(
                    src,
                    new Rectangle(offset, offset, drawSize, drawSize),
                    crop,
                    GraphicsUnit.Pixel);
                g.ResetClip();
            }
        }

        return bmp;
    }

    static GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius)
    {
        var path = new GraphicsPath();
        var d = Math.Max(2, radius * 2);
        if (width < d || height < d)
        {
            path.AddRectangle(new Rectangle(x, y, width, height));
            return path;
        }

        var arc = new Rectangle(x, y, d, d);
        path.AddArc(arc, 180, 90);
        arc.X = x + width - d;
        path.AddArc(arc, 270, 90);
        arc.Y = y + height - d;
        path.AddArc(arc, 0, 90);
        arc.X = x;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    static Rectangle GetCenterSquare(Image src)
    {
        int s = Math.Min(src.Width, src.Height);
        return new Rectangle((src.Width - s) / 2, (src.Height - s) / 2, s, s);
    }

    static void WritePngIco(string path, IList<PngFrame> frames)
    {
        using (var fs = File.Create(path))
        using (var w = new BinaryWriter(fs))
        {
            w.Write((ushort)0);
            w.Write((ushort)1);
            w.Write((ushort)frames.Count);

            uint offset = (uint)(6 + 16 * frames.Count);
            foreach (var f in frames)
            {
                w.Write((byte)(f.Width >= 256 ? 0 : f.Width));
                w.Write((byte)(f.Height >= 256 ? 0 : f.Height));
                w.Write((byte)0);
                w.Write((byte)0);
                w.Write((ushort)0);
                w.Write((ushort)32);
                w.Write((uint)f.Data.Length);
                w.Write(offset);
                offset += (uint)f.Data.Length;
            }

            foreach (var f in frames)
                w.Write(f.Data);
        }
    }

    sealed class PngFrame
    {
        public readonly byte[] Data;
        public readonly int Width;
        public readonly int Height;

        public PngFrame(byte[] data, int width, int height)
        {
            Data = data;
            Width = width;
            Height = height;
        }
    }
}
