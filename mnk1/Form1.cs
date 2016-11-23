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
        public const int D = 2;
        public const int M = 3;
        public const int FUNCTION_NUMBER = 1; // 1 -> y = cos(2 * PI * x)
                                              // 2 -> y = 5x^3 + x^2  + 5
                                              // 3 -> y = x * sin(2 * PI * x)
                                              // 4 -> y = 2 * x;
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

            double[,] matrix = new double[M, M];
            double[] b = new double[M];

            matrix = createMatrix(SelectionTable);
            b = createRightVector(SelectionTable);

            //Решение СЛАУ методом Гаусса
            LinearSystem ls = new LinearSystem(matrix, b);
            double[] w = ls.XVector; //вектор с результатом

            drawResultFunction(w, FUNCTION_NUMBER);
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

                if (functionNumber == 1)
                    yi = CosFunction(xi);
                else if (functionNumber == 2)
                    yi = PolynomFunction(xi);
                else if (functionNumber == 3)
                    yi = SinFunction(xi);
                else
                    yi = SimpleFunction(xi);
                yi += r.NextDouble() * (D - D * (-1)) + D * (-1); //Моделирование ошибки измерения

                selectionTable[i].X = xi;
                selectionTable[i].Y = yi;
            }
        }

        //Функция выводит на экран график заданной функции
        void drawFunction(int functionNumber)
        {
            double y = 0;

            switch (functionNumber)
            {

                case 1:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = CosFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);

                    }
                    break;
                case 2:
                    for (double x = 0; x < 15; x++)
                    {
                        y = PolynomFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;
                case 3:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = SinFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;
                case 4:
                    for (double x = 0; x < 10; x++)
                    {
                        y = SimpleFunction(x);
                        chart1.Series[0].Points.AddXY(x, y);
                    }
                    break;
            }

        }

        //Функция выводит на экран найденную функцию
        void drawResultFunction(double[] w, int functionNumber)
        {

            double y = 0;
            switch (functionNumber)
            {

                case 1:
                    for (double x = 0; x < Math.PI * 2; x += Math.PI / 180.0)
                    {
                        y = ResultFunction(w,x);
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
        double[,] createMatrix(selectionElement[] selectionTable)
        {
            double sum = 0;
            double[,] matrix = new double[M, M];

            for (int i = 0; i < M; i++)
                for (int j = 0; j < M; j++)
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
        double[] createRightVector(selectionElement[] selectionTable)
        {
            double sum = 0;
            double[] b = new double[M];
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
}
