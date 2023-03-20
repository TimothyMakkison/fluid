using BenchmarkDotNet.Attributes;
using Fluid.Ast;
using Parlot;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Fluid.Benchmarks
{
    [MemoryDiagnoser]
    public class FluidBenchmarks : BaseBenchmarks
    {
        private readonly TemplateOptions _options = new TemplateOptions();
        private readonly FluidParser _parser  = new FluidParser();
        private readonly IFluidTemplate _fluidTemplate;

        public FluidBenchmarks()
        {
            _options.MemberAccessStrategy.Register<Product>();
            _options.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.CamelCase;
            _parser.TryParse(ProductTemplate, out _fluidTemplate, out var _);
        }

        [Benchmark]
        public override object Parse()
        {
            return _parser.Parse(ProductTemplate);
        }

        [Benchmark]
        public override object ParseBig()
        {
            return _parser.Parse(BlogPostTemplate);
        }

        [Benchmark]
        public override string Render()
        {
            var context = new TemplateContext(_options).SetValue("products", Products);
            return _fluidTemplate.Render(context);
        }

        [Benchmark]
        public override string ParseAndRender()
        {
            _parser.TryParse(ProductTemplate, out var template);
            var context = new TemplateContext(_options).SetValue("products", Products);
            return template.Render(context);
        }
    }

    class Statement
    {

        public Statement(TextSpan ts)
        {
            this.ts=ts;
        }
        private TextSpan ts;
        private string buff;

        public void Write(StringBuilder sb)
        {
            ts = new TextSpan(ts.Buffer, 2, 40);
            buff = ts.ToString();
            sb.Append(buff);
        }
    }

    class StatementSpan
    {

        public StatementSpan(TextSpan ts)
        {
            this.ts=ts;
        }
        private TextSpan ts;
        private string buff;

        public void Write(StringBuilder sb)
        {
            ts = new TextSpan(ts.Buffer, 2, 40);
            //buff = ts.ToString();
            sb.Append(ts.Span);
        }
    }


    [MemoryDiagnoser]
    //[IterationCount(2)]
    public class WriteBenchmark
    {
        private readonly OriginTextSpanStatement _preOrig;
        private readonly TextSpanStatement _preText;

        private readonly OriginTextSpanStatement _nonOrig;
        private readonly TextSpanStatement _nonText;

        private readonly TemplateContext _context;
        private readonly string _text = "     Hello    ";
        public WriteBenchmark()
        {
            _preOrig = new OriginTextSpanStatement(_lorem) { StripLeft = true, StripRight = true };
            _preText = new TextSpanStatement(_lorem) { StripLeft = true, StripRight = true };

            _nonOrig = new OriginTextSpanStatement(_lorem) { StripLeft = true, StripRight = true };
            _nonText = new TextSpanStatement(_lorem) { StripLeft = true, StripRight = true };
            _nonOrig._buffer = _lorem;
            _context = new TemplateContext();

            span = new TextSpan(_lorem);
        }

        [GlobalSetup]
        public async Task Setup()
        {
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);
            await _preOrig.WriteToAsync(writer, NullEncoder.Default, _context);
            await _preText.WriteToAsync(writer, NullEncoder.Default, _context);
        }

        //[Benchmark]
        //public TextSpanStatement AssignOrig()
        //{
        //    return new TextSpanStatement(_text) { StripLeft = true, StripRight = true };
        //}

        //[Benchmark]
        //public TextSpanStatement AssignText()
        //{
        //    return new TextSpanStatement(_text) { StripLeft = true, StripRight = true };
        //}

        private string _lorem = "     Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse felis lorem, iaculis sed ligula vitae, tincidunt suscipit ante. Vivamus nec urna vitae mauris blandit bibendum et ac enim. In lacinia nunc ut magna iaculis, at feugiat enim congue. Aliquam cursus gravida faucibus. Integer ultrices libero sit amet nisl sollicitudin sagittis. Mauris at tellus feugiat, rutrum enim a, iaculis erat. Phasellus pretium commodo dignissim. Maecenas at maximus sem. Quisque a gravida urna, vitae aliquam metus. Nunc et aliquam metus, at pretium ipsum. Aliquam sollicitudin ligula enim, vitae mattis lorem dignissim id.\r\n\r\nMauris vitae massa id nisl semper sagittis. Mauris pulvinar porttitor lorem, eleifend imperdiet leo vehicula sed. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec euismod elit sed cursus viverra. Etiam quis sapien aliquam, posuere purus eu, convallis massa. Nunc non orci at erat tincidunt aliquet sed eu nibh.\r\n\r\nNunc nec nulla in velit faucibus convallis. Nunc erat orci, volutpat ut neque a, vestibulum semper arcu. Mauris ante metus, rhoncus at tempor sed, placerat id nisi. Mauris molestie velit quis ligula venenatis maximus. Aliquam ullamcorper pretium tincidunt. In sed velit suscipit diam mattis commodo. Aliquam consectetur augue magna, quis rhoncus tellus sodales id. Vestibulum dapibus eros vel mi rhoncus, vitae malesuada orci posuere. Sed rutrum elit sapien, id tempor elit tempor sit amet. Cras ullamcorper viverra turpis. In nec rutrum nunc. Curabitur id convallis arcu.\r\n\r\nVestibulum eu dictum dolor, at varius odio. Sed purus augue, scelerisque et nulla at, condimentum tincidunt neque. Aenean tristique odio velit, ut venenatis mauris venenatis ut. Praesent eu sapien nec felis euismod malesuada. Suspendisse sagittis dui quis metus posuere, blandit varius dui blandit. Integer faucibus ipsum sit amet venenatis efficitur. Sed vitae neque ullamcorper, porta erat a, gravida arcu. Phasellus finibus ultrices felis, nec consequat ex tincidunt at. Quisque tincidunt malesuada lacus, quis eu     ";
        private TextSpan span ;


        //[Benchmark]
        //public StringBuilder Span()
        //{
        //    var sb = new StringBuilder();

        //    var state = new StatementSpan(span);
        //    state.Write(sb);
        //    return sb;
        //}

        //[Benchmark]
        //public StringBuilder Sub()
        //{
        //    var sb = new StringBuilder();

        //    var state = new Statement(span);
        //    state.Write(sb);
        //    return sb;
        //}


        //[Benchmark]
        //public StringBuilder Span()
        //{
        //    var sb = new StringBuilder();

        //    sb.Append(_lorem.AsSpan()[1..]);
        //    return sb;
        //}

        //[Benchmark]
        //public StringBuilder Sub()
        //{
        //    var sb = new StringBuilder();

        //    sb.Append(_lorem.Substring(1));
        //    return sb;
        //}


        //[Benchmark]
        //public async ValueTask<Completion> PreText()
        //{
        //    var sb = new StringBuilder();
        //    var writer = new StringWriter(sb);

        //    return await _preText.WriteToAsync(writer, NullEncoder.Default, _context);
        //}

        //[Benchmark]
        //public async ValueTask<Completion> PreOrig()
        //{
        //    var sb = new StringBuilder();
        //    var writer = new StringWriter(sb);

        //    return await _preOrig.WriteToAsync(writer, NullEncoder.Default, _context);
        //}

        [Benchmark]
        public async ValueTask<Completion> NonText()
        {
            var _nonText = new TextSpanStatement(_lorem) { StripLeft = true, StripRight = true };
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);

            return await _nonText.WriteToAsync(writer, NullEncoder.Default, _context);
        }

        [Benchmark]
        public async ValueTask<Completion> NonOrig()
        {
            var _nonOrig = new OriginTextSpanStatement(_lorem) { StripLeft = true, StripRight = true };
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);

            return await _nonOrig.WriteToAsync(writer, NullEncoder.Default, _context);
        }
    }
}
