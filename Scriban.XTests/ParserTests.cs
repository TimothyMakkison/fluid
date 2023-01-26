using Fluid;
using Fluid.Ast;
using Fluid.Parser;
using System.Collections.Generic;

namespace Scriban.XTests;

public class ParserTests
{
#if COMPILED
        private static IParser _parser = new ScribanParser().Compile();
#else
    private static IParser _parser = new ScribanParser();
#endif

    private static IReadOnlyList<Statement> Parse(string source)
    {
        _parser.TryParse(source, out var template, out var errors);
        return ((FluidTemplate)template).Statements;
    }

    private async Task CheckAsync(string source, string expected, Action<TemplateContext> init = null)
    {
        _parser.TryParse("{{ if " + source + " }}true{{ else }}false{{ end }}", out var template, out var messages);

        var context = new TemplateContext();
        init?.Invoke(context);

        var result = await template.RenderAsync(context);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldFiltersWithNamedArguments()
    {

        var statements = Parse("{{ a | b: c:1, 'value', d: 3 }}");
        Assert.Single(statements);

        var outputStatement = statements[0] as OutputStatement;
        Assert.NotNull(outputStatement);

        var filterExpression = outputStatement.Expression as FilterExpression;
        Assert.NotNull(filterExpression);
        Assert.Equal("b", filterExpression.Name);

        var input = filterExpression.Input as MemberExpression;
        Assert.NotNull(input);

        Assert.Equal("c", filterExpression.Parameters[0].Name);
        Assert.Null(filterExpression.Parameters[1].Name);
        Assert.Equal("d", filterExpression.Parameters[2].Name);
    }


    [Fact]
    public void ShouldParseText()
    {
        var statements = Parse("Hello World");

        var textStatement = statements[0] as TextSpanStatement;

        Assert.Single(statements);
        Assert.NotNull(textStatement);
        Assert.Equal("Hello World", textStatement.Text.ToString());
    }

    [Fact]
    public void ShouldParseOutput()
    {
        var statements = Parse("{{ 1 }}");

        var outputStatement = statements[0] as OutputStatement;

        Assert.Single(statements);
        Assert.NotNull(outputStatement);
    }

    [Theory]
    [InlineData("{{ a }}")]
    [InlineData("{{ a.b }}")]
    [InlineData("{{ a.b[1] }}")]
    public void ShouldParseOutputWithMember(string source)
    {
        var statements = Parse(source);

        var outputStatement = statements[0] as OutputStatement;

        Assert.Single(statements);
        Assert.NotNull(outputStatement);
    }

    [Fact]
    public void ShouldParseForTag()
    {
        var statements = Parse("{{ for a in b }}{{ end }}");

        Assert.IsType<ForStatement>(statements.ElementAt(0));
    }

    [Fact]
    public void ShouldParseForElseTag()
    {
        var statements = Parse("{{ for a in b }}x{{ else }}y{{ end }}");

        Assert.IsType<ForStatement>(statements.ElementAt(0));
        var forStatement = statements.ElementAt(0) as ForStatement;
        Assert.True(forStatement.Statements.Count == 1);
        Assert.NotNull(forStatement.Else);
        Assert.True((forStatement.Else is ElseStatement s) && s.Statements.Count == 1);
    }

    [Fact]
    public void ShouldParseForLimitLiteral()
    {
        var statements = Parse("{{ for item in items limit: 1 }}x{{ end }}");

        Assert.IsType<ForStatement>(statements.ElementAt(0));
        var forStatement = statements.ElementAt(0) as ForStatement;
        Assert.True(forStatement.Statements.Count == 1);
        Assert.True(forStatement.Limit is LiteralExpression);
    }

    [Fact]
    public void ShouldParseForLimitMember()
    {
        var statements = Parse("{{ for item in items limit: limit }}x{{ end }}");

        Assert.IsType<ForStatement>(statements.ElementAt(0));
        var forStatement = statements.ElementAt(0) as ForStatement;
        Assert.True(forStatement.Statements.Count == 1);
        Assert.True(forStatement.Limit is MemberExpression);
    }

    [Fact]
    public void ShouldReadSingleCharInTag()
    {
        var statements = Parse(@"{{ for a in b }};{{ end }}");
        Assert.Single(statements);
        var text = ((ForStatement)statements[0]).Statements[0] as TextSpanStatement;
        Assert.Equal(";", text.Text.ToString());
    }

    [Fact]
    public void ShouldParseComment()
    {
        var statements = Parse(@"{{ comment }} on {{ this }} and {{{ that }}} {{ endcomment }}");

        Assert.Single(statements);
        Assert.IsType<CommentStatement>(statements.ElementAt(0));
        Assert.Equal(" on {{ this }} and {{{ that }}} ", (statements.ElementAt(0) as CommentStatement).Text.ToString());
    }

    [Fact]
    public void ShouldParseCommentWithBlocks()
    {
        var statements = Parse(@"{{ comment }} {{if true}} {{end}} {{ endcomment }}");

        Assert.Single(statements);
        Assert.IsType<CommentStatement>(statements.ElementAt(0));
        Assert.Equal(" {{if true}} {{end}} ", (statements.ElementAt(0) as CommentStatement).Text.ToString());
    }

    [Fact]
    public void ShouldParseIfTag()
    {
        var statements = Parse("{{ if true }}yes{{ end }}");

        Assert.IsType<IfStatement>(statements.ElementAt(0));
        Assert.True(statements.ElementAt(0) is IfStatement s && s.Statements.Count == 1);
    }

    [Fact]
    public void ShouldParseIfElseTag()
    {
        var statements = Parse("{{ if true }}yes{{else}}no{{ end }}");

        var ifStatement = statements.ElementAt(0) as IfStatement;
        Assert.NotNull(ifStatement);
        Assert.Single(ifStatement.Statements);
        Assert.NotNull(ifStatement.Else);
        Assert.Empty(ifStatement.ElseIfs);
    }

    [Fact]
    public void ShouldParseIfElseTagWithAssignment()
    {
        var source = @"{{ if true }}{{ a = ""yes"" }}{{else}}{{ a= ""no"" }}{{ end }}{{ a }}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var render = template.Render();
        Assert.Equal("yes", render);
    }

    [Fact]
    public void ShouldParseAssignEmit()
    {
        var source = @"{{ a = ""yes"" }}{{ a }}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var render = template.Render();
        Assert.Equal("yes", render);
    }

    [Theory]
    [InlineData(@"{{ a= [10]; a[0] }}", "10")]
    [InlineData(@"{{ a= [10, 2,3]; a[0];a[1];a[2] }}", "1023")]
    [InlineData(@"{{ a= [10, 
2,
3]; a[0];a[1];a[2] }}", "1023")]
    [InlineData(@"{{ a= [""Hello"", "" World""]; a[0];a[1]}}", "Hello World")]

