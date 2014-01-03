using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.CommonDX
{
    /// <summary>
    /// Display an overlay text with FPS and ms/frame counters.
    /// </summary>
    public class FpsRenderer
    {
        TextFormat textFormat;
        Brush sceneColorBrush;
        Stopwatch clock;
        double totalTime;
        long frameCount;
        double measuredFPS;


        /// <summary>
        /// Initializes a new instance of <see cref="FpsRenderer"/> class.
        /// </summary>
        public FpsRenderer()
        {
            Show = true;
        }


        public bool Show { get; set; }


        public virtual void Initialize(DeviceManager deviceManager)
        {
            sceneColorBrush = new SolidColorBrush(deviceManager.ContextDirect2D, Color.Black);
            textFormat = new TextFormat(deviceManager.FactoryDirectWrite, "Calibri", 20) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center };
            clock = Stopwatch.StartNew();
            deviceManager.ContextDirect2D.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
        }


        public virtual void Render(TargetBase target)
        {
            if (!Show)
                return;


            frameCount++;
            var timeElapsed = (double)clock.ElapsedTicks / Stopwatch.Frequency; ;
            totalTime += timeElapsed;
            if (totalTime >= 1.0f)
            {
                measuredFPS = (double)frameCount / totalTime;
                frameCount = 0;
                totalTime = 0.0;
            }


            var context2D = target.DeviceManager.ContextDirect2D;


            context2D.BeginDraw();
            context2D.Transform = Matrix.Identity;
            context2D.DrawText(string.Format("{0:F2} FPS ({1:F1} ms)", measuredFPS, timeElapsed * 1000.0), textFormat, new RectangleF(8, 8, 8 + 256, 8 + 16), sceneColorBrush);
            context2D.EndDraw();


            clock.Restart();
        }
    }

}
