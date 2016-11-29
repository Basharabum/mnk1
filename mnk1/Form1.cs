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
        public const int N = 100; // размер выборки
        public const int D = 1;
        public const int M = 30;
        public const int MISTAKE_TYPE = 1;    // 1 -> с равномерным распределением
                                              // 2 -> с нормальным распределением

        public const int FUNCTION_NUMBER = 1; // 1 -> y = cos(2 * PI * x)
                                              // 2 -> y = 5x^3 + x^2  + 5
                                              // 3 -> y = x * sin(2 * PI * x)
                                              // 4 -> y = 2 * x; простая функция

        public const int COUNT_OF_BLOCKS_IN_CROSS = 10; //количество блоков при кросс-валидации

        public Form1()
        {
            InitializeComponent();
            chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series[0].Name = "Функция";
            chart1.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            chart1.Series[1].Name = "Выборка";
            chart1.Series[2].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series[2].Name = "Найденная функция";

            //Отрисовка графика для заданной функции
            drawFunction(FUNCTION_NUMBER);

            //Создание массива под выборку
            selectionElement[] SelectionTable = new selectionElement[N];
            for (int i = 0; i < N; i++)
            {
                SelectionTable[i] = new selectionElement();
            }

            //Генерирование выборки
            createSelection(FUNCTION_NUMBER, SelectionTable);

            //вывод выборки на экран
            foreach (selectionElement elem in SelectionTable)
            {
                chart1.Series[1].Points.AddXY(elem.X, elem.Y);
            }

            double[] w = findPolynomCoeff(SelectionTable, M);

            crossValidation(SelectionTable, M, 1);
            //Вывод на экран найденного полинома
            drawResultFunction(w, FUNCTION_NUMBER);
        }

        //Функция по выборке строит систему линейных уравнений и решает ее, возвращая массив неизвестных w
        double[] findPolynomCoeff(selectionElement[] selectionTable, int m)
        {
            double[,] matrix;
            double[] b;

            //создание матрицы на основе выборки
            matrix = createMatrix(selectionTable, m);
            //создание вектора правых частей
            b = createRightVector(selectionTable, m);

            //Решение СЛАУ методом Гаусса
            LinearSystem ls = new LinearSystem(matrix, b);
            double[] w = ls.XVector; //вектор с результатом
            return w;
        }

        //Функция генерирует выборку
        void createSelection(int functionNumber, selectionElement[] selectionTable)
        {
            double xi = 0;
            double yi = 0;
            Random r = new Random();

            for (int i = 0; i < N; i++)
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
                }

                //Добавление ошибки
                if (MISTAKE_TYPE == 1)
                {
                    //min + r.NextDouble() * (max - min);
                    yi += D * (-1) + r.NextDouble() * (D - D * (-1)); //Моделирование ошибки измерения с равномерным распределением на отрезке [-D,D]
                }
                else
                {
                    yi += r.NextNormal() * D; //Моделирование ошибки измерения с нормальным распределением на отрезке (0,D)
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

            return resultSum /= 2;
        }


        /*  
         *   Кросс-валидация
         *
         *   SelectionTable -  выборка
         *                m -  степень полинома
         *                l -  параметр лямбда
         */
        void crossValidation(selectionElement[] selectionTable, int m, int l)
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
            double error = 0;
            double fullError = 0;
            for (int i = 0; i < COUNT_OF_BLOCKS_IN_CROSS; i++)
            {
                //Добавление элементов текущей группы элементов для контроля в массив контроля
                for (int j = 0; j < countOfElementsInBlock; j++)
                {
                    blockForControl[j].X = selectionTable[i * COUNT_OF_BLOCKS_IN_CROSS + j].X;
                    blockForControl[j].Y = selectionTable[i * COUNT_OF_BLOCKS_IN_CROSS + j].Y;
                }
                //Добавление элементов текущей группы элементов для обучения в массив обучения
                for (int g = 0; g < selectionTable.Length; g++)
                {
                    if ((g >= i * COUNT_OF_BLOCKS_IN_CROSS) && (g < i * COUNT_OF_BLOCKS_IN_CROSS + countOfElementsInBlock))//пропустить элементы выборки, которые уже записаны в контроль
                        continue;

                    blockForEducation[t].X = selectionTable[g].X;
                    blockForEducation[t].Y = selectionTable[g].Y;
                    t++;
                }
                t = 0;

                //Получение полинома из обучающей выборки
                double[]w = findPolynomCoeff(blockForEducation, m);
                //Рассчет ошибки с помощью контрольной выборки
                error = calculateError(w, blockForControl) / blockForControl.Length;
                fullError += error;
                error = 0;
            }

            fullError = fullError / COUNT_OF_BLOCKS_IN_CROSS;
            

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
            }

        }

        //Функция выводит на экран найденный полином
        void drawResultFunction(double[] w, int functionNumber)
        {
            double y = 0;
            switch (functionNumber)
            {

                case 1:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[2].Points.AddXY(x, y);
                    }
                    break;
                case 2:
                    for (double x = 0; x < 15; x++)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[2].Points.AddXY(x, y);
                    }
                    break;
                case 3:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[2].Points.AddXY(x, y);
                    }
                    break;
                case 4:
                    for (double x = 0; x < 10; x++)
                    {
                        y = ResultFunction(w, x);
                        chart1.Series[2].Points.AddXY(x, y);
                    }
                    break;
            }

        }


        double CosFunction(double x) { return Math.Cos(2 * Math.PI * x); }
        double SinFunction(double x) { return x * Math.Sin(2 * Math.PI * x); }
        double PolynomFunction(double x) { return 5 * Math.Pow(x, 3) + Math.Pow(x, 2) + 5; }

        double ResultFunction(double[] w, double x)
        {
            double sum = 0;
            for (int i = 0; i < w.Length; i++)
            {
                sum += w[i] * Math.Pow(x, i);
            }
            return sum;
        }
        double SimpleFunction(double x) { return 2 * x; }

        //Создание и заполнение матрицы (A)
        double[,] createMatrix(selectionElement[] selectionTable, int m)
        {
            double sum = 0;
            double[,] matrix = new double[m, m];

            for (int i = 0; i <m; i++)
                for (int j = 0; j < m; j++)
                {
                    for (int g = 0; g < selectionTable.Length; g++)
                    {
                        sum += Math.Pow(selectionTable[g].X, i + j);
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
            for (int i = 0; i < M; i++)
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
