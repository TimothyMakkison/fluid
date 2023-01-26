using Fluid.Ast;
using Parlot;
using Parlot.Fluent;
using System;

namespace Fluid.Parser
{
    public static class ScribanTagParsers
    {
        public static Parser<TagResult> TagStart(bool skipWhiteSpace = false) => new TagStartParser(skipWhiteSpace);
        public static Parser<TagResult> TagEnd(bool skipWhiteSpace = false) => new TagEndParser(skipWhiteSpace);
        public static Parser<TagResult> OutputTagStart(bool skipWhiteSpace = false) => new OutputTagStartParser(skipWhiteSpace);
        public static Parser<TagResult> OutputTagEnd(bool skipWhiteSpace = false) => new OutputTagEndParser(skipWhiteSpace);

        public static Parser<int> EscapeBlockStart() => new EscapeBlockStartParser();
        public static Parser<int> EscapeBlockEnd(int length) => new EscapeBlockEndParser(length);

        public static ResettingSwitch<T, U> ResettingSwitch<T, U>(this Parser<T> previousParser, Func<ParseContext, T, Parser<U>> action) => new ResettingSwitch<T,U>(previousParser, action);


        private sealed class TagStartParser : Parser<TagResult>
        {
            private readonly bool _skipWhiteSpace;

            public TagStartParser(bool skipWhiteSpace = false)
            {
                _skipWhiteSpace = skipWhiteSpace;
            }

            public override bool Parse(ParseContext context, ref ParseResult<TagResult> result)
            {
                if (_skipWhiteSpace)
                {
                    context.SkipWhiteSpace();
                }

                var start = context.Scanner.Cursor.Position;

                var p = (FluidParseContext)context;

                if(start.Offset < p.LiquidStart)
                {
                    p.InsideLiquidTag = false;
                }
                if (p.InsideLiquidTag)
                {
                    result.Set(start.Offset, context.Scanner.Cursor.Offset, TagResult.TagOpen);
                    return true;
                }

                if (context.Scanner.ReadChar('{') && context.Scanner.ReadChar('{'))
                {

                    var trim = context.Scanner.ReadChar('-');

                    if (p.PreviousTextSpanStatement != null)
                    {
                        if (trim)
                        {
                            p.PreviousTextSpanStatement.StripRight = true;
                        }

                        p.PreviousTextSpanStatement.NextIsTag = true;

                        p.PreviousTextSpanStatement = null;
                    }

                    result.Set(start.Offset, context.Scanner.Cursor.Offset, trim ? TagResult.TagOpenTrim : TagResult.TagOpen);
                    p.LiquidStart = context.Scanner.Cursor.Offset;
                    p.InsideLiquidTag = true;
                    return true;
                }
                else
                {
                    context.Scanner.Cursor.ResetPosition(start);
                    return false;
                }
            }
        }

        private sealed class TagEndParser : Parser<TagResult>
        {
            private readonly bool _skipWhiteSpace;

            public TagEndParser(bool skipWhiteSpace = false)
            {
                _skipWhiteSpace = skipWhiteSpace;
            }

            public override bool Parse(ParseContext context, ref ParseResult<TagResult> result)
            {
                var p = (FluidParseContext)context;

                var newLineIsPresent = false;
                var semiColonIsPresent = false;

                if(context.Scanner.Cursor.Offset < p.LiquidStart)
                {
                    p.InsideLiquidTag = false;
                }

                if (!p.InsideLiquidTag )
                {
                    return true;
                }

                if (_skipWhiteSpace)
                {
                    if (p.InsideLiquidTag)
                    {
                        var cursor = context.Scanner.Cursor;

                        while (Character.IsWhiteSpace(cursor.Current))
                        {
                            cursor.Advance();
                        }

                        while (cursor.Current == ';')
                        {
                            semiColonIsPresent = true;
                            cursor.Advance();
                        }

                        if (Character.IsNewLine(cursor.Current))
                        {
                            newLineIsPresent = true;
                            while (Character.IsNewLine(cursor.Current))
                            {
                                cursor.Advance();
                            }
                        }
                    }
                    else
                    {
                        context.SkipWhiteSpace();
                    }
                }

                var start = context.Scanner.Cursor.Position;
                bool trim;

                if (p.InsideLiquidTag)
                {
                    trim = context.Scanner.ReadChar('-');

                    if (context.Scanner.ReadChar('}') && context.Scanner.ReadChar('}'))
                    {
                        p.StripNextTextSpanStatement = trim;
                        p.PreviousTextSpanStatement = null;
                        p.PreviousIsTag = true;
                        p.PreviousIsOutput = false;

                        //context.Scanner.Cursor.ResetPosition(start);

                        result.Set(start.Offset, start.Offset, TagResult.TagClose);
                        p.InsideLiquidTag = false;
                        return true;
                    }
                    else
                    {
                        if (newLineIsPresent || semiColonIsPresent)
                        {
                            result.Set(start.Offset, context.Scanner.Cursor.Offset, TagResult.TagClose);
                            return true;
                        }
                    }

                    context.Scanner.Cursor.ResetPosition(start);
                    return false;
                }

                trim = context.Scanner.ReadChar('-');

                if (context.Scanner.ReadChar('}') && context.Scanner.ReadChar('}'))
                {
                    p.StripNextTextSpanStatement = trim;
                    p.PreviousTextSpanStatement = null;
                    p.PreviousIsTag = true;
                    p.PreviousIsOutput = false;

                    p.InsideLiquidTag = false;

                    result.Set(start.Offset, context.Scanner.Cursor.Offset, trim ? TagResult.TagCloseTrim : TagResult.TagClose);
                    return true;
                }
                else
                {
                    context.Scanner.Cursor.ResetPosition(start);
                    return false;
                }
            }
        }

