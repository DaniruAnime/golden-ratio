using System;
using System.Globalization;

namespace FunctionAnalyzerMono
{
    public static class SelfTests
    {
        private static int successes;
        private static int failures;

        public static int Run()
        {
            successes = failures = 0;
            Check("1. Автоматический минимум (x-2)^2", delegate {
                ExtremumResult r = GoldenSectionMethod.Find(F("(x - 2)^2"), 0, 5, 1e-8, false);
                Assert(r.Status == CalculationStatus.Success, r.Message);
                Assert(r.Width <= 1e-8 && r.Iterations.Count > 1, "Нарушено условие остановки");
                Near(r.X, 2, 1e-7);
                Near(r.FX, 0, 1e-12);
            });
            Check("2. Автоматический максимум -(x-2)^2", delegate {
                ExtremumResult r = GoldenSectionMethod.Find(F("-(x - 2)^2"), 0, 5, 1e-8, true);
                Assert(r.Status == CalculationStatus.Success, r.Message);
                Assert(r.Width <= 1e-8, "Интервал слишком широк");
                Near(r.X, 2, 1e-7);
                Near(r.FX, 0, 1e-12);
            });
            Check("3. Автоматический корень x^2-2", delegate {
                RootResult r = BisectionMethod.Find(F("x^2 - 2"), 1, 2, 1e-11, 1e-12);
                Assert(r.Status == CalculationStatus.Success, r.Message);
                Near(r.X, Math.Sqrt(2), 1e-9);
                Assert(Math.Abs(r.FX) < 1e-8, "Слишком большая невязка");
                Assert(r.Iterations.Count > 1, "Бисекция не сделала нужные шаги");
            });
            Check("4. Корни на обеих границах без итераций", delegate {
                RootResult left = BisectionMethod.Find(F("x^2 - 1"), 1, 3, 1e-8, 1e-8);
                RootResult right = BisectionMethod.Find(F("x^2 - 1"), 0, 1, 1e-8, 1e-8);
                Assert(left.Status == CalculationStatus.BoundaryRoot && left.Iterations.Count == 0,
                    "Левый корень не распознан");
                Assert(right.Status == CalculationStatus.BoundaryRoot && right.Iterations.Count == 0,
                    "Правый корень не распознан");
                Near(left.X, 1, 0);
                Near(right.X, 1, 0);
            });
            Check("5. Отсутствует смена знака", delegate {
                RootResult r = BisectionMethod.Find(F("x^2 + 1"), -1, 1, 1e-8, 1e-8);
                Assert(r.Status == CalculationStatus.InvalidBracket && !r.HasPoint,
                    "Метод должен отказаться");
            });
            Check("6. Внутренние корни при одинаковом знаке", delegate {
                RootResult r = BisectionMethod.Find(F("x^2 - 4"), -3, 3, 1e-8, 1e-8);
                Assert(r.Status == CalculationStatus.InvalidBracket, "Метод должен отказаться");
                Assert(r.Message.IndexOf("могут существовать", StringComparison.Ordinal) >= 0,
                    "Неверное сообщение о возможных корнях");
            });
            Check("7. Некорректные формулы и деление на ноль", delegate {
                MustThrow(delegate { F("(x + 1"); });
                MustThrow(delegate { F("unknown(x)"); });
                MustThrow(delegate { F("1/(x-x)").Evaluate(4); });
            });
            Check("8. Область определения", delegate {
                ExtremumResult r = GoldenSectionMethod.Find(F("sqrt(x)"), -2, 3, 1e-8, false);
                Assert(r.Status == CalculationStatus.CalculationError && !r.HasPoint,
                    "Должна быть ошибка области определения при минимизации");
                RootResult b = BisectionMethod.Find(F("ln(x)"), -1, 2, 1e-8, 1e-8);
                Assert(b.Status == CalculationStatus.CalculationError && !b.HasPoint,
                    "Должна быть ошибка области определения при поиске корня");
            });
            Check("9. Приоритет операторов и десятичная запятая", delegate {
                Near(F("-x^2").Evaluate(3), -9, 0);
                Near(F("(-x)^2").Evaluate(3), 9, 0);
                Near(F("2^3^2").Evaluate(0), 512, 0);
                Near(F("2^-3").Evaluate(0), .125, 1e-15);
                Near(F("sin(pi/2) + 1,25 + 1e-2").Evaluate(0), 2.26, 1e-12);
                Near(F("log(100)+ln(e)").Evaluate(0), 3, 1e-12);
            });
            Check("10. Внутренний защитный лимит (только тестовый доступ)", delegate {
                ExtremumResult r = GoldenSectionMethod.FindWithTestLimit(F("(x-2)^2"),
                    0, 5, 1e-14, false, 1);
                Assert(r.Status == CalculationStatus.IterationLimit && r.Iterations.Count == 1,
                    "Не распознан лимит золотого сечения");
                RootResult b = BisectionMethod.FindWithTestLimit(F("x^2-2"),
                    1, 2, 1e-14, 1e-14, 1);
                Assert(b.Status == CalculationStatus.IterationLimit && b.Iterations.Count == 1,
                    "Не распознан лимит бисекции");
            });
            Check("11. Чрезвычайно малая положительная точность", delegate {
                double smallest = double.Epsilon;
                ExtremumResult r = GoldenSectionMethod.Find(F("(x-2)^2"), 0, 5, smallest, false);
                Assert(r.Status == CalculationStatus.PrecisionLimit && r.Iterations.Count > 1,
                    "Должна быть обнаружена невозможность дальнейшего сужения");
                RootResult b = BisectionMethod.Find(F("x^2-2"), 1, 2, smallest, smallest);
                Assert(b.Status == CalculationStatus.PrecisionLimit && b.Iterations.Count > 1,
                    "Должна быть обнаружена невозможность дальнейшей бисекции");
            });
            Check("12. Уже достаточная точность на исходном отрезке", delegate {
                ExtremumResult r = GoldenSectionMethod.Find(F("x^2"), -1, 1, 2.0, false);
                Assert(r.Status == CalculationStatus.Success && r.Iterations.Count == 0,
                    "Точности уже достаточно, итерации не нужны");
            });
            Check("13. Критерий по невязке и критерий по аргументу", delegate {
                RootResult residual = BisectionMethod.Find(F("x^2-2"), 1, 2, double.Epsilon, 0.2);
                Assert(residual.Status == CalculationStatus.Success && Math.Abs(residual.FX) <= 0.2,
                    "Бисекция должна останавливаться по невязке");
                RootResult argument = BisectionMethod.Find(F("x^2-2"), 1, 2, 0.1, double.Epsilon);
                Assert(argument.Status == CalculationStatus.Success && argument.ArgumentBound <= 0.1,
                    "Бисекция должна останавливаться по аргументу");
            });
            Check("14. Проверка входных данных", delegate {
                MustThrow(delegate { GoldenSectionMethod.Find(F("x"), 2, 1, 1e-6, false); });
                MustThrow(delegate { BisectionMethod.Find(F("x"), -1, 1, 0, 1e-6); });
                MustThrow(delegate { BisectionMethod.Find(F("x"), -1, 1, 1e-6, -1); });
                MustThrow(delegate { GoldenSectionMethod.Find(F("x"), -1, 1, 0, false); });
                MustThrow(delegate { GoldenSectionMethod.FindWithTestLimit(F("x"), -1, 1, 1e-6, false, 0); });
            });
            Console.WriteLine("Результат: пройдено {0}, ошибок {1}.", successes, failures);
            return failures == 0 ? 0 : 1;
        }

        private static CompiledFunction F(string text) { return new FunctionParser().Compile(text); }

        private static void Check(string name, Action action)
        {
            try { action(); successes++; Console.WriteLine("[OK] " + name); }
            catch (Exception ex) { failures++; Console.WriteLine("[ОШИБКА] " + name + ": " + ex.Message); }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Near(double actual, double expected, double tolerance)
        {
            if (Math.Abs(actual - expected) > tolerance)
                throw new Exception("Ожидалось " + expected.ToString("G17", CultureInfo.InvariantCulture) +
                    ", получено " + actual.ToString("G17", CultureInfo.InvariantCulture));
        }

        private static void MustThrow(Action action)
        {
            try { action(); }
            catch (ExpressionException) { return; }
            catch (ArgumentException) { return; }
            throw new Exception("Ожидалась диагностируемая ошибка.");
        }
    }
}
