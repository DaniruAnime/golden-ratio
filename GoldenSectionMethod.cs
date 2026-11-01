using System;

namespace FunctionAnalyzerMono
{
    public static class GoldenSectionMethod
    {
        // Внутренняя страховка, не связанная с пользовательским интерфейсом.
        private const int MaxIterations = 10000;

        public static ExtremumResult Find(CompiledFunction function, double a, double b,
            double epsilon, bool findMaximum)
        {
            return Calculate(function, a, b, epsilon, findMaximum, MaxIterations);
        }

        // Только для автоматического тестирования сценария исчерпания защиты.
        internal static ExtremumResult FindWithTestLimit(CompiledFunction function, double a,
            double b, double epsilon, bool findMaximum, int testLimit)
        {
            if (testLimit < 1 || testLimit > MaxIterations)
                throw new ArgumentException("Некорректный тестовый лимит итераций.");
            return Calculate(function, a, b, epsilon, findMaximum, testLimit);
        }

        private static ExtremumResult Calculate(CompiledFunction function, double a, double b,
            double epsilon, bool findMaximum, int iterationLimit)
        {
            NumericValidation.Check(function, a, b, epsilon);
            ExtremumResult result = new ExtremumResult { A = a, B = b };
            double tau = (Math.Sqrt(5.0) - 1.0) / 2.0;
            try
            {
                // Не считаем функцию определённой на всём отрезке по одной лишь
                // успешной проверке концов. Все рабочие точки также проверяются.
                function.Evaluate(a);
                function.Evaluate(b);

                double width = b - a;
                if (width > epsilon)
                {
                    double x1 = a + (1.0 - tau) * width;
                    double x2 = a + tau * width;
                    double f1 = 0.0, f2 = 0.0;
                    bool hasF1 = false, hasF2 = false;

                    for (int i = 1; i <= iterationLimit && width > epsilon; i++)
                    {
                        // Если внутренние точки слились или совпали с границей,
                        // дальнейшее сужение в double нельзя обосновать.
                        if (!(a < x1 && x1 < x2 && x2 < b))
                        {
                            result.Status = CalculationStatus.PrecisionLimit;
                            result.Message = "Внутренние точки больше не различимы в типе double.";
                            break;
                        }

                        if (!hasF1) { f1 = function.Evaluate(x1); hasF1 = true; }
                        if (!hasF2) { f2 = function.Evaluate(x2); hasF2 = true; }

                        result.Iterations.Add(new ExtremumIteration {
                            Number = i, A = a, B = b, X1 = x1, X2 = x2,
                            F1 = f1, F2 = f2, Width = width
                        });

                        if ((findMaximum && f1 < f2) || (!findMaximum && f1 > f2))
                        {
                            a = x1;
                            x1 = x2;
                            f1 = f2;
                            hasF1 = true;
                            x2 = a + tau * (b - a);
                            hasF2 = false;
                        }
                        else if ((findMaximum && f1 > f2) || (!findMaximum && f1 < f2))
                        {
                            b = x2;
                            x2 = x1;
                            f2 = f1;
                            hasF2 = true;
                            x1 = a + (1.0 - tau) * (b - a);
                            hasF1 = false;
                        }
                        else
                        {
                            // Равные значения: для унимодальной функции достаточно
                            // сохранить центральную часть отрезка.
                            a = x1;
                            b = x2;
                            x1 = a + (1.0 - tau) * (b - a);
                            x2 = a + tau * (b - a);
                            hasF1 = false;
                            hasF2 = false;
                        }

                        double newWidth = b - a;
                        if (!(newWidth < width))
                        {
                            result.Status = CalculationStatus.PrecisionLimit;
                            result.Message = "Границы перестали сближаться из-за округления double.";
                            break;
                        }
                        width = newWidth;
                    }
                }

                result.A = a;
                result.B = b;
                result.Width = b - a;
                result.X = a + (b - a) / 2.0;
                result.FX = function.Evaluate(result.X);
                result.HasPoint = true;
                if (result.Width <= epsilon)
                {
                    result.Status = CalculationStatus.Success;
                    result.Message = "Интервал сужен до заданной точности ε.";
                }
                else if (result.Status != CalculationStatus.PrecisionLimit)
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
                result.Width = b - a;
            }
            return result;
        }
    }

    internal static class NumericValidation
    {
        public static void Check(CompiledFunction function, double a, double b, double epsilon)
        {
            if (function == null) throw new ArgumentException("Функция не задана.");
            if (!IsFinite(a) || !IsFinite(b) || !(a < b))
                throw new ArgumentException("Требуются конечные границы с a < b.");
            if (!IsFinite(b - a))
                throw new ArgumentException("Слишком широкий интервал: переполнение b - a.");
            if (!IsFinite(epsilon) || epsilon <= 0)
                throw new ArgumentException("Точность должна быть конечным положительным числом.");
        }

        public static bool IsFinite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
    }
}
