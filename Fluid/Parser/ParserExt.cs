using Parlot;
using Parlot.Fluent;
using System;
using System.Collections.Generic;

namespace Fluid.Parser
{
    public static class ParserExtensions
    {
        public static SkipWhiteSpaceOrLines<T> SkipWhiteSpaceOrLines<T>(Parser<T> parser) => new SkipWhiteSpaceOrLines<T>(parser);

        public static ResettingNot<T> ResettingNot<T>(Parser<T> parser) => new ResettingNot<T>(parser);

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

    public sealed class ResettingSwitch<T, U> : Parser<U>
    {
        private readonly Parser<T> _previousParser;
        private readonly Func<ParseContext, T, Parser<U>> _action;
        public ResettingSwitch(Parser<T> previousParser, Func<ParseContext, T, Parser<U>> action)
        {
            _previousParser = previousParser ?? throw new ArgumentNullException(nameof(previousParser));
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public override bool Parse(ParseContext context, ref ParseResult<U> result)
        {
            var previousResult = new ParseResult<T>();
            var start = context.Scanner.Cursor.Position;

            if (!_previousParser.Parse(context, ref previousResult))
            {
                return false;
            }

            var nextParser = _action(context, previousResult.Value);

            if (nextParser == null)
            {
                context.Scanner.Cursor.ResetPosition(start);
                return false;
            }

            var parsed = new ParseResult<U>();

            if (nextParser.Parse(context, ref parsed))
            {
                result.Set(parsed.Start, parsed.End, parsed.Value);
                return true;
            }

            return false;
        }
    }

    public sealed class ResettingNot<T> : Parser<T>
    {
        private readonly Parser<T> _parser;

        public ResettingNot(Parser<T> parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public override bool Parse(ParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Position;

            var matches = _parser.Parse(context, ref result);
            context.Scanner.Cursor.ResetPosition(start);
            return !matches;
        }
    }
}