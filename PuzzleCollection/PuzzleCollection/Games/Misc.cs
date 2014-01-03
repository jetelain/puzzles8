using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    internal static class Misc
    {

        internal static bool IS_MOUSE_DOWN(Buttons m) 
        {
            return ((int)((m) - Buttons.LEFT_BUTTON) <=
                               (int)(Buttons.RIGHT_BUTTON - Buttons.LEFT_BUTTON));
        }
        internal static bool IS_MOUSE_DRAG(Buttons m) 
        {
            return ((int)((m) - Buttons.LEFT_DRAG) <=
                               (int)(Buttons.RIGHT_DRAG - Buttons.LEFT_DRAG));
        }
        internal static bool IS_MOUSE_RELEASE(Buttons m) 
        {
            return ((int)((m) - Buttons.LEFT_RELEASE) <=
                               (int)(Buttons.RIGHT_RELEASE - Buttons.LEFT_RELEASE));
        }
        internal static bool IS_CURSOR_MOVE(Buttons m) 
        {
            return ((m) == Buttons.CURSOR_UP || (m) == Buttons.CURSOR_DOWN ||
                            (m) == Buttons.CURSOR_RIGHT || (m) == Buttons.CURSOR_LEFT);
        }
        internal static bool IS_CURSOR_SELECT(Buttons m) 
        {
            return ((m) == Buttons.CURSOR_SELECT || (m) == Buttons.CURSOR_SELECT2);
        }

        internal static void move_cursor(Buttons button, ref int x, ref int y, int maxw, int maxh, bool wrap)
        {
            int dx = 0, dy = 0;
            switch (button)
            {
                case Buttons.CURSOR_UP: dy = -1; break;
                case Buttons.CURSOR_DOWN: dy = 1; break;
                case Buttons.CURSOR_RIGHT: dx = 1; break;
                case Buttons.CURSOR_LEFT: dx = -1; break;
                default: return;
            }
            if (wrap)
            {
                x = (x + dx + maxw) % maxw;
                y = (y + dy + maxh) % maxh;
            }
            else
            {
                x = Math.Min(Math.Max(x + dx, 0), maxw - 1);
                y = Math.Min(Math.Max(y + dy, 0), maxh - 1);
            }
        }
        internal static  void game_mkhighlight(Frontend fe, float[] ret,
                      int background, int highlight, int lowlight)
        {
            fe.frontend_default_colour(ret,background * 3);
            game_mkhighlight_specific(fe, ret, background, highlight, lowlight);
        }
        internal static void game_mkhighlight_specific(Frontend fe, float[] ret,
                   int background, int highlight, int lowlight)
        {
            float max;
            int i;

            /*
             * Drop the background colour so that the highlight is
             * noticeably brighter than it while still being under 1.
             */
            max = ret[background * 3];
            for (i = 1; i < 3; i++)
                if (ret[background * 3 + i] > max)
                    max = ret[background * 3 + i];
            if (max * 1.2F > 1.0F)
            {
                for (i = 0; i < 3; i++)
                    ret[background * 3 + i] /= (max * 1.2F);
            }

            for (i = 0; i < 3; i++)
            {
                if (highlight >= 0)
                    ret[highlight * 3 + i] = ret[background * 3 + i] * 1.2F;
                if (lowlight >= 0)
                    ret[lowlight * 3 + i] = ret[background * 3 + i] * 0.8F;
            }
        }

    }
}
