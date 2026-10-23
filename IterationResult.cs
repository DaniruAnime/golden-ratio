using System.Collections.Generic;

namespace FunctionAnalyzerMono
{
    public enum CalculationStatus
    {
        Success,
        BoundaryRoot,
        IterationLimit,
        PrecisionLimit,
        CalculationError,
        InvalidBracket
    }

    public sealed class ExtremumIteration
    {
        public int Number { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double F1 { get; set; }
        public double F2 { get; set; }
        public double Width { get; set; }
    }

    public sealed class ExtremumResult
    {
        public ExtremumResult() { Iterations = new List<ExtremumIteration>(); }
        public List<ExtremumIteration> Iterations { get; private set; }
        public CalculationStatus Status { get; set; }
        public string Message { get; set; }
        public double X { get; set; }
        public double FX { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public double Width { get; set; }
        public bool HasPoint { get; set; }
    }

    public sealed class RootIteration
    {
        public int Number { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double FA { get; set; }
        public double FB { get; set; }
        public double FC { get; set; }
        public double Width { get; set; }
    }

    public sealed class RootResult
    {
        public RootResult() { Iterations = new List<RootIteration>(); }
        public List<RootIteration> Iterations { get; private set; }
        public CalculationStatus Status { get; set; }
        public string Message { get; set; }
        public double X { get; set; }
        public double FX { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public double ArgumentBound { get; set; }
        public bool HasPoint { get; set; }
    }
}
