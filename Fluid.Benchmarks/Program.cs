using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace Fluid.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var bench = new FluidBenchmarks();
            //var result = bench.ParseAndRender();

            var bench = new WriteBenchmark();
            await bench.Setup();
            await bench.NonOrig();

            //await bench.PreText();

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
