using PuzzleCollection.CommonDX;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public class Drawing : IDisposable
    {
        private readonly List<Color> colors = new List<Color>();
        private readonly List<SolidColorBrush> brushes = new List<SolidColorBrush>();

        private readonly SharpDX.Direct2D1.DeviceContext context2D;
        private readonly SharpDX.Direct2D1.Factory factory2D;
        private readonly SharpDX.DirectWrite.Factory factoryDW;

        internal Drawing(DeviceManager deviceManager)
        {
            context2D = deviceManager.ContextDirect2D;
            factoryDW = deviceManager.FactoryDirectWrite;
            factory2D = deviceManager.FactoryDirect2D;
        }

        internal void draw_rect_outline(int x, int y, int w, int h, int colour)
        {
            context2D.DrawRectangle(new RectangleF(x, y, w, h), brushes[colour]);
        }

        internal void draw_rect(int x, int y, int w, int h, int colour)
        {
            context2D.FillRectangle(new RectangleF(x, y, w, h), brushes[colour]);
        }

        internal void draw_update(int p1, int p2, int p3, int p4)
        {
            // Nothing to do
        }

        internal void draw_circle(int cx, int cy, int radius, int fillcolour, int outlinecolour)
        {
            Ellipse ellipse = new Ellipse(new Vector2(cx, cy), radius, radius);
            if (fillcolour != -1)
            {
                context2D.FillEllipse(ellipse, brushes[fillcolour]);
            }
            if (outlinecolour != -1)
            {
                context2D.DrawEllipse(ellipse, brushes[outlinecolour]);
            }
        }

        internal void draw_line(int x1, int y1, int x2, int y2, int colour)
        {
            context2D.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), brushes[colour]);
        }
        Dictionary<int, TextFormat> textFormats = new Dictionary<int, TextFormat>();

        internal void draw_text(int x, int y, int fonttype, int fontsize, int align, int colour, string text)
        {
            TextFormat textFormat;
            if (!textFormats.TryGetValue(fontsize, out textFormat))
            {
                textFormat = new TextFormat(factoryDW, "Calibri", fontsize);
                textFormats.Add(fontsize, textFormat);
            }
            var layout = new TextLayout(factoryDW, text, textFormat, 1024, 1024);
            var metrics = layout.Metrics;
            var origin = new Vector2(x, y);
            if ((align & ALIGN_HCENTRE) != 0)
            {
                origin.X -= metrics.Width / 2;
            }
            origin.Y -= metrics.Height / 2;
            context2D.DrawTextLayout(origin, layout, brushes[colour]);
            layout.Dispose();
        }

        internal const int ALIGN_VNORMAL =0x000;
        internal const int ALIGN_VCENTRE =0x100;

        internal const int ALIGN_HLEFT   =0x000;
        internal const int ALIGN_HCENTRE =0x001;
        internal const int ALIGN_HRIGHT  =0x002;

        internal const int FONT_FIXED    =0;
        internal const int FONT_VARIABLE = 1;

        internal int AddColor(Color color)
        {
            int index = colors.Count;
            colors.Add(color);
            brushes.Add(new SolidColorBrush(context2D, color));
            return index;
        }

        internal void Reset()
        {
            foreach (var brush in brushes)
            {
                brush.Dispose();
            }
            colors.Clear();
            brushes.Clear();
        }

        internal void draw_polygon(int[] coords, int npoints, int fillcolour, int outlinecolour)
        {
            var pg = new PathGeometry(factory2D);
            using (var pgs = pg.Open())
            {
                pgs.BeginFigure(new Vector2(coords[0], coords[1]), FigureBegin.Filled);
                for (int i = 1; i < npoints; i++)
                {
                    pgs.AddLine(new Vector2(coords[i * 2], coords[i * 2 + 1]));
                }
                pgs.EndFigure(FigureEnd.Closed);
                pgs.Close();
            }

            if (fillcolour != -1)
            {
                context2D.FillGeometry(pg, brushes[fillcolour]);
            }
            if (outlinecolour != -1)
            {
                context2D.DrawGeometry(pg, brushes[outlinecolour]);
            }
            pg.Dispose();
        }

        internal void unclip()
        {
            context2D.PopAxisAlignedClip();
        }

        internal void clip(int x, int y, int w, int h)
        {
            context2D.PushAxisAlignedClip(new RectangleF(x, y, w, h), AntialiasMode.Aliased);
        }

        public void Dispose()
        {
            Reset();
            foreach(var format in textFormats.Values)
            {
                format.Dispose();
            }
            textFormats.Clear();
        }

        internal void draw_rect_corners(int acx, int acy, int p, int COL_CURSOR)
        {
        }
    }
}
