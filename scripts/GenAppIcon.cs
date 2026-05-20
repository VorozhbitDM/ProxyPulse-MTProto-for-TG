using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

static class GenAppIcon
{
    static readonly int[] Sizes = { 16, 24, 32, 48, 64, 128, 256 };

    static void Main(string[] args)
    {
        var root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
        var sourcePath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(root, "src", "ProxyPulse", "app.png");
        var outPath = args.Length > 1
            ? Path.GetFullPath(args[1])
            : Path.Combine(root, "src", "ProxyPulse", "app.ico");

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Icon source not found: " + sourcePath);

        var pngs = new List<PngFrame>();
        using (var src = Image.FromFile(sourcePath))
        {
            foreach (var size in Sizes)
            {
                using (var bmp = RenderCircularIcon(src, size))
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    pngs.Add(new PngFrame(ms.ToArray(), size, size));
                }
            }
        }

        WritePngIco(outPath, pngs);
        Console.WriteLine("Source: " + sourcePath);
        Console.WriteLine("Wrote:  " + outPath + " (" + new FileInfo(outPath).Length + " bytes, " + pngs.Count + " sizes)");
    }

    static Bitmap RenderCircularIcon(Image src, int size)
    {
        var crop = GetCenterSquare(src);
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            using (var clip = new GraphicsPath())
            {
                clip.AddEllipse(0, 0, size, size);
                g.SetClip(clip);
                g.DrawImage(src, new Rectangle(0, 0, size, size), crop, GraphicsUnit.Pixel);
            }
        }

        ApplyCircularAlpha(bmp);
        return bmp;
    }

    static Rectangle GetCenterSquare(Image src)
    {
        int s = Math.Min(src.Width, src.Height);
        return new Rectangle((src.Width - s) / 2, (src.Height - s) / 2, s, s);
    }

    static void ApplyCircularAlpha(Bitmap bmp)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        float cx = (w - 1) / 2f;
        float cy = (h - 1) / 2f;
        float radius = Math.Min(cx, cy);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist > radius)
                {
                    bmp.SetPixel(x, y, Color.Transparent);
                }
                else if (dist > radius - 1.2f)
                {
                    var c = bmp.GetPixel(x, y);
                    float t = (radius - dist) / 1.2f;
                    if (t < 0) t = 0;
                    bmp.SetPixel(x, y, Color.FromArgb((int)(c.A * t), c.R, c.G, c.B));
                }
            }
        }
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