    // TODO Add FilterValue
    //[InlineData(@"{{ a= [""Hey"", 'welcome ' | upcase]; a[1]}}", "WELCOME")]
    public void ShouldAssignArray(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var render = template.Render();
        Assert.Equal(expected, render);
    }

    [Fact]
    public void ShouldParseIfElseIfTag()
    {
        var statements = Parse("{{ if true }}yes{{elsif a}}maybe{{else}}no{{end}}");

        var ifStatement = statements.ElementAt(0) as IfStatement;
        Assert.NotNull(ifStatement);
        Assert.Single(ifStatement.Statements);
        Assert.NotNull(ifStatement.Else);
        Assert.NotNull(ifStatement.ElseIfs);
    }


    [Theory]
    [InlineData("yes", 0)]
    [InlineData("maybe", 2)]
    [InlineData("perhaps", 4)]
    [InlineData("no", 3)]
    public void ShouldRenderElsif(string expected, int value)
    {
        var source = @"{{a = value}}{{ if a==0 }}yes{{elsif a==2}}maybe{{elsif a > 3 }}perhaps{{else}}no{{end}}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var context = new TemplateContext(new { value });
        var render = template.Render(context);
        Assert.Equal(expected, render);
    }

    [Theory]
    [InlineData("abc { def")]
    [InlineData("abc } def")]
    [InlineData("abc }} def")]
    [InlineData("abc { def }}")]
    [InlineData("abc }}")]
    [InlineData("}} def")]
    [InlineData("abc }}} def")]
    public void ShouldSucceedParseValidTemplate(string source)
    {
        var result = _parser.TryParse(source, out var template, out var errors);
        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);
    }

    [Theory]
    [InlineData("abc {{ {{ }} def")]
    [InlineData("abc {{ { }} def")]
    public void ShouldFailParseInvalidTemplate(string source)
    {
        var result = _parser.TryParse(source, out var template, out var errors);
        Assert.False(result);
    }

    [Theory]
    [InlineData("{{ _foo = 1 }}")]
    [InlineData("{{ __foo = 1 }}")]
    [InlineData("{{ fo-o = 1 }}")]
    [InlineData("{{ fo_o = 1 }}")]
    [InlineData("{{ fo--o = 1 }}")]
    [InlineData("{{ fo__o = 1 }}")]
    public void ShouldAcceptDashesInIdentifiers(string source)
    {
        var result = _parser.TryParse(source, out var template, out var error);

        Assert.True(result, error);
    }

    [Theory]
    [InlineData("{{ for = 5 }}")]
    [InlineData("{{ for }}")]
    [InlineData("{{ = 100 }}")]
    [InlineData("{{ if = 100 }}")]
    public void ShouldFailToParseKnownTagsAsVariables(string source)
    {
        var result = _parser.TryParse(source, out var template, out var error);

        Assert.False(result);
    }

    [Theory]
    [InlineData("{{ 1f = 123 }}{{ 1f }}")]
    [InlineData("{{ 123f = 123 }}{{ 123f }}")]
    [InlineData("{{ 1_ = 123 }}{{ 1_ }}")]
    [InlineData("{{ 1-1 = 123 }}{{ 1-1 }}")]
    public void ShouldAcceptDigitsAtStartOfIdentifiers(string source)
    {
        var result = _parser.TryParse(source, out var template, out var error);

        Assert.True(result, error);
        Assert.Equal("123", template.Render());
    }

    [Theory]
    [InlineData(@"abc{{ {{ }}def", "at (")]
    [InlineData(@"{{ username = ""John G. Chalmers-Smith"" }}
{{ if username and username.size > 10 }}
  Wow, {{ username }}, you have a long name!
{{ else }}
  Hello there {{ { }}!
{{ end }}", "at (")]
    [InlineData(@"{{ username = ""John G. Chalmers-Smith"" }}
{{ if username and 
      username.size > 5 &&
      username.size < 10 }}
  Wow, {{ username }}, you have a longish name!
{{ else }}
  Hello there!
{{ end }}", "at (")]
    public void ShouldFailParseInvalidTemplateWithCorrectLineNumber(string source, string expectedErrorEndString)
    {
        var result = _parser.TryParse(source, out var template, out var errors);
        Assert.False(result);
        Assert.Contains(expectedErrorEndString, errors);
    }

    [Theory]
    [InlineData("{{ for a in b }}")]
    [InlineData("{{ if true }}")]
    [InlineData("{{ case a }}")]
    [InlineData("{{ capture myVar }}")]
    public void ShouldFailNotClosedBlock(string source)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.False(result);
        Assert.NotNull(errors);
    }

    [Theory]
    [InlineData("{{ if true }} {{ end }}")]
    [InlineData("{{ for a in b }} {{ end }}")]
    [InlineData("{{ case a }} {{ when 'cake' }} blah {{ end }}")]
    [InlineData("{{ capture myVar }} capture me! {{ end }}")]
    public void ShouldSucceedClosedBlock(string source)
    {
        var result = _parser.TryParse(source, out var template, out var error);

        Assert.True(result, error);
        Assert.NotNull(template);
        Assert.Null(error);
    }

    [Fact]
    public void ShouldAllowNewLinesInCase()
    {
        var result = _parser.TryParse(@"
                {{ case food }}
                    


                    {{ when 'cake' }}
                        yum
                    {{ when 'rock' }}
                        yuck
                {{ end }}
                ", out var template, out var errors);

        var context = new TemplateContext();
        context.SetValue("food", "cake");

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);
    }

    [Theory]
    [InlineData(@"{{ 20 | 
divided_by: 7.0 |
round: 2 }}", "2.86")]
    [InlineData("{{ 20 | divided_by: 7.0 | round: 2 }}", "2.86")]
    [InlineData("{{ 20 | divided_by: 7 | round: 2 }}", "2")]
    public void ShouldParseIntegralNumbers(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData("{{if true}}{{ bab = 20 }}{{end}}{{ bab | divided_by: 5 | divided_by: 2 }}", "2")]
    [InlineData("{{ value | divided_by: 5 | divided_by: 2 }}", "2")]
    [InlineData("{{ value }}", "20")]
    [InlineData("{{ if true; bab = 20; end; bab | divided_by: 5 | divided_by: 2 }}", "2")]
    public void ShouldOutputValue(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var context = new TemplateContext(new { value = 20 });
        var rendered = template.Render(context);

        Assert.Equal(expected, rendered);
    }


    //[Fact]
    //public void ShouldIndexStringSegment()
    //{
    //    var segment = new StringSegment("012345");

    //    Assert.Equal('0', segment.Index(0));
    //    Assert.Equal('5', segment.Index(-1));

    //    segment = segment.Subsegment(1, 4);

    //    Assert.Equal('1', segment.Index(0));
    //    Assert.Equal('4', segment.Index(-1));
    //}

    [Fact]
    public void ShouldParseCurlyBraceInOutputStatements()
    {
        Parse("{{ 'on {0}' }}");
    }

    [Fact]
    public void ShouldBeAbleToCompareNilValues()
    {
        // [1, 2, 3] | map will return [nil, nil, nil] then | uniq will try to call NilValue.GetHashCode()

        var model = new
        {
            Doubles = new List<double> { 1.1, 2.2, 3.3 }
        };

        var template = "{{Doubles |map |uniq}}";

        if (_parser.TryParse(template, out var result))
        {
            result.Render(new TemplateContext(model));
        }
    }

    [Fact]
    public void ShouldRegisterModelType()
    {
        var model = new
        {
            name = "Tobi"
        };

        var source = "{{name}}";

        _parser.TryParse(source, out var template);

        var rendered = template.Render(new TemplateContext(model));

        Assert.Equal("Tobi", rendered);
    }

    [Theory]
    [InlineData("{{ for }}")]
    [InlineData("{{ case }}")]
    [InlineData("{{ if }}")]
    [InlineData("{{ comment }}")]
    [InlineData("{{ capture }}")]

    public void ShouldThrowParseExceptionMissingTag(string template)
    {
        Assert.Throws<ParseException>(() => _parser.Parse(template));
    }

    [Theory]
    [InlineData("{{ 'a\\nb' }}", "a\nb")]
    [InlineData("{{ 'a\\tb' }}", "a\tb")]
    public void ShouldParseEscapeSequences(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData("{{ 'a\nb' }}", "a\nb")]
    [InlineData("{{ 'a\r\nb' }}", "a\r\nb")]
    public void ShouldParseLineBreaksInStringLiterals(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData("{{ -3 }}", "-3")]
    public void ShouldParseNegativeNumbers(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData("{{ my_integer = 7 }}{{ 20 | divided_by: my_integer }}", "2")]
    [InlineData("{{ my_integer = 7 }}{{ my_float = my_integer | times: 1.0 }}{{ 20 | divided_by: my_float | round: 5 }}", "2.85714")]
    [InlineData("{{ 183.357 | times: 12 }}", "2200.284")]
    public void ShouldChangeVariableType(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData(@"{{ my_string = 'abcd' }}{{ my_string.size }}", "4")]
    public void SizeAppliedToStrings(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData(@"{%{ {{ hey }} }%}", " {{ hey }} ")]
    [InlineData(@"{%{ {{ true == true }} }%}", " {{ true == true }} ")]
    [InlineData(@"{%{ on {{ this }} and {{{ that }}} }%}", " on {{ this }} and {{{ that }}} ")]
    [InlineData(@"{%{ {{if true}} {{end}} }%}", " {{if true}} {{end}} ")]
    public void EscapeBlockShouldEscapeContents(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Contains(expected, rendered);
    }

    [Theory]
    [InlineData(@"{%{ Hello }%}", " Hello ")]
    [InlineData(@"{%%{ Hello }%%}", " Hello ")]
    [InlineData(@"{%%%%%{ Hello }%%%%%}", " Hello ")]
    public void VariableLengthEscapeBlocks(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Contains(expected, rendered);
    }

    [Theory]
    [InlineData(@"{%{Hello}%}", "Hello")]
    [InlineData(@"{%%{ Hello}%%}", " Hello")]
    [InlineData(@"{%%{ Hello  }%%}", " Hello  ")]
    [InlineData(@"{%{ Hello }%}", " Hello ")]
    [InlineData(@"{%%{ Hello }%%}", " Hello ")]
    public void EscapeBlocksShouldIncludeSpaces(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData(@"{%{ {%{ }%}", " {%{ ")]
    [InlineData(@"{%{ {%%{ }%}", " {%%{ ")]
    [InlineData(@"{%%{ {%%{ }%%}", " {%%{ ")]
    [InlineData(@"{%%{ }%} }%%}", " }%} ")]
    [InlineData(@"{%%{ }%%%} }%%}", " }%%%} ")]
    public void EscapeBlocksShouldEndOnMatchingBlock(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Theory]
    [InlineData("{{ '{{ {{ }} }}' }}{{ x = '{{ {{ }} }}' }}{{ x }}", "{{ {{ }} }}{{ {{ }} }}")]
    public void StringsCanContainCurlies(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }


    [Theory]
    [InlineData("{{ null }}", "")]
    public void NullEmitsEmpty(string source, string expected)
    {
        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal(expected, rendered);
    }

    [Fact]
    public void ShouldSkipNewLinesInLiquidTags()
    {
        var source = @"{{ if true or false -}}
true
{{- end }}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("true", rendered);
    }

    [Fact]
    public void ShouldSkipNewLinesInTags3()
    {
        var source = @"{{ 
if true or false
if false
echo false
else
""true""
end
end
}}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("true", rendered);
    }


    [Fact]
    public void ShouldSkipNewLinesInTags8()
    {
        var source = @"
{{if true or false}}
{{ if false }}
""false""
{{ else }}
""true""
{{ end }}
{{ end }}
";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Contains("true", rendered);
    }


    [Fact]
    public void ShouldSkipNewLinesInTags2()
    {
        var source = @"{{ 
if true or false
true
end
}}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("true", rendered);
    }

    [Fact]
    public void ShouldSkipNewLinesInTags5()
    {
        var source = @"{{ 
if true or false
""true""
end
}}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("true", rendered);
    }

    [Fact]
    public void ShouldSkipNewLinesInTags7()
    {
        var source = @"{{ 
""Hey""
}}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("Hey", rendered);
    }

    [Fact]
    public void ShouldSkipNewLinesInTags4()
    {
        var source = @"{{ if true or false; if false; echo false; else; echo true; end; end}}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("true", rendered);
    }

    [Fact]
    public void ShouldSkipNewLinesInOutput()
    {
        var source = @"{{
true
}}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("true", rendered);
    }

    [Fact]
    public void ShouldReadOutput()
    {
        var source = @"{{ true }}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("true", rendered);
    }

    [Theory]

    [InlineData("'' == p", "false")]
    [InlineData("p == ''", "false")]
    [InlineData("p != ''", "true")]

    [InlineData("p == nil", "true")]
    [InlineData("p != nil", "false")]
    [InlineData("nil == p", "true")]

    [InlineData("p == blank", "true")]
    [InlineData("blank == p ", "true")]

    [InlineData("empty == blank", "true")]
    [InlineData("blank == empty", "true")]

    [InlineData("nil == blank", "true")]
    [InlineData("blank == nil", "true")]

    [InlineData("blank == ''", "true")]
    [InlineData("'' == blank", "true")]

    [InlineData("nil == ''", "false")]
    [InlineData("'' == nil", "false")]

    [InlineData("empty == ''", "true")]
    [InlineData("'' == empty", "true")]

    [InlineData("e == ''", "true")]
    [InlineData("'' == e", "true")]

    [InlineData("e == blank", "true")]
    [InlineData("blank == e", "true")]

    [InlineData("empty == nil", "false")]
    [InlineData("nil == empty", "false")]

    [InlineData("p != nil and p != ''", "false")]
    [InlineData("p != '' and p != nil", "false")]

    [InlineData("e != nil and e != ''", "false")]
    [InlineData("e != '' and e != nil", "false")]

    [InlineData("f != nil and f != ''", "true")]
    [InlineData("f != '' and f != nil", "true")]

    [InlineData("e == nil", "false")]
    [InlineData("nil == e", "false")]

    [InlineData("e == empty ", "true")]
    [InlineData("empty == e ", "true")]

    [InlineData("empty == f", "false")]
    [InlineData("f == empty", "false")]

    [InlineData("p == empty", "false")]
    [InlineData("empty == p", "false")]

    public Task EmptyShouldEqualToNil(string source, string expected)
    {
        return CheckAsync(source, expected, t => t.SetValue("e", "").SetValue("f", "hello"));
    }

    [Theory]
    [InlineData("zero == empty", "false")]
    [InlineData("empty == zero", "false")]
    [InlineData("zero == blank", "false")]
    [InlineData("blank == zero", "false")]

    [InlineData("one == empty", "false")]
    [InlineData("empty == one", "false")]
    [InlineData("one == blank", "false")]
    [InlineData("blank == one", "false")]
    public Task EmptyShouldNotEqualNumbers(string source, string expected)
    {
        return CheckAsync(source, expected, t => t.SetValue("zero", 0).SetValue("one", 1));
    }

    [Theory]
    [InlineData("blank == false", "true")]
    [InlineData("empty == false", "false")]
    public Task BlankShouldComparesToFalse(string source, string expected)
    {
        return CheckAsync(source, expected, t => t.SetValue("zero", 0).SetValue("one", 1));
    }

    [Fact]
    public void ModelShouldNotImpactBlank()
    {
        var source = "{{ a = ' ' }}{{ a == blank }}";
        var model = new { a = " ", b = "" };
        var context = new TemplateContext(model);
        var template = _parser.Parse(source);
        Assert.Equal("true", template.Render(context));
    }

   // [Fact]
   // public void TestLines()
   // {
   //     var source = @"{{ a_multi_line_value = ""test1\ntest2\ntest3\n"" }}
   //{{ a_multi_line_value }}Hello";
   //     var model = new { a = "world", b = "cold", c = true, d = "dr" };
   //     var context = new TemplateContext(model);
   //     var template = _parser.Parse(source);
   //     Assert.Equal("true", template.Render(context));
   // }

    [Fact]
    public void CycleShouldHandleNumbers()
    {
        var source = @"{{ for i in (1..100) limit:9}}{{ cycle 1, 2 ,3 }}<br />{{ end }}";

        var result = _parser.TryParse(source, out var template, out var errors);

        Assert.True(result, errors);
        Assert.NotNull(template);
        Assert.Null(errors);

        var rendered = template.Render();

        Assert.Equal("1<br />2<br />3<br />1<br />2<br />3<br />1<br />2<br />3<br />", rendered);
    }

    [Fact]
    public void ShouldAssignWithLogicalExpression()
    {
        var source = @"{{- condition_temp = HasInheritance == false or ConvertConstructorInterfaceData | append: 'o' }}{{ condition_temp }}";

        Assert.True(_parser.TryParse(source, out var template, out var _));
        Assert.True(((FluidTemplate)template).Statements.Count == 2);
        var rendered = template.Render();

        Assert.Equal("falseo", rendered);
    }

    [Fact]
    public void ShouldParseRecursiveIfs()
    {
        var source = @"
{{- if true }}
    a1
    {{- if true }}
        b1
        {{- if true }}
            c1
        {{- end }}
        {{- if true }}
            c2
        {{- end }}
    {{- end }}
    {{- if true }}
        b2
    {{- end }}
    a2
{{- end }}
";

        Assert.True(_parser.TryParse(source, out var template, out var _));
        var rendered = template.Render();
        Assert.Contains("a1", rendered);
        Assert.Contains("b1", rendered);
        Assert.Contains("c1", rendered);
        Assert.Contains("c2", rendered);
        Assert.Contains("b2", rendered);
        Assert.Contains("a2", rendered);
    }

    [Fact]
    public void ShouldParseNJsonSchema()
    {
        var source = @"
{{- if HasDescription }}
/** {{ Description }} */
{{- end }}
{{ if ExportTypes }}export {{ end }}{{ if IsAbstract }}abstract {{ end }}class {{ ClassName }}{{ Inheritance }} {
{{- for property in Properties }}
{{-   if property.HasDescription }}
    /** {{ property.Description }} */
{{-   end }}
    {{ if property.IsReadOnly }}readonly {{ end }}{{ property.PropertyName }}{{ if property.IsOptional }}?{{ elsif RequiresStrictPropertyInitialization and property.HasDefaultValue == false }}!{{ end }}: {{ property.Type }}{{ property.TypePostfix }};
{{- end }}
{{- if HasIndexerProperty }}
    [key: string]: {{ IndexerPropertyValueType }}; 
{{- end }}
{{- if HasDiscriminator }}
    protected _discriminator: string;
{{- end }}
{{- condition_temp = HasInheritance == false or ConvertConstructorInterfaceData }}
{{- if GenerateConstructorInterface or HasBaseDiscriminator }}
    constructor({{ if GenerateConstructorInterface }}data?: I{{ ClassName }}{{ end }}) {
{{-     if HasInheritance }}
        super({{ if GenerateConstructorInterface }}data{{ end }});
{{-     end }}
{{-     if GenerateConstructorInterface and condition_temp }}
        if (data) {
{{-         if HasInheritance == false }}
            for (var property in data) {
                if (data.hasOwnProperty(property))
                    (<any>this)[property] = (<any>data)[property];
            }
{{-         end }}
{{-         if ConvertConstructorInterfaceData }}
{{-             for property in Properties }}
{{-                 if property.SupportsConstructorConversion }}
{{-                     if property.IsArray }}
            if (data.{{ property.PropertyName }}) {
                this.{{ property.PropertyName }} = [];
                for (let i = 0; i < data.{{ property.PropertyName }}.length; i++) {
                    let item = data.{{ property.PropertyName }}[i];
                    this.{{ property.PropertyName }}[i] = item && !(<any>item).toJSON ? new {{ property.ArrayItemType }}(item) : <{{ property.ArrayItemType }}>item;
                }
            }
{{-                     elsif property.IsDictionary }}
            if (data.{{ property.PropertyName }}) {
                this.{{ property.PropertyName }} = {};
                for (let key in data.{{ property.PropertyName }}) {
                    if (data.{{ property.PropertyName }}.hasOwnProperty(key)) {
                        let item = data.{{ property.PropertyName }}[key];
                        this.{{ property.PropertyName }}[key] = item && !(<any>item).toJSON ? new {{ property.DictionaryItemType }}(item) : <{{ property.DictionaryItemType }}>item;
                    }
                }
            }
{{-                     else }}
            this.{{ property.PropertyName }} = data.{{ property.PropertyName }} && !(<any>data.{{ property.PropertyName }}).toJSON ? new {{ property.Type }}(data.{{ property.PropertyName }}) : <{{ property.Type }}>this.{{ property.PropertyName }}; 
{{-                     end }}
{{-                 end }}
{{-             end }}
{{-         end }}
        }
{{-     end }}
{{-     if HasDefaultValues }}
        {{ if GenerateConstructorInterface }}if (!data) {{ end }}{
{{-         for property in Properties }}
{{-             if property.HasDefaultValue }}
            this.{{ property.PropertyName }} = {{ property.DefaultValue }};
{{-             end }}
{{-         end }}
        }
{{-     end }}
{{-     if HasBaseDiscriminator }}
        this._discriminator = ""{{ DiscriminatorName }}"";
{{-     end }}
    }
{{- end }}
    init(_data?: any{{ if HandleReferences }}, _mappings?: any{{ end }}) {
{{- if HasInheritance }}
        super.init(_data);
{{- end }}
{{- if HasIndexerProperty or HasProperties }}
        if (_data) {
{{-     if HasIndexerProperty }}
            for (var property in _data) {
                if (_data.hasOwnProperty(property))
                    this[property] = _data[property];
            }
{{-     end }}
{{-     for property in Properties }}
            {{ property.ConvertToClassCode | tab }}
{{-     end }}
        }
{{- end }}
    }
    static fromJS(data: any{{ if HandleReferences }}, _mappings?: any{{ end }}): {{ ClassName }}{{ if HandleReferences }} | null{{ end }} {
        data = typeof data === 'object' ? data : {};
{{- if HandleReferences }}
{{-   if HasBaseDiscriminator }}
{{-     for derivedClass in DerivedClasses }}
        if (data[""{{ BaseDiscriminator }}""] === ""{{ derivedClass.Discriminator }}"")
{{-       if derivedClass.IsAbstract }}
            throw new Error(""The abstract class '{{ derivedClass.ClassName }}' cannot be instantiated."");
{{-       else }}
            return createInstance<{{ derivedClass.ClassName }}>(data, _mappings, {{ derivedClass.ClassName }});
{{-       end }}
{{-     end }}
{{-   end }}
{{-   if IsAbstract }}
        throw new Error(""The abstract class '{{ ClassName }}' cannot be instantiated."");
{{-   else }}
        return createInstance<{{ ClassName }}>(data, _mappings, {{ ClassName }});
{{-   end }}
{{- else }}
{{-   if HasBaseDiscriminator }}
{{-     for derivedClass in DerivedClasses }}
        if (data[""{{ BaseDiscriminator }}""] === ""{{ derivedClass.Discriminator }}"") {
{{-       if derivedClass.IsAbstract }}
            throw new Error(""The abstract class '{{ derivedClass.ClassName }}' cannot be instantiated."");
{{-       else }}
            let result = new {{ derivedClass.ClassName }}();
            result.init(data);
            return result;
{{-       end }}
        }
{{-     end }}
{{-   end }}
{{-     if IsAbstract }}
        throw new Error(""The abstract class '{{ ClassName }}' cannot be instantiated."");
{{-     else }}
        let result = new {{ ClassName }}();
        result.init(data);
        return result;
{{-     end }}
{{- end }}
    }
    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
{{- if HasIndexerProperty }}
        for (var property in this) {
            if (this.hasOwnProperty(property))
                data[property] = this[property];
        }
{{- end }}
{{- if HasDiscriminator }}
        data[""{{ BaseDiscriminator }}""] = this._discriminator; 
{{- end }}
{{- for property in Properties }}
        {{ property.ConvertToJavaScriptCode | tab }}
{{- end }}
{{- if HasInheritance }}
        super.toJSON(data);
{{- end }}
        return data; 
    }
{{- if GenerateCloneMethod }}
    clone(): {{ ClassName }} {
{{-   if IsAbstract }}
        throw new Error(""The abstract class '{{ ClassName }}' cannot be instantiated."");
{{-   else }}
        const json = this.toJSON();
        let result = new {{ ClassName }}();
        result.init(json);
        return result;
{{-   end }}
    }
{{- end }}
}
{{- if GenerateConstructorInterface }}
{{-   if HasDescription }}
/** {{ Description }} */
{{-   end }}
{{ if ExportTypes }}export {{ end }}interface I{{ ClassName }}{{ InterfaceInheritance }} {
{{-   for property in Properties }}
{{-       if property.HasDescription }}
    /** {{ property.Description }} */
{{-       end }}
    {{ property.PropertyName }}{{ if property.IsOptional }}?{{ end }}: {{ property.ConstructorInterfaceType }}{{ property.TypePostfix }};
{{-   end }}
{{-   if HasIndexerProperty }}
    [key: string]: {{ IndexerPropertyValueType }}; 
{{-   end }}
}
{{- end }}
";

        Assert.True(_parser.TryParse(source, out var template, out var _));
        var rendered = template.Render();
        Assert.Equal(@"
class  {
    init(_data?: any) {
    }
    static fromJS(data: any):  {
        data = typeof data === 'object' ? data : {};
        let result = new ();
        result.init(data);
        return result;
    }
    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        return data; 
    }
}
", rendered);
    }


    [Theory]
    [InlineData("{{1}}", "1")]
    [InlineData("{{-1-}}", "1")]
    [InlineData("{{-len='1,2,3'|split:','|size-}}{{len}}", "3")] // size-}} is ambiguous and can be read as "size -}}" or "size- }}"
    public async Task ShouldSupportCompactNotation(string source, string expected)
    {
        Assert.True(_parser.TryParse(source, out var template, out var _));
        var context = new TemplateContext();
        var result = await template.RenderAsync(context);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldParseEchoTag()
    {
        var source = @"{{ 'welcome to the liquid tag' | upcase }}";

        Assert.True(_parser.TryParse(source, out var template, out var errors), errors);
        var rendered = template.Render();
        Assert.Contains("WELCOME TO THE LIQUID TAG", rendered);
    }

    [Fact]
    public void ShouldPrintInlineValue()
    {
        var source = @"{{ a = ""10"";if true; b = 123; end; a;a;b}}";

        Assert.True(_parser.TryParse(source, out var template, out var errors), errors);
        var rendered = template.Render();
        Assert.Contains("1010123", rendered);
    }

    [Fact]
    public void ShouldPrintLiquidNewBlockValues()
    {
        var source = @"{{ a = ""10"";if true}} {{b = 123; end; a;a;b}}";

        Assert.True(_parser.TryParse(source, out var template, out var errors), errors);
        var rendered = template.Render();
        Assert.Contains("1010123", rendered);
    }

    [Fact]
    public void ShouldPrintNewLineValue()
    {
        var source = @"{{ a = ""10""
if true
b = 123
end
a;

a;b-}}
hello everyone