        private sealed class EscapeBlockStartParser : Parser<int>
        {
            public override bool Parse(ParseContext context, ref ParseResult<int> result)
            {
                var start = context.Scanner.Cursor.Position;

                // Use the scanner's logic to ignore whitespaces since it knows about multi-line grammars
                context.Scanner.SkipWhiteSpace();

                if (context.Scanner.ReadChar('{') && context.Scanner.ReadChar('%'))
                {
                    while (context.Scanner.ReadChar('%'))
                    {

                    }
                    if (context.Scanner.ReadChar('{'))
                    {
                        result.Value = context.Scanner.Cursor.Position - start - 2;
                        return true;
                    }
                }

                context.Scanner.Cursor.ResetPosition(start);

                return false;
                }
        }

        private sealed class EscapeBlockEndParser : Parser<int>
        {
            private readonly int _expectedLength;

            public EscapeBlockEndParser(int expectedLength)
            {
                _expectedLength = expectedLength;
            }

            public override bool Parse(ParseContext context, ref ParseResult<int> result)
            {
                var start = context.Scanner.Cursor.Position;

                if (context.Scanner.ReadChar('}') && context.Scanner.ReadChar('%'))
                {
                    for (int i = 0; i < _expectedLength - 1; i++)
                    {
                        if (!context.Scanner.ReadChar('%'))
                        {
                            context.Scanner.Cursor.ResetPosition(start);
                            return false;
                        }
                    }

                    if (context.Scanner.ReadChar('}'))
                    {
                        result.Value = context.Scanner.Cursor.Position - start - 2;
                        return true;
                    }
                }

                context.Scanner.Cursor.ResetPosition(start);

                return false;
            }
        }

        private sealed class OutputTagStartParser : Parser<TagResult>
        {
            private readonly bool _skipWhiteSpace;

            public OutputTagStartParser(bool skipWhiteSpace = false)
            {
                _skipWhiteSpace = skipWhiteSpace;
            }

            public override bool Parse(ParseContext context, ref ParseResult<TagResult> result)
            {
                if (_skipWhiteSpace)
                {
                    context.SkipWhiteSpace();
                }

                var start = context.Scanner.Cursor.Position;

                if (context.Scanner.ReadChar('{') && context.Scanner.ReadChar('{'))
                {
                    var trim = context.Scanner.ReadChar('-');

                    var p = (FluidParseContext)context;

                    if (p.PreviousTextSpanStatement != null)
                    {
                        if (trim)
                        {
                            p.PreviousTextSpanStatement.StripRight = true;
                        }

                        p.PreviousTextSpanStatement.NextIsOutput = true;

                        p.PreviousTextSpanStatement = null;
                    }


                    result.Set(start.Offset, context.Scanner.Cursor.Offset, trim ? TagResult.TagOpenTrim : TagResult.TagOpen);
                    return true;
                }
                else
                {
                    context.Scanner.Cursor.ResetPosition(start);
                    return false;
                }
            }
        }

        private sealed class OutputTagEndParser : Parser<TagResult>
        {
            private readonly bool _skipWhiteSpace;

            public OutputTagEndParser(bool skipWhiteSpace = false)
            {
                _skipWhiteSpace = skipWhiteSpace;
            }

            public override bool Parse(ParseContext context, ref ParseResult<TagResult> result)
            {
                if (_skipWhiteSpace)
                {
                    context.SkipWhiteSpace();
                }

                var start = context.Scanner.Cursor.Position;

                bool trim = context.Scanner.ReadChar('-');

                if (context.Scanner.ReadChar('}') && context.Scanner.ReadChar('}'))
                {
                    var p = (FluidParseContext)context;

                    p.StripNextTextSpanStatement = trim;
                    p.PreviousTextSpanStatement = null;
                    p.PreviousIsTag = false;
                    p.PreviousIsOutput = true;

                    result.Set(start.Offset, context.Scanner.Cursor.Offset, trim ? TagResult.TagCloseTrim : TagResult.TagClose);
                    return true;
                }
                else
                {
                    context.Scanner.Cursor.ResetPosition(start);
                    return false;
                }
            }
        }
    }
}
