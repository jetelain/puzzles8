using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesIsland
    {
          internal BridgesState state;
          internal int x, y, count;
          internal BridgesSurrounds adj = new BridgesSurrounds();

          internal BridgesIsland Clone()
          {
              return new BridgesIsland()
              {
                  state = state,
                  x = x,
                  y=y,
                  count = count,
                  adj = adj.Clone()
              };
          }
    }
}