Hey";

        Assert.True(_parser.TryParse(source, out var template, out var errors), errors);
        var rendered = template.Render();
        Assert.Contains("1010123", rendered);
    }

    [Fact]
    public void ShouldParseLiquidTag()
    {
        var source = @"
{{ 
   'welcome ' | upcase 
   'to the liquid tag' | 
upcase }}";

        Assert.True(_parser.TryParse(source, out var template, out var errors), errors);
        var rendered = template.Render();
        Assert.Contains("WELCOME TO THE LIQUID TAG", rendered);
    }

    [Fact]
    public void ShouldParseLiquidTagWithBlocks()
    {
        var source = @"
{{ cool = true
   if cool
     'welcome to the liquid tag' | upcase
   end 
}}
";

        Assert.True(_parser.TryParse(source, out var template, out var errors), errors);
        var rendered = template.Render();
        Assert.Contains("WELCOME TO THE LIQUID TAG", rendered);
    }

    [Fact]
    public void ShouldParseFunctionCall()
    {

        var options = new FluidParserOptions { AllowFunctions = true };

#if COMPILED
        var _parser = new ScribanParser(options).Compile();
#else
        var _parser = new ScribanParser(options);
#endif

        _parser.TryParse("{{ a() }}", out var template, out var errors);
        var statements = ((FluidTemplate)template).Statements;

        Assert.Single(statements);

        var outputStatement = statements[0] as OutputStatement;
        Assert.NotNull(outputStatement);

        var memberExpression = outputStatement.Expression as MemberExpression;
        Assert.Equal(2, memberExpression.Segments.Count);
        Assert.IsType<IdentifierSegment>(memberExpression.Segments[0]);
        Assert.IsType<FunctionCallSegment>(memberExpression.Segments[1]);
    }

    [Fact]
    public void ShouldNotParseFunctionCall()
    {

        var options = new FluidParserOptions { AllowFunctions = false };

#if COMPILED
        var parser = new ScribanParser(options).Compile();
#else
        var parser = new ScribanParser(options);
#endif

        Assert.False(parser.TryParse("{{ a() }}", out var template, out var errors));
        Assert.Contains(ErrorMessages.FunctionsNotAllowed, errors);
    }

    [Fact]
    public void KeywordsShouldNotConflictWithIdentifiers()
    {
        // Ensure the parser doesn't read 'empty' when identifiers start with this keywork
        // Same for blank, true, false

        var source = "{{ emptyThing = 'this is not empty' }}{{ emptyThing }}{{ empty.size }}";
        var context = new TemplateContext(new { empty = "eric" });
        var template = _parser.Parse(source);
        Assert.Equal("this is not empty4", template.Render(context));
    }

    [Fact]
    public void ShouldContinueForLoop()
    {
        var source = @"
                {{- array = (1..6) }}
                {{- for item in array limit: 3 }}
                {{- item}}
                {{- end }}
                {{- for item in array offset: continue limit: 2 }}
                {{- item}}
                {{- end }}";

        var result = _parser.TryParse(source, out var template, out var errors);
        Assert.True(result, errors);
        Assert.Equal("12345", template.Render());
    }

    [Fact]
    public void ShouldPrintForRangeLoop()
    {
        var source = @"
                {{- for item in (1..5) }}
                {{- item}}
                {{- end }}";

        var result = _parser.TryParse(source, out var template, out var errors);
        Assert.True(result, errors);
        Assert.Equal("12345", template.Render());
    }
}