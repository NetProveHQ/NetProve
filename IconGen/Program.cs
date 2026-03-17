using System;
using System.IO;
using System.Collections.Generic;

// Generate NetPulse icon: "N" and "P" interlocked with gradient background

var sizes = new[] { 16, 32, 48, 64, 128, 256 };
var entries = new List<byte[]>();
var datas = new List<byte[]>();
int offset = 6 + 16 * sizes.Length;

foreach (int size in sizes)
{
    byte[] bmpData = DrawIcon(size);
    var entry = new byte[16];
    entry[0] = (byte)(size < 256 ? size : 0);
    entry[1] = (byte)(size < 256 ? size : 0);
    BitConverter.GetBytes((ushort)1).CopyTo(entry, 4);
    BitConverter.GetBytes((ushort)32).CopyTo(entry, 6);
    BitConverter.GetBytes(bmpData.Length).CopyTo(entry, 8);
    BitConverter.GetBytes(offset).CopyTo(entry, 12);
    entries.Add(entry);
    datas.Add(bmpData);
    offset += bmpData.Length;
}

string icoPath = @"C:\Users\ASUS\NetPulse\NetPulse\app.ico";
using var fs = File.Create(icoPath);
fs.Write(new byte[] { 0, 0, 1, 0 });
fs.Write(BitConverter.GetBytes((ushort)sizes.Length));
foreach (var e in entries) fs.Write(e);
foreach (var d in datas) fs.Write(d);
Console.WriteLine($"Icon created: {new FileInfo(icoPath).Length} bytes");

static byte[] DrawIcon(int size)
{
    var pixels = new byte[size * size * 4]; // BGRA bottom-up
    double cx = size / 2.0, cy = size / 2.0;
    double r = size / 2.0 - 0.5;
    double cornerR = size * 0.22; // rounded square corner radius

    for (int y = 0; y < size; y++)
    {
        int by = size - 1 - y; // bottom-up for BMP
        for (int x = 0; x < size; x++)
        {
            int idx = (by * size + x) * 4;

            // Rounded rectangle check
            double margin = size * 0.02;
            double rx = size / 2.0 - margin;
            double ry = size / 2.0 - margin;
            double dx = Math.Abs(x - cx);
            double dy = Math.Abs(y - cy);

            bool inside = false;
            double edgeDist = 0;

            if (dx <= rx - cornerR && dy <= ry)
            { inside = true; edgeDist = ry - dy; }
            else if (dx <= rx && dy <= ry - cornerR)
            { inside = true; edgeDist = rx - dx; }
            else if (dx > rx - cornerR && dy > ry - cornerR)
            {
                double cdx = dx - (rx - cornerR);
                double cdy = dy - (ry - cornerR);
                double cd = Math.Sqrt(cdx * cdx + cdy * cdy);
                if (cd <= cornerR)
                { inside = true; edgeDist = cornerR - cd; }
            }

            if (!inside) continue;

            int alpha = edgeDist < 1.0 ? (int)(255 * edgeDist) : 255;

            // Background gradient: deep blue to purple-blue (diagonal)
            double t = ((double)x / size + (double)y / size) / 2.0;
            int bgR = Lerp(15, 45, t);    // #0F172A -> #2D1B5E
            int bgG = Lerp(23, 27, t);
            int bgB = Lerp(42, 94, t);

            // Draw the letters "N" and "P"
            double fx = (double)x / size;
            double fy = (double)y / size;

            // Letter boundaries
            bool isN = DrawN(fx, fy, size);
            bool isP = DrawP(fx, fy, size);

            if (isN)
            {
                // N color: cyan-blue #06B6D4 -> #3B82F6
                double lt = fy;
                int lr = Lerp(6, 59, lt);
                int lg = Lerp(182, 130, lt);
                int lb = Lerp(212, 246, lt);
                pixels[idx] = (byte)lb; pixels[idx + 1] = (byte)lg; pixels[idx + 2] = (byte)lr; pixels[idx + 3] = (byte)alpha;
            }
            else if (isP)
            {
                // P color: purple-pink #8B5CF6 -> #EC4899
                double lt = fy;
                int lr = Lerp(139, 236, lt);
                int lg = Lerp(92, 72, lt);
                int lb = Lerp(246, 153, lt);
                pixels[idx] = (byte)lb; pixels[idx + 1] = (byte)lg; pixels[idx + 2] = (byte)lr; pixels[idx + 3] = (byte)alpha;
            }
            else
            {
                pixels[idx] = (byte)bgB; pixels[idx + 1] = (byte)bgG; pixels[idx + 2] = (byte)bgR; pixels[idx + 3] = (byte)alpha;
            }
        }
    }

    // BMP header
    var ms = new MemoryStream();
    var bw = new BinaryWriter(ms);
    bw.Write(40); bw.Write(size); bw.Write(size * 2);
    bw.Write((ushort)1); bw.Write((ushort)32);
    bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
    bw.Write(pixels);
    int rowBytes = (size + 31) / 32 * 4;
    bw.Write(new byte[rowBytes * size]);
    return ms.ToArray();
}

