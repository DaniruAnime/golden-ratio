using System;

namespace FunctionAnalyzerMono
{
    public static class BisectionMethod
    {
        // Защита от зацикливания; пользователю этот параметр не предлагается.
        private const int MaxIterations = 10000;

        public static RootResult Find(CompiledFunction function, double a, double b,
            double epsilonX, double epsilonF)
        {
            return Calculate(function, a, b, epsilonX, epsilonF, MaxIterations);
        }

        // Доступно только тестам внутри этой же сборки.
        internal static RootResult FindWithTestLimit(CompiledFunction function, double a,
            double b, double epsilonX, double epsilonF, int testLimit)
        {
            if (testLimit < 1 || testLimit > MaxIterations)
                throw new ArgumentException("Некорректный тестовый лимит итераций.");
            return Calculate(function, a, b, epsilonX, epsilonF, testLimit);
        }

        private static RootResult Calculate(CompiledFunction function, double a, double b,
            double epsilonX, double epsilonF, int iterationLimit)
        {
            NumericValidation.Check(function, a, b, epsilonX);
            if (!NumericValidation.IsFinite(epsilonF) || epsilonF <= 0)
                throw new ArgumentException("Точность по функции εf должна быть положительной и конечной.");

            RootResult result = new RootResult { A = a, B = b };
            try
            {
                double fa = function.Evaluate(a);
                double fb = function.Evaluate(b);
                if (fa == 0.0 || fb == 0.0)
                {
                    double root = fa == 0.0 ? a : b;
                    result.A = root;
                    result.B = root;
                    result.X = root;
                    result.FX = 0.0;
                    result.ArgumentBound = 0.0;
                    result.HasPoint = true;
                    result.Status = CalculationStatus.BoundaryRoot;
                    result.Message = "Точный корень найден на границе до начала итераций.";
                    return result;
                }
                if (SameSign(fa, fb))
                {
                    result.Status = CalculationStatus.InvalidBracket;
                    result.Message = "На концах интервала нет смены знака. Метод бисекции не " +
                        "может гарантировать результат на таком интервале; корни внутри могут существовать.";
                    return result;
                }

                for (int i = 1; i <= iterationLimit; i++)
                {
                    double width = b - a;
                    double c = a + width / 2.0;
                    if (!(a < c && c < b))
                    {
                        result.Status = CalculationStatus.PrecisionLimit;
                        result.Message = "Между границами больше нет представимого числа double.";
                        break;
                    }

                    double fc = function.Evaluate(c);
                    result.Iterations.Add(new RootIteration {
                        Number = i, A = a, B = b, C = c,
                        FA = fa, FB = fb, FC = fc, Width = width
                    });
                    result.X = c;
                    result.FX = fc;
                    // Учитываем округление середины: расстояние до каждой
                    // старой границы может чуть отличаться от width/2.
                    result.ArgumentBound = Math.Max(c - a, b - c);
                    result.HasPoint = true;

                    if (fc == 0.0)
                    {
                        a = b = c;
                        result.Status = CalculationStatus.Success;
                        result.Message = "Найдено точное нулевое значение функции.";
                        break;
                    }

                    if (SameSign(fa, fc)) { a = c; fa = fc; }
                    else { b = c; fb = fc; }

                    if (Math.Abs(fc) <= epsilonF)
                    {
                        result.Status = CalculationStatus.Success;
                        result.Message = "Достигнута точность по невязке |f(x)| ≤ εf.";
                        break;
                    }
                    if (result.ArgumentBound <= epsilonX)
                    {
                        result.Status = CalculationStatus.Success;
                        result.Message = "Достигнута точность по аргументу: оценка ошибки ≤ εx.";
                        break;
                    }
                }

                result.A = a;
                result.B = b;
                if (result.Status != CalculationStatus.Success &&
                    result.Status != CalculationStatus.PrecisionLimit)
                {
                    result.Status = CalculationStatus.IterationLimit;
                    result.Message = "Достигнут внутренний лимит итераций; необходимая точность не достигнута.";
                }
            }
            catch (ExpressionException ex)
            {
                result.Status = CalculationStatus.CalculationError;
                result.Message = "Ошибка вычисления функции: " + ex.Message;
                result.HasPoint = false;
                result.A = a;
                result.B = b;
            }
            return result;
        }

        private static bool SameSign(double a, double b)
        {
            return (a > 0.0 && b > 0.0) || (a < 0.0 && b < 0.0);
        }
    }
}
