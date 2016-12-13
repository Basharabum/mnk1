using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace mnk1
{
    public partial class Form1 : Form
    {

        public const int COUNT_OF_BLOCKS_IN_CROSS = 20; //количество блоков при кросс-валидации

        public Form1()
        {
            InitializeComponent();
            chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series[0].Name = "Функция";
            chart1.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            chart1.Series[1].Name = "Выборка";
            chart1.Series[2].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series[2].Name = "Найденная функция";
            chart1.Series[3].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series[3].Name = "Функция после кросс-валидации";
            chart1.Series[4].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series[4].Name = "Функция после регуляризации";
        }

        //Функция по выборке строит систему линейных уравнений и решает ее, возвращая массив неизвестных w
        double[] findPolynomCoeff(selectionElement[] selectionTable, int m, double lambda = -1)
        {
            double[,] matrix;
            double[] b;

            //создание матрицы на основе выборки
            matrix = createMatrix(selectionTable, m, lambda);
            //создание вектора правых частей
            b = createRightVector(selectionTable, m);

            //Решение СЛАУ методом Гаусса
            LinearSystem ls = new LinearSystem(matrix, b);
            double[] w = ls.XVector; //вектор с результатом

            bool check = SolutionCheck(matrix, b, w);

            return w;
        }

        //Функция проверяет решение СЛАУ
        bool SolutionCheck(double[,] matrix, double[] b, double[] w)
        {

            double sum = 0;
            double[] result = new double[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                for (int j = 0; j < b.Length; j++)
                {
                    sum += matrix[i, j] * w[j];
                }
                result[i] = sum;
                sum = 0;
            }

            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] != result[i])
                    return false;
            }
            return true;
        }
        //Функция генерирует выборку
        void createSelection(int functionNumber, selectionElement[] selectionTable, int n, int mistakeType, double d)
        {
            double xi = 0;
            double yi = 0;
            Random r = new Random();

            for (int i = 0; i < n; i++)
            {
                xi = r.NextDouble() * (2 * Math.PI - 0) + 0;

                //Вычисление значения функции
                switch (functionNumber)
                {
                    case 1:
                        yi = CosFunction(xi);
                        break;
                    case 2:
                        yi = PolynomFunction(xi);
                        break;
                    case 3:
                        yi = SinFunction(xi);
                        break;
                    case 4:
                        yi = SimpleFunction(xi);
                        break;
                    case 5:
                        yi = SimpleSinFunction(xi);
                        break;
                    case 6:
                        yi = SimpleCosFunction(xi);
                        break;
                    case 7:
                        yi = SinCosFunction(xi);
                        break;
                }
                //Добавление ошибки
                if (mistakeType == 1)
                {
                    //min + r.NextDouble() * (max - min);
                    yi += d * (-1) + r.NextDouble() * (d - d * (-1)); //Моделирование ошибки измерения с равномерным распределением на отрезке [-D,D]
                }
                else
                {
                    yi += r.NextNormal() * d; //Моделирование ошибки измерения с нормальным распределением на отрезке (0,D)
                }
                selectionTable[i].X = xi;
                selectionTable[i].Y = yi;
            }
        }

        double calculateError(double[] w, selectionElement[] SelectionTable)
        {
            double resultSum = 0;
            for (int i = 0; i < SelectionTable.Length; i++)
                resultSum += Math.Pow(SelectionTable[i].Y - ResultFunction(w, SelectionTable[i].X), 2);

            return resultSum;
        }

        //Кросс-валидация
        double crossValidation(selectionElement[] selectionTable, int m, double lambda = -1)
        {
            //Разделение выборки на блоки
            int countOfElementsInBlock = selectionTable.Length / COUNT_OF_BLOCKS_IN_CROSS;

            selectionElement[] blockForControl = new selectionElement[countOfElementsInBlock];
            for (int i = 0; i < countOfElementsInBlock; i++)
                blockForControl[i] = new selectionElement();

            selectionElement[] blockForEducation = new selectionElement[selectionTable.Length - countOfElementsInBlock];
            for (int i = 0; i < selectionTable.Length - countOfElementsInBlock; i++)
                blockForEducation[i] = new selectionElement();

            int t = 0;
            int d = 0;
            double blockError = 0; //ошибка для текущего блока контроля
            double fullError = 0; //полная общая ошибка для всей выборки

            int elementIter = selectionTable.Length / countOfElementsInBlock;
            int needElement = 0;
            for (int i = 0; i < COUNT_OF_BLOCKS_IN_CROSS; i++)
            {
                needElement = i;
                //Добавление элементов текущей группы элементов для контроля в массив контроля
                for (int j = 0; j < selectionTable.Length; j++)
                {
                    if (j == needElement)
                    {
                        blockForControl[d].X = selectionTable[j].X;
                        blockForControl[d].Y = selectionTable[j].Y;
                        d++;
                        needElement += elementIter;
                    }
                    else
                    {
                        blockForEducation[t].X = selectionTable[j].X;
                        blockForEducation[t].Y = selectionTable[j].Y;
                        t++;
                    }
                }

                t = 0;
                d = 0;
                //Получение полинома из обучающей выборки
                double[] w = findPolynomCoeff(blockForEducation, m, lambda);
                //Рассчет ошибки с помощью контрольной выборки
                blockError = calculateError(w, blockForControl) / blockForControl.Length;
                fullError += blockError;
                blockError = 0;
            }

            fullError = fullError / COUNT_OF_BLOCKS_IN_CROSS;

            return fullError;
        }

        void regularization(selectionElement[] selectionTable, int functionNumber, int m, double lambda)
        {
            double[] w = findPolynomCoeff(selectionTable, m, lambda);
            drawResultFunction(w, functionNumber, false, true);
        }

        //Функция выводит на экран график заданной функции
        void drawFunction(int functionNumber)
        {
            double y = 0;

            switch (functionNumber)
            {
                case 1:
                    // y = cos(2 * PI * x)
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = CosFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);

                    }
                    break;
                case 2:
                    //y = 5x^3 + x^2  + 5
                    for (double x = 0; x < 15; x++)
                    {
                        y = PolynomFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;
                case 3:
                    //y = x * sin(2 * PI * x)
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = SinFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;
                case 4:
                    //y = 2 * x;
                    for (double x = 0; x < 10; x++)
                    {
                        y = SimpleFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;
                case 5:
                    // y = sin(x)
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = SimpleSinFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;

                case 6:
                    // y = cos(x)
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = SimpleCosFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;
                case 7:
                    // y = sin(x) - sin(x)*cos(x)
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = SinCosFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;
            }
        }

        //Функция выводит на экран найденный полином
        void drawResultFunction(double[] w, int functionNumber, bool cross = false, bool regular = false)
        {
            int seriesNumber = 2; //номер серии, в которую рисовать
            if (cross == true)
                seriesNumber = 3;
            if (regular == true)
                seriesNumber = 4;

            double y = 0;
            switch (functionNumber)
            {
                case 1:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[seriesNumber].Points.AddXY(x, y);
                    }
                    break;
                case 2:
                    for (double x = 0; x < 15; x++)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[seriesNumber].Points.AddXY(x, y);
                    }
                    break;
                case 3:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[seriesNumber].Points.AddXY(x, y);
                    }
                    break;
                case 4:
                    for (double x = 0; x < 10; x++)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[seriesNumber].Points.AddXY(x, y);
                    }
                    break;
                case 5:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[seriesNumber].Points.AddXY(x, y);
                    }
                    break;
                case 6:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[seriesNumber].Points.AddXY(x, y);
                    }
                    break;
                case 7:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[seriesNumber].Points.AddXY(x, y);
                    }
                    break;
            }
        }

        //Функции:
        double CosFunction(double x) { return Math.Cos(2 * Math.PI * x); }
        double SinFunction(double x) { return x * Math.Sin(2 * Math.PI * x); }
        double SimpleSinFunction(double x) { return Math.Sin(x); }
        double SimpleCosFunction(double x) { return Math.Cos(x); }
        double SinCosFunction(double x) { return Math.Sin(x) - Math.Sin(x) * Math.Cos(x); }
        double PolynomFunction(double x) { return 5 * Math.Pow(x, 3) + Math.Pow(x, 2) + 5; }
        double SimpleFunction(double x) { return 2 * x; }
        double ResultFunction(double[] w, double x)
        {
            double sum = 0;
            for (int i = 0; i < w.Length; i++)
            {
                sum += w[i] * Math.Pow(x, i);
            }
            return sum;
        }

        //Создание и заполнение матрицы (A)
        double[,] createMatrix(selectionElement[] selectionTable, int m, double lambda = -1)
        {
            double sum = 0;
            double[,] matrix = new double[m, m];
            
            for (int i = 0; i < m; i++)
                for (int j = 0; j < m; j++)
                {
                    for (int g = 0; g < selectionTable.Length; g++)
                    {
                        sum += Math.Pow(selectionTable[g].X, i + j);
                    }

                    if ((lambda != -1) && (i == j)) //регуляризация
                    {
                        sum += lambda;
                    }

                    matrix[i, j] = sum;
                    sum = 0;
                }

            return matrix;
        }

        //Создание и заполнение вектора правых частей (b)
        double[] createRightVector(selectionElement[] selectionTable, int m)
        {
            double sum = 0;
            double[] b = new double[m];
            for (int i = 0; i < m; i++)
            {
                for (int k = 0; k < selectionTable.Length; k++)
                {
                    sum += selectionTable[k].Y * Math.Pow(selectionTable[k].X, i);
                }
                b[i] = sum;
                sum = 0;
            }
            return b;
        }

        //Чекбокс отображения функции
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                chart1.Series[0].Enabled = true;
            else
                chart1.Series[0].Enabled = false;
        }

        //Чекбокс отображения выборки
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                chart1.Series[1].Enabled = true;
            else
                chart1.Series[1].Enabled = false;
        }

        //Чекбокс отображения найденного полинома
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
                chart1.Series[2].Enabled = true;
            else
                chart1.Series[2].Enabled = false;
        }
        //Чекбокс отображения полинома после кросс-валидации
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
                chart1.Series[3].Enabled = true;
            else
                chart1.Series[3].Enabled = false;

        }
        //Чекбокс отображения полинома после регуляризации
        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                chart1.Series[4].Enabled = true;
            else
                chart1.Series[4].Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                chart1.Series[i].Points.Clear();
            }

            int functionNumber = 0;
            int mistakeType = 1;

            if (radioButton1.Checked)
            {
                functionNumber = 1;
            }
            else if (radioButton2.Checked)
            {
                functionNumber = 2;
            }
            else if (radioButton3.Checked)
            {
                functionNumber = 3;
            }
            else if (radioButton6.Checked)
            {
                functionNumber = 5;
            }
            else if (radioButton7.Checked)
            {
                functionNumber = 6;
            }
            else if (radioButton8.Checked)
            {
                functionNumber = 7;
            }

            if (radioButton4.Checked)
            {
                mistakeType = 1;
            }
            else if (radioButton5.Checked)
            {
                mistakeType = 2;
            }

            int n = Convert.ToInt32(numericUpDown1.Text);
            int m = Convert.ToInt32(numericUpDown3.Text);
            double d = Convert.ToDouble(numericUpDown2.Text);

            //Отрисовка графика для заданной функции
            drawFunction(functionNumber);

            //Создание массива под выборку
            selectionElement[] SelectionTable = new selectionElement[n];
            for (int i = 0; i < n; i++)
            {
                SelectionTable[i] = new selectionElement();
            }

            //Генерирование выборки
            createSelection(functionNumber, SelectionTable, n, mistakeType, d);

            //вывод выборки на экран
            foreach (selectionElement elem in SelectionTable)
            {
                chart1.Series[1].Points.AddXY(elem.X, elem.Y);
            }

            double[] w = findPolynomCoeff(SelectionTable, m);
            drawResultFunction(w, functionNumber);

            double currentError = 0;    // ошибка, полученная для текущей степени M полинома 
            double minError = 0;        // минимальная ошибка среди всех степеней M полинома
            int bestM = 1;              // лучшая степень полинома
            double bestLambda = 0;      // лучшее значение лямбды
            
            //0-ая итерация
            minError = crossValidation(SelectionTable, m, 0.3);
            bestM = m;

            for (int mCh = 3; mCh < 15; mCh++)
                for (double lam = 0.0; lam < 1.0; lam += 0.1)
                {
                    currentError = crossValidation(SelectionTable, mCh, lam);
                    if (currentError < minError)
                    {
                        minError = currentError;
                        bestM = mCh;
                        bestLambda = lam;
                    }
                }

            label6.Text = minError.ToString();
            label8.Text = bestM.ToString();
            label10.Text = bestLambda.ToString();
            double[] wBest = findPolynomCoeff(SelectionTable, bestM);
            drawResultFunction(wBest, functionNumber, true);

            regularization(SelectionTable, functionNumber, bestM, bestLambda);
        }
    }

    //Класс Элемент выборки
    class selectionElement
    {
        double x;
        double y;

        public selectionElement() { x = 0; y = 0; }
        public selectionElement(double theX, double theY) { x = theX; y = theY; }

        public double X
        {
            get { return x; }
            set { x = value; }
        }
        public double Y
        {
            get { return y; }
            set { y = value; }
        }
    }

    // Преобразование Бокса — Мюллера для получения нормально распределенной ошибки

    static class RandomExtension
    {
        private static bool haveReadyValue;
        private static double readyValue;

        public static double NextNormal(this Random rand)
        {
            double x = 0;
            double y = 0;
            double s = 0;

            //Если уже есть сохраненная готовая величина, удовлетворяющая стандартному нормальному распределению
            if (haveReadyValue)
            {
                haveReadyValue = false;
                return readyValue;
            }
            else
            {
                do
                {
                    //генерация x и y — независимых случайных величин, равномерно распределённых на отрезке [-1,1]
                    x = 2 * rand.NextDouble() - 1;
                    y = 2 * rand.NextDouble() - 1;
                    //Вычисление s по формуле: s = x^2 + y^2
                    s = x * x + y * y;
                } while (s > 1 || s == 0); //если s > 1 или s = 0, то значения x и y следует сгенерировать заново

                double root = Math.Sqrt(-2 * Math.Log(s) / s);

                //сохраняем одну из величин, для последующих вызовов
                readyValue = y * root;
                haveReadyValue = true;

                return x * root;
            }
        }
    }
}