static bool DrawN(double fx, double fy, int size)
{
    // N letter positioned left-center, overlapping with P
    // N occupies roughly x: 0.12-0.55, y: 0.20-0.80
    double stroke = Math.Max(0.06, 2.5 / size);

    double nx1 = 0.15, nx2 = 0.55;
    double ny1 = 0.20, ny2 = 0.80;

    // Left vertical stroke
    if (fx >= nx1 && fx <= nx1 + stroke && fy >= ny1 && fy <= ny2) return true;
    // Right vertical stroke
    if (fx >= nx2 - stroke && fx <= nx2 && fy >= ny1 && fy <= ny2) return true;
    // Diagonal stroke from top-left to bottom-right
    double expectedX = nx1 + stroke / 2 + (fy - ny1) / (ny2 - ny1) * (nx2 - nx1 - stroke);
    if (fy >= ny1 && fy <= ny2 && Math.Abs(fx - expectedX) < stroke * 0.7) return true;

    return false;
}

static bool DrawP(double fx, double fy, int size)
{
    // P letter positioned right-center, overlapping with N
    // P occupies roughly x: 0.45-0.85, y: 0.20-0.80
    double stroke = Math.Max(0.06, 2.5 / size);

    double px1 = 0.45, px2 = 0.85;
    double py1 = 0.20, py2 = 0.80;

    // Left vertical stroke of P
    if (fx >= px1 && fx <= px1 + stroke && fy >= py1 && fy <= py2) return true;

    // Top horizontal stroke
    if (fy >= py1 && fy <= py1 + stroke && fx >= px1 && fx <= px2 - 0.05) return true;

    // Middle horizontal stroke (at ~50% height)
    double midY = (py1 + py2) * 0.5;
    if (fy >= midY - stroke / 2 && fy <= midY + stroke / 2 && fx >= px1 && fx <= px2 - 0.05) return true;

    // Right curved part of P (semicircle from top to middle)
    double bowlCx = px2 - 0.05;
    double bowlCy = (py1 + midY) / 2.0;
    double bowlRx = 0.0; // will be set
    double bowlRy = (midY - py1) / 2.0;

    // Right arc
    if (fy >= py1 && fy <= midY)
    {
        double rightEdge = px2 - 0.05;
        double arcCy = (py1 + midY) / 2.0;
        double arcR = (midY - py1) / 2.0;

        // Check if on the right arc (semicircle)
        double normY = (fy - arcCy) / arcR;
        if (Math.Abs(normY) <= 1.0)
        {
            double arcX = rightEdge + Math.Sqrt(1.0 - normY * normY) * 0.08;
            // Actually, make a proper rounded bowl
            double expectedRightX = px1 + stroke + Math.Sqrt(Math.Max(0, 1.0 - normY * normY)) * (rightEdge - px1 - stroke);
            if (fx >= expectedRightX - stroke && fx <= expectedRightX + stroke * 0.3)
                return true;
        }
    }

    return false;
}

static int Lerp(int a, int b, double t) => (int)(a + (b - a) * Math.Clamp(t, 0, 1));
