using BenchmarkDotNet.Running;

namespace Fluid.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var scr = new FluidBenchmarks();
            scr.Parse();
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
