using BenchmarkDotNet.Running;

namespace Fluid.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var scr = new FluidBenchmarks();
            var o = scr.ParseBig();
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
