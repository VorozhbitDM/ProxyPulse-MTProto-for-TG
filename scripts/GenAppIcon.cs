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
            : Path.Combine(root, "docs", "proxy-pulse-app-icon.png");
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
                using (var bmp = RenderSquare(src, size))
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

    static Bitmap RenderSquare(Image src, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(src, 0, 0, size, size);
        }
        return bmp;
    }

    // ICO with embedded PNGs (Vista+). Planes=0 per common tooling; works with CSC Win32 embed.
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
