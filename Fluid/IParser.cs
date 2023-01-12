using Fluid.Ast;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Fluid;
public interface IParser
{
    Dictionary<string, Func<Expression, Expression, Expression>> RegisteredOperators { get; }
    Dictionary<string, Parser<Statement>> RegisteredTags { get; }

    Parser<List<Statement>> Grammar { get; }

    IParser Compile();
    void RegisterEmptyBlock(string tagName, Func<IReadOnlyList<Statement>, TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render);
    void RegisterEmptyTag(string tagName, Func<TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render);
    void RegisterExpressionBlock(string tagName, Func<Expression, IReadOnlyList<Statement>, TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render);
    void RegisterExpressionTag(string tagName, Func<Expression, TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render);
    void RegisterIdentifierBlock(string tagName, Func<string, IReadOnlyList<Statement>, TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render);
    void RegisterIdentifierTag(string tagName, Func<string, TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render);
    void RegisterParserBlock<T>(string tagName, Parser<T> parser, Func<T, IReadOnlyList<Statement>, TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render);
    void RegisterParserTag<T>(string tagName, Parser<T> parser, Func<T, TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render);

    bool TryParse(string template, out IFluidTemplate result, out string error);
}