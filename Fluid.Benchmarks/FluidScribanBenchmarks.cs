using BenchmarkDotNet.Attributes;

namespace Fluid.Benchmarks
{
    [MemoryDiagnoser]
    public class FluidScribanBenchmarks : BaseBenchmarks
    {
        private readonly TemplateOptions _options = new TemplateOptions();
        private readonly ScribanParser _parser  = new ScribanParser();
        private readonly IFluidTemplate _fluidTemplate;

        public FluidScribanBenchmarks()
        {
            _options.MemberAccessStrategy.Register<Product>();
            _options.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.CamelCase;
            _parser.TryParse(ScribanProductTemplate, out _fluidTemplate, out var _);
        }

        //[Benchmark]
        public override object Parse()
        {
            return _parser.Parse(ScribanProductTemplate);
        }

        [Benchmark]
        public override object ParseBig()
        {
            return _parser.Parse(ScribanBlogPostTemplate);
        }

        //[Benchmark]
        public override string Render()
        {
            var context = new TemplateContext(_options).SetValue("products", Products);
            return _fluidTemplate.Render(context);
        }

        //[Benchmark]
        public override string ParseAndRender()
        {
            _parser.TryParse(ScribanProductTemplate, out var template);
            var context = new TemplateContext(_options).SetValue("products", Products);
            return template.Render(context);
        }
    }
}
