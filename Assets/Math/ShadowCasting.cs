using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowCasting {
    /// <param name="blocksLight">A function that accepts the X and Y coordinates of a tile and determines whether the
    /// given tile blocks the passage of light. The function must be able to accept coordinates that are out of bounds.
    /// </param>
    /// <param name="setVisible">A function that sets a tile to be visible, given its X and Y coordinates. The function
    /// must ignore coordinates that are out of bounds.
    /// </param>
    /// <param name="getDistance">A function that takes the X and Y coordinate of a point where X >= 0,
    /// Y >= 0, and X >= Y, and returns the distance from the point to the origin.
    /// </param>
    public ShadowCasting(Func<int, int, bool> blocksLight, Action<int, int> setVisible,
                                Func<int, int, int> getDistance) {
        BlocksLight = blocksLight;
        GetDistance = getDistance;
        SetVisible = setVisible;
    }

    public void Compute(int originX, int originY, int rangeLimit) {
        SetVisible(originX, originY);
        for(uint octant = 0; octant < 8; octant++) Compute(octant, originX, originY, rangeLimit, 1, new Slope(1, 1), new Slope(0, 1));
    }

    struct Slope // represents the slope Y/X as a rational number
    {
        public Slope(int y, int x) { Y = y; X = x; }
        public readonly int Y, X;
    }

    void Compute(uint octant, int originX, int originY, int rangeLimit, int x, Slope top, Slope bottom) {
        for(; (uint)x <= (uint)rangeLimit; x++) // rangeLimit < 0 || x <= rangeLimit
        {
            // compute the Y coordinates where the top vector leaves the column (on the right) and where the bottom vector
            // enters the column (on the left). this equals (x+0.5)*top+0.5 and (x-0.5)*bottom+0.5 respectively, which can
            // be computed like (x+0.5)*top+0.5 = (2(x+0.5)*top+1)/2 = ((2x+1)*top+1)/2 to avoid floating point math
            int topY = top.X == 1 ? x : ((x * 2 + 1) * top.Y + top.X - 1) / (top.X * 2); // the rounding is a bit tricky, though
            int bottomY = bottom.Y == 0 ? 0 : ((x * 2 - 1) * bottom.Y + bottom.X) / (bottom.X * 2);

            int wasOpaque = -1; // 0:false, 1:true, -1:not applicable
            for(int y = topY; y >= bottomY; y--) {
                int tx = originX, ty = originY;
                switch(octant) // translate local coordinates to map coordinates
                {
                    case 0: tx += x; ty -= y; break;
                    case 1: tx += y; ty -= x; break;
                    case 2: tx -= y; ty -= x; break;
                    case 3: tx -= x; ty -= y; break;
                    case 4: tx -= x; ty += y; break;
                    case 5: tx -= y; ty += x; break;
                    case 6: tx += y; ty += x; break;
                    case 7: tx += x; ty += y; break;
                }

                bool inRange = rangeLimit < 0 || GetDistance(tx, ty) <= rangeLimit;
                
                //if(inRange) SetVisible(tx, ty);
                // NOTE: use the next line instead if you want the algorithm to be symmetrical
                if(inRange && (y != topY || top.Y * x >= top.X * y) && (y != bottomY || bottom.Y * x <= bottom.X * y)) SetVisible(tx, ty);

                bool isOpaque = !inRange || BlocksLight(tx, ty);
                if(x != rangeLimit) {
                    if(isOpaque) {
                        if(wasOpaque == 0) // if we found a transition from clear to opaque, this sector is done in this column, so
                        {                  // adjust the bottom vector upwards and continue processing it in the next column.
                            Slope newBottom = new Slope(y * 2 + 1, x * 2 - 1); // (x*2-1, y*2+1) is a vector to the top-left of the opaque tile
                            if(!inRange || y == bottomY) { bottom = newBottom; break; } // don't recurse unless we have to
                            else Compute(octant, originX, originY, rangeLimit, x + 1, top, newBottom);
                        }
                        wasOpaque = 1;
                    } else // adjust top vector downwards and continue if we found a transition from opaque to clear
                      {    // (x*2+1, y*2+1) is the top-right corner of the clear tile (i.e. the bottom-right of the opaque tile)
                        if(wasOpaque > 0) top = new Slope(y * 2 + 1, x * 2 + 1);
                        wasOpaque = 0;
                    }
                }
            }

            if(wasOpaque != 0) break; // if the column ended in a clear tile, continue processing the current sector
        }
    }

    readonly Func<int, int, bool> BlocksLight;
    readonly Func<int, int, int> GetDistance;
    readonly Action<int, int> SetVisible;
}