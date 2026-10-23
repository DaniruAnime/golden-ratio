using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FunctionAnalyzerMono
{
    public sealed class MainForm : Form
    {
        private TextBox extremeFormula, extremeA, extremeB, extremeEpsilon;
        private ComboBox extremeMode;
        private DataGridView extremeTable;
        private Label extremeSummary;
        private PlotPanel extremePlot;

        private TextBox rootFormula, rootA, rootB, rootEpsilonX, rootEpsilonF;
        private DataGridView rootTable;
        private Label rootSummary;
        private PlotPanel rootPlot;

        public MainForm()
        {
            Text = "Численный анализ функций — золотое сечение и бисекция";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1150, 795);
            MinimumSize = new Size(960, 660);
            Font = new Font("DejaVu Sans", 9f);
            BackColor = Color.WhiteSmoke;
            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateExtremumPage());
            tabs.TabPages.Add(CreateRootPage());
            Controls.Add(tabs);
        }

        private TabPage CreateExtremumPage()
        {
            TabPage page = new TabPage("Поиск экстремума");
            Panel top = MakeTopPanel();
            FlowLayoutPanel line1 = MakeLine(8);
            FlowLayoutPanel line2 = MakeLine(49);
            extremeFormula = AddInput(line1, "f(x)", "(x - 2)^2", 285);
            extremeA = AddInput(line1, "a", "0", 80);
            extremeB = AddInput(line1, "b", "5", 80);
            extremeEpsilon = AddInput(line1, "Точность ε", "0,000001", 120);
            Label modeLabel = new Label { Text = "Режим:", AutoSize = true, Margin = new Padding(10, 11, 4, 0) };
            line2.Controls.Add(modeLabel);
            extremeMode = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 6, 8, 0) };
            extremeMode.Items.Add("Минимум");
            extremeMode.Items.Add("Максимум");
            extremeMode.SelectedIndex = 0;
            line2.Controls.Add(extremeMode);
            line2.Controls.Add(MakeButton("Найти экстремум", FindExtremum));
            line2.Controls.Add(MakeButton("Очистить результаты", ClearExtremum));
            Label warn = new Label {
                Text = "Метод подходит, когда на выбранном участке графика только одна вершина или впадина." +
                    Environment.NewLine +
                    "Если их несколько, программа может найти не самую высокую или низкую точку.",
                Left = 10, Top = 101, Width = 1040, Height = 44, ForeColor = Color.DarkGoldenrod
            };
            top.Controls.Add(line1);
            top.Controls.Add(line2);
            top.Controls.Add(warn);

            extremeTable = MakeTable(new string[] { "№", "a", "b", "x₁", "x₂", "f(x₁)", "f(x₂)", "Длина" });
            extremePlot = new PlotPanel { Dock = DockStyle.Fill };
            SplitContainer splitter = MakeSplit(extremeTable, extremePlot);
            extremeSummary = MakeSummary("Введите формулу и нажмите «Найти экстремум».");
            page.Controls.Add(splitter);
            page.Controls.Add(extremeSummary);
            page.Controls.Add(top);
            return page;
        }

        private TabPage CreateRootPage()
        {
            TabPage page = new TabPage("Поиск корня");
            Panel top = MakeTopPanel();
            top.Height = 100;
            FlowLayoutPanel line1 = MakeLine(8);
            FlowLayoutPanel line2 = MakeLine(49);
            rootFormula = AddInput(line1, "f(x)", "x^2 - 2", 295);
            rootA = AddInput(line1, "a", "1", 80);
            rootB = AddInput(line1, "b", "2", 80);
            rootEpsilonX = AddInput(line1, "Точность εx", "0.000000001", 125);
            rootEpsilonF = AddInput(line1, "Точность εf", "0.000000001", 125);
            line2.Controls.Add(MakeButton("Найти корень", FindRoot));
            line2.Controls.Add(MakeButton("Очистить результаты", ClearRoot));
            top.Controls.Add(line1);
            top.Controls.Add(line2);

            rootTable = MakeTable(new string[] { "№", "a", "b", "c", "f(a)", "f(b)", "f(c)", "Длина" });
            rootPlot = new PlotPanel { Dock = DockStyle.Fill };
            SplitContainer splitter = MakeSplit(rootTable, rootPlot);
            rootSummary = MakeSummary("Введите формулу и нажмите «Найти корень».");
            page.Controls.Add(splitter);
            page.Controls.Add(rootSummary);
            page.Controls.Add(top);
            return page;
        }

        private static Panel MakeTopPanel()
        {
            return new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Color.WhiteSmoke };
        }

        private static FlowLayoutPanel MakeLine(int top)
        {
            return new FlowLayoutPanel { Left = 5, Top = top, Width = 1090, Height = 42,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                WrapContents = false, AutoScroll = true };
        }

        private static TextBox AddInput(FlowLayoutPanel line, string label, string initial, int width)
        {
            Panel panel = new Panel { Width = width, Height = 37, Margin = new Padding(3, 1, 6, 1) };
            Label heading = new Label { Text = label, AutoSize = false, Height = 16, Width = width - 4,
                Top = 0, Left = 1, ForeColor = Color.FromArgb(60, 65, 70) };
            TextBox input = new SafePasteTextBox { Text = initial, Top = 17, Left = 0,
                Width = width - 6, Height = 23 };
            panel.Controls.Add(heading);
            panel.Controls.Add(input);
            line.Controls.Add(panel);
            return input;
        }

        private static Button MakeButton(string label, EventHandler callback)
        {
            Button button = new Button { Text = label, AutoSize = true,
                Height = 30, Margin = new Padding(8, 6, 5, 0) };
            button.Click += callback;
            return button;
        }

        private static SplitContainer MakeSplit(Control table, Control plot)
        {
            SplitContainer splitter = new SplitContainer {
                Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.FixedSingle
            };
            // При создании WinForms ещё не вычислил размер контейнера.
            // Откладываем первоначальную установку делителя до Layout.
            bool initialPositionSet = false;
            splitter.SizeChanged += delegate {
                if (!initialPositionSet && splitter.ClientSize.Height > 300)
                {
                    splitter.SplitterDistance = (splitter.ClientSize.Height - splitter.SplitterWidth) / 2;
                    initialPositionSet = true;
                }
            };
            splitter.Panel1.Controls.Add(table);
            splitter.Panel2.Controls.Add(plot);
            return splitter;
        }

        private static DataGridView MakeTable(string[] headers)
        {
            DataGridView grid = new DataGridView {
                Dock = DockStyle.Fill, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, ReadOnly = true,
                MultiSelect = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
            };
            foreach (string header in headers)
                grid.Columns.Add(header, header);
            grid.Columns[0].FillWeight = 40;
            return grid;
        }

        private static Label MakeSummary(string message)
        {
            return new Label { Dock = DockStyle.Bottom, Height = 83, Text = message,
                Padding = new Padding(12, 9, 10, 7), BackColor = Color.FromArgb(243, 247, 251),
                BorderStyle = BorderStyle.FixedSingle, AutoEllipsis = false };
        }

        private void FindExtremum(object sender, EventArgs e)
        {
            extremeTable.Rows.Clear();
            extremePlot.ClearPlot();
            try
            {
                CompiledFunction f = new FunctionParser().Compile(extremeFormula.Text);
                double a = ReadDouble(extremeA, "a");
                double b = ReadDouble(extremeB, "b");
                double epsilon = ReadDouble(extremeEpsilon, "ε");
                bool maximum = extremeMode.SelectedIndex == 1;
                ExtremumResult result = GoldenSectionMethod.Find(f, a, b, epsilon, maximum);
                extremeTable.SuspendLayout();
                try
                {
                    foreach (ExtremumIteration it in result.Iterations)
                        extremeTable.Rows.Add(it.Number, F(it.A), F(it.B), F(it.X1), F(it.X2),
                            F(it.F1), F(it.F2), F(it.Width));
                }
                finally { extremeTable.ResumeLayout(); }
                extremePlot.ShowFunction(f, a, b, result.HasPoint, result.X, result.FX);
                extremeSummary.ForeColor = IsSuccessful(result.Status) ? Color.DarkGreen : Color.DarkRed;
                string headline = ResultHeadline(result.Status, result.Message);
                extremeSummary.Text = result.HasPoint
                    ? headline + Environment.NewLine +
                        "x* = " + F(result.X) + "; f(x*) = " + F(result.FX) + Environment.NewLine +
                        "Интервал: [" + F(result.A) + "; " + F(result.B) + "]; длина = " + F(result.Width)
                    : headline;
            }
            catch (Exception ex) { ShowInputError(ex, extremeSummary); }
        }

        private void FindRoot(object sender, EventArgs e)
        {
            rootTable.Rows.Clear();
            rootPlot.ClearPlot();
            try
            {
                CompiledFunction f = new FunctionParser().Compile(rootFormula.Text);
                double a = ReadDouble(rootA, "a");
                double b = ReadDouble(rootB, "b");
                double epsX = ReadDouble(rootEpsilonX, "εx");
                double epsF = ReadDouble(rootEpsilonF, "εf");
                RootResult result = BisectionMethod.Find(f, a, b, epsX, epsF);
                rootTable.SuspendLayout();
                try
                {
                    foreach (RootIteration it in result.Iterations)
                        rootTable.Rows.Add(it.Number, F(it.A), F(it.B), F(it.C), F(it.FA),
                            F(it.FB), F(it.FC), F(it.Width));
                }
                finally { rootTable.ResumeLayout(); }
                rootPlot.ShowFunction(f, a, b, result.HasPoint, result.X, result.FX);
                rootSummary.ForeColor = IsSuccessful(result.Status) ? Color.DarkGreen : Color.DarkRed;
                string headline = ResultHeadline(result.Status, result.Message);
                rootSummary.Text = result.HasPoint
                    ? headline + Environment.NewLine +
                        "x* = " + F(result.X) + "; f(x*) = " + F(result.FX) +
                        "; |f(x*)| = " + F(Math.Abs(result.FX)) + Environment.NewLine +
                        "Интервал: [" + F(result.A) + "; " + F(result.B) +
                        "]; оценка ошибки x ≤ " + F(result.ArgumentBound)
                    : headline;
            }
            catch (Exception ex) { ShowInputError(ex, rootSummary); }
        }

        private void ClearExtremum(object sender, EventArgs e)
        {
            extremeTable.Rows.Clear();
            extremePlot.ClearPlot();
            extremeSummary.ForeColor = Color.Black;
            extremeSummary.Text = "Результаты очищены. Введённые параметры сохранены.";
        }

        private void ClearRoot(object sender, EventArgs e)
        {
            rootTable.Rows.Clear();
            rootPlot.ClearPlot();
            rootSummary.ForeColor = Color.Black;
            rootSummary.Text = "Результаты очищены. Введённые параметры сохранены.";
        }

        private void ShowInputError(Exception ex, Label output)
        {
            output.ForeColor = Color.DarkRed;
            output.Text = "Ошибка: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Не удалось выполнить расчёт",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static double ReadDouble(TextBox input, string field)
        {
            string value = input.Text.Trim().Replace(',', '.');
            double number;
            if (value.Length == 0 || !double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out number) || !NumericValidation.IsFinite(number))
                throw new ArgumentException("Поле «" + field + "»: введите конечное число (точка или запятая).");
            return number;
        }

        private static string F(double number)
        {
            return number.ToString("G12", CultureInfo.InvariantCulture);
        }

        private static bool IsSuccessful(CalculationStatus status)
        {
            return status == CalculationStatus.Success || status == CalculationStatus.BoundaryRoot;
        }

        private static string ResultHeadline(CalculationStatus status, string details)
        {
            switch (status)
            {
                case CalculationStatus.Success: return "Точность достигнута.";
                case CalculationStatus.BoundaryRoot: return "Корень найден на границе интервала.";
                case CalculationStatus.InvalidBracket:
                    return "Ошибка: на концах интервала нет смены знака.";
                case CalculationStatus.IterationLimit:
                    return "Ошибка: заданная точность не достигнута (сработал защитный лимит).";
                case CalculationStatus.PrecisionLimit:
                    return "Ошибка: точность чисел не позволяет продолжить расчёт.";
                case CalculationStatus.CalculationError:
                    const string prefix = "Ошибка вычисления функции: ";
                    if (string.IsNullOrEmpty(details)) return "Ошибка: не удалось вычислить функцию.";
                    if (details.StartsWith(prefix, StringComparison.Ordinal))
                        details = details.Substring(prefix.Length);
                    return "Ошибка: " + details;
                default: return "Ошибка: вычисление не выполнено.";
            }
        }

    }
}
