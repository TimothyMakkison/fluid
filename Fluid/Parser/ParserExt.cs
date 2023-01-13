using Parlot;
using Parlot.Fluent;

namespace Fluid.Parser
{
    public static class ParserExtensions
    {
        public static SkipWhiteSpaceOrLines<T> SkipWhiteSpaceOrLines<T>(Parser<T> parser) => new SkipWhiteSpaceOrLines<T>(parser);

        public static SkipOnlyWhiteSpace<T> SkipOnlyWhiteSpace<T>(Parser<T> parser) => new SkipOnlyWhiteSpace<T>(parser);
    }

    public sealed class SkipWhiteSpaceOrLines<T> : Parser<T>
    {
        private readonly Parser<T> _parser;

        public SkipWhiteSpaceOrLines(Parser<T> parser)
        {
            _parser = parser;
        }

        public override bool Parse(ParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Position;

            // Use the scanner's logic to ignore whitespaces since it knows about multi-line grammars
            context.Scanner.SkipWhiteSpaceOrNewLine();

            if (_parser.Parse(context, ref result))
            {
                return true;
            }

            context.Scanner.Cursor.ResetPosition(start);

            return false;
        }
    }
    public sealed class SkipOnlyWhiteSpace<T> : Parser<T>
    {
        private readonly Parser<T> _parser;

        public SkipOnlyWhiteSpace(Parser<T> parser)
        {
            _parser = parser;
        }

        public override bool Parse(ParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Position;

            // Use the scanner's logic to ignore whitespaces since it knows about multi-line grammars
            context.Scanner.SkipWhiteSpace();

            if (_parser.Parse(context, ref result))
            {
                return true;
            }

            context.Scanner.Cursor.ResetPosition(start);

            return false;
        }
    }
}