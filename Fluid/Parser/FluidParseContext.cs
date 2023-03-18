using Fluid.Ast;
using Parlot;
using Parlot.Fluent;
using System.Collections;

namespace Fluid.Parser
{
    public class FluidParseContext : ParseContext
    {
        public FluidParseContext(string text, bool useNewLines = false) : base(new Scanner(text), useNewLines)
        {
        }

        public TextSpanStatement PreviousTextSpanStatement { get; set; }
        public bool StripNextTextSpanStatement { get; set; }
        public bool NonGreedyStripNextTextSpanStatement { get; set; }
        public bool PreviousIsTag { get; set; }
        public bool PreviousIsOutput { get; set; }
        public bool PreviousIsEscape{ get; set; }
        public int LiquidStart {get;set;}
        public bool InsideLiquidTag { get; set; } // Used in the {% liquid %} tag to ensure a new line corresponds to '%}'
    }
}
