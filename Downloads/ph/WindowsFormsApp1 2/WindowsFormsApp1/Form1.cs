﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OxyPlot;
using LiveCharts;
using LiveCharts.WinForms;
using LiveCharts.Wpf;
using AForge.Math;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public int NumOfPoints = 1024;
        double XStart = -0.02;
        double XEnd = 0.02;
        double[] x = null;
        double[] x_w = null;
        double amplitude;
        double sigma_G, sigma_K, omega_G;
        double omega_K { get; set; }
        Graphics graphics { get; set; }
        Graphics mirror_graph { get; set; }
        Rectangle moving_length { get; set; }
        Rectangle sample { get; set; }
        Rectangle f_line { get; set; }
        Rectangle f_s_line { get; set; }
        Rectangle s_line { get; set; }
        System.Windows.Forms.DataVisualization.Charting.Series ser { get; set; }
        long tic { get; set; }
        Complex[] Y_c = null;
        double koef { get; set; }
        Complex[] G = null;
        Complex[] G_K = null;
        Complex[] K = null;

        // приводим к нормальному виду
        // здесь выполняется все кроме пересоздания массива при
        // изменении числа точек и изменения параметров
        // измение массива х возлагатся на метод, в котором изменяеются его параметры
        void Initialize_Empty()
        {
            G = new Complex[NumOfPoints];
            Y_c = new Complex[NumOfPoints];
            x_w = ArrayBuilder.CreateVector(
                0,
                1.0 / ((XEnd - XStart) / NumOfPoints), 
                NumOfPoints);
            for (int i = 0; i < NumOfPoints; i++)
            {
                G[i] = new Complex(Functions.func_gauss(x_w[i], sigma_G, omega_G), 0);
            }
            for (int i = 0; i < NumOfPoints; i++)
            {
                var mag = G[i].Magnitude;
                Y_c[i] = new Complex(mag * mag, 0);
            }
            Functions.FastDFT(Y_c, 1);
            Complex add_k = new Complex(Y_c[0].Re, 0);
            for (int i = 0; i < NumOfPoints; i++)
            {
                Y_c[i] += add_k;
            }
            Functions.FlipFlop(Y_c);
            koef = Y_c.Max(t => t.Re);
        }
        void Initialize_Filled()
        {
            x_w = ArrayBuilder.CreateVector(
                0,
                1.0 / ((XEnd - XStart) / NumOfPoints),
                NumOfPoints);
            K = new Complex[NumOfPoints];
            G_K = new Complex[NumOfPoints];
            for (int i = 0; i < NumOfPoints; i++)
            {
                K[i] = new Complex(1 - amplitude * Functions.func_gauss(x_w[i], sigma_K, omega_K), 0);
            }
            for (int i = 0; i < NumOfPoints; i++)
            {
                G_K[i] = Complex.Multiply(K[i], G[i]);
            }
            for (int i = 0; i < NumOfPoints; i++)
            {
                var mag = G_K[i].Magnitude;
                Y_c[i] = new Complex(mag * mag, 0);
            }
            Functions.FastDFT(Y_c, 1);
            Complex add_k = new Complex(Y_c[0].Re, 0);
            for (int i = 0; i < NumOfPoints; i++)
            {
                Y_c[i] += add_k;
            }
            Functions.FlipFlop(Y_c);
            koef = Y_c.Max(t => t.Re);
        }

        
        public Form1()
        {
            InitializeComponent();

            chart1.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Bright;
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "{F0}";
            chart1.ChartAreas[0].AxisX.Title = "волновое число(см -1)";
            chart1.ChartAreas[0].AxisX.TitleFont = new Font(chart1.ChartAreas[0].AxisX.TitleFont.Name, 14,
                chart1.ChartAreas[0].AxisX.TitleFont.Style, chart1.ChartAreas[0].AxisX.TitleFont.Unit);

            chart2.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Bright;
            chart2.ChartAreas[0].AxisX.LabelStyle.Format = "{F0}";
            chart2.ChartAreas[0].AxisX.Title = "волновое число(см -1)";
            chart2.ChartAreas[0].AxisX.TitleFont = new Font(chart2.ChartAreas[0].AxisX.TitleFont.Name, 14,
                chart2.ChartAreas[0].AxisX.TitleFont.Style, chart2.ChartAreas[0].AxisX.TitleFont.Unit);

            chart3.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Bright;
            chart3.ChartAreas[0].AxisX.LabelStyle.Format = "{F1}";
            chart3.ChartAreas[0].AxisX.Title = "мкм";
            chart3.ChartAreas[0].AxisX.TitleFont = new Font(chart3.ChartAreas[0].AxisX.TitleFont.Name, 14,
                chart3.ChartAreas[0].AxisX.TitleFont.Style, chart3.ChartAreas[0].AxisX.TitleFont.Unit);

            amplitude = Convert.ToDouble(textBox1.Text);
            sigma_G = Convert.ToDouble(textBox5.Text) / 6;
            sigma_K = Convert.ToDouble(textBox6.Text) / 6;
            omega_K = Convert.ToDouble(textBox8.Text);
            omega_G = Convert.ToDouble(textBox7.Text);
            x = ArrayBuilder.CreateVector(XStart, XEnd, NumOfPoints);
            graphics = tableLayoutPanel3.CreateGraphics();
            button1.Enabled = false;
            button4.Enabled = false;
            button3.Enabled = false;
            button6.Enabled = false;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            chart2.Titles[0].Text = "Спектр источника";
            chart2.Titles[0].Visible = true;
            chart2.Series.Clear();
            Functions.complex_re_paint(chart2, x_w, G, 1, sigma_G, omega_G, "G");
            button1.Enabled = false;
            button6.Enabled = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // дальше проверка длинны это проверка того что ты не удалили все из текст бокса
        // а проверка приведения, это проверка того что в текст боксе число
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            double buf;
            if (textBox1.Text.Length == 0) return;
            else if (!double.TryParse(textBox1.Text, out buf)) return;
            amplitude = buf;
            if (amplitude < 0)
            {
                textBox1.Text = 0.ToString();
                amplitude = 0;
            }
            if (amplitude > 1)
            {
                textBox1.Text = 1.ToString();
                amplitude = 1;
            }
            button1.Enabled = false;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Graphics graphics = tableLayoutPanel3.CreateGraphics();
            chart2.Titles[0].Visible = false;
            chart1.Series.Clear();
            chart2.Series.Clear();
            button1.Enabled = false;
            SolidBrush smoke_brush = new SolidBrush(Color.WhiteSmoke);
            if (!moving_length.IsEmpty)
            {
                graphics.FillRectangle(smoke_brush, moving_length);
            }
            if (!sample.IsEmpty)
            {
                graphics.FillRectangle(smoke_brush, sample);
                sample = new Rectangle();
                Pen red_pen = new Pen(Color.Red, 3);
                graphics.DrawRectangle(red_pen, f_line);
            }
            SolidBrush red_brush = new SolidBrush(Color.Red);
            int base_x = tableLayoutPanel3.Width / 9; // единицы измерения длинны
            int base_y = tableLayoutPanel3.Height / 9; // единицы измерения длинны
            moving_length = new Rectangle(8 * base_x, 4 * base_y, base_x / 5, base_y);
            s_line = new Rectangle((int)(base_x * 21 / 10.0), (int)(base_y * 4.5), moving_length.Left - (int)(base_x * 21 / 10.0), 1);
            graphics.FillRectangle(red_brush, moving_length);
            chart3.Series.Clear();
            ser = chart3.Series.Add("New plot");
            chart3.ChartAreas[0].AxisX.Maximum = XEnd * 1000;
            chart3.ChartAreas[0].AxisX.Minimum = XStart * 1000;
            chart3.ChartAreas[0].AxisY.Maximum = 1.2;
            chart3.ChartAreas[0].AxisY.Minimum = 0;
            ser.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            ser.BorderWidth = 2;
            Initialize_Empty();
            tic = 0;
            timer1.Interval = Math.Max(1, (int)(1500.0 / NumOfPoints));
            timer1.Enabled = true;
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            double buf;
            if (textBox7.Text.Length == 0) return;
            else if (!double.TryParse(textBox7.Text, out buf)) return;
            if (buf <= 1000)
            {
                textBox7.Text = (1000).ToString();
                buf = 1000;
            } else if(buf > 3500)
            {
                textBox7.Text = (3500).ToString();
                buf = 3500;
            }
            omega_G = buf;
            button1.Enabled = false;
        }

        private void tic_graph()
        {
            int base_x = tableLayoutPanel3.Width / 9; // единицы измерения длинны
            int base_y = tableLayoutPanel3.Height / 9; // единицы измерения длинны
            Graphics graphics = tableLayoutPanel3.CreateGraphics();
            mirror_graph = tableLayoutPanel3.CreateGraphics();
            mirror_graph.TranslateTransform((int)(base_x * 4.5), (int)(base_y * 4.5));
            mirror_graph.RotateTransform(45);
            DoubleBuffered = true;
            SolidBrush white_brush = new SolidBrush(Color.WhiteSmoke);
            SolidBrush red_brush = new SolidBrush(Color.Red);
            SolidBrush black_brush = new SolidBrush(Color.Black);
            SolidBrush green_brush = new SolidBrush(Color.Red);
            SolidBrush yellow_brush = new SolidBrush(Color.LightGray);
            Pen green_pen = new Pen(Color.Red, 3);

            graphics.FillRectangle(white_brush, moving_length);
            graphics.FillRectangle(white_brush, s_line);
            moving_length = new Rectangle(tableLayoutPanel3.Width * 8 / 9 - (int)(2.3 * tableLayoutPanel3.Width / 9 * tic / NumOfPoints)
                , moving_length.Top, moving_length.Width, moving_length.Height);
            s_line = new Rectangle((int)(base_x * 4.5), (int)(base_y * 4.5) - 3, moving_length.Left - (int)(base_x * 4.5) - 2, 7);

            ser.Points.AddXY(x[tic] * 1000, Y_c[tic].Re / koef);

            graphics.FillRectangle(red_brush, moving_length);
            graphics.DrawRectangle(green_pen, s_line);
            mirror_graph.FillRectangle(black_brush, new Rectangle(-base_x / 10, (int)(-base_y / 1.5), base_x / 5, (int)(base_y * 1.5)));

            if (!sample.IsEmpty)
            {
                graphics.FillRectangle(yellow_brush, sample);
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            tic_graph();
            SolidBrush white_brush = new SolidBrush(Color.WhiteSmoke);

            if (NumOfPoints > 8000)
                tic += 16;
            else if (NumOfPoints > 4000)
                tic += 8;
            else if (NumOfPoints > 2000)
                tic += 4;
            else if (NumOfPoints > 1000)
                tic += 2;
            else
                tic++;
            if (tic >= NumOfPoints)
            {
                timer1.Enabled = false;
                if (button6.Enabled)
                {
                    button6.Enabled = false;
                    button4.Enabled = true;
                    //clear_rays();
                }
                else
                {
                    button1.Enabled = true;
                    //clear_rays();
                }
            }
        }
    
        private void textBox5_TextChanged_1(object sender, EventArgs e)
        {
            double buf;
            if (textBox5.Text.Length == 0) return;
            else if (!double.TryParse(textBox5.Text, out buf)) return;
            if (buf <= 100)
            {
                textBox5.Text = (100).ToString();
                buf = 100;
            }
            else if (buf > 6000)
            {
                textBox5.Text = (6000).ToString();
                buf = 6000;
            }
            sigma_G = buf / 6;
            button1.Enabled = false;
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            double buf;
            if (textBox6.Text.Length == 0) return;
            else if (!double.TryParse(textBox6.Text, out buf)) return;
            if (buf <= 100)
            {
                textBox6.Text = (100).ToString();
                buf = 100;
            }
            else if (buf > 6000)
            {
                textBox6.Text = (6000).ToString();
                buf = 6000;
            }
            sigma_K = buf / 6;
            button1.Enabled = false;
        }

        private void textBox8_TextChanged_2(object sender, EventArgs e)
        {
            double buf;
            if (textBox8.Text.Length == 0) return;
            else if (!double.TryParse(textBox8.Text, out buf)) return;
            if (buf <= 1000)
            {
                textBox8.Text = (1000).ToString();
                buf = 1000;
            }
            else if (buf > 3500)
            {
                textBox8.Text = (3500).ToString();
                buf = 3500;
            }
            omega_K = buf;
            button1.Enabled = false;
        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {
            DoubleBuffered = true;
            Graphics graphics = e.Graphics;
            SolidBrush black_brush = new SolidBrush(Color.Black);
            SolidBrush blue_brush = new SolidBrush(Color.Blue);
            SolidBrush red_brush = new SolidBrush(Color.Red);
            Pen green_pen = new Pen(Color.DarkGreen, 3);
            Pen red_pen = new Pen(Color.Red, 7);
            Pen r_pen = new Pen(Color.Red, 3);

            red_pen.StartCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

            int base_x = tableLayoutPanel3.Width / 9; // единицы измерения длинны
            int base_y = tableLayoutPanel3.Height / 9; // единицы измерения длинны

            Point p1 = new Point(4 * base_x, 8 * base_y - base_y / 3);
            Point p2 = new Point(5 * base_x, 8 * base_y - base_y / 3);
            Point p3 = new Point((int)(4.5 * base_x), (int)(8.2 * base_y));
            Point[] points = new Point[] { p1, p2, p3 };
            graphics.FillPolygon(blue_brush, points);

            Point l1 = new Point((int)(4 * base_x), (int)(2.4 * base_y));
            Point l2 = new Point((int)(4 * base_x), 3 * base_y);
            Point l3 = new Point((int)(5 * base_x), (int)(2.4 * base_y));
            Point l4 = new Point((int)(5 * base_x), (int)(3 * base_y));
            Point l11 = new Point((int)(3.8 * base_x), (int)(6 * base_y));
            Point l21 = new Point((int)(3.8 * base_x), (int)(6.6 * base_y));
            Point l31 = new Point((int)(5.2 * base_x), (int)(6 * base_y));
            Point l41 = new Point((int)(5.2 * base_x), (int)(6.6 * base_y));
            Point l12 = new Point((int)(2.9 * base_x), 5 * base_y);
            Point l22 = new Point((int)(3.5 * base_x), 5 * base_y);
            Point l13 = new Point((int)(5 * base_x), 5 * base_y);
            Point l23 = new Point((int)(5.6 * base_x), 5 * base_y);
            Point l33 = new Point((int)(5 * base_x), (int)(4 * base_y));
            Point l43 = new Point((int)(5.6 * base_x), (int)(4 * base_y));




            graphics.DrawLine(red_pen, l1, l2);
            graphics.DrawLine(red_pen, l4, l3);
            graphics.DrawLine(red_pen, l21, l11);
            graphics.DrawLine(red_pen, l41, l31);
            graphics.DrawLine(red_pen, l22, l12);
            graphics.DrawLine(red_pen, l13, l23);
            graphics.DrawLine(red_pen, l43, l33);

            f_line = new Rectangle((int)(base_x * 4.5) - 3, (int)(base_y * 6 / 5.0) + 1, 7, (int)(8 * base_y - base_y / 3) - (int)(base_y * 6 / 5.0) - 3);
            f_s_line = new Rectangle(base_x * 21 / 10, (int)(base_y * 4.5), (int)(base_x * 4.5) - base_x * 21 / 10, 3);
            moving_length = new Rectangle(8 * base_x, 4 * base_y, base_x / 5, base_y);
            s_line = new Rectangle((int)(base_x * 4.5), (int)(base_y * 4.5) - 3, moving_length.Left - (int)(base_x * 4.5) - 2, 7);
            graphics.DrawRectangle(r_pen, f_line);
            graphics.FillRectangle(red_brush, f_s_line);
            graphics.DrawRectangle(r_pen, s_line);


            graphics.DrawRectangle(green_pen, new Rectangle(1, base_y - 4, base_x * 9 - 2, base_y * 8 + 6));
            graphics.FillRectangle(black_brush, new Rectangle(4 * base_x, base_y, base_x, base_y / 5));
            graphics.FillRectangle(blue_brush, new Rectangle(4 * base_x, 8 * base_y, base_x, base_y / 5));
            graphics.FillRectangle(black_brush, new Rectangle(base_x, 4 * base_y, base_x, base_y));
            graphics.FillRectangle(red_brush, new Rectangle(2 * base_x, 4 * base_y + (int)(base_y * 0.4), base_x / 10, base_y / 5));
            graphics.FillRectangle(red_brush, new Rectangle(8 * base_x, 4 * base_y, base_x / 5, base_y));


            graphics.TranslateTransform((int)(base_x * 4.5), (int)(base_y * 4.5));
            graphics.RotateTransform(45);
            graphics.FillRectangle(black_brush, new Rectangle(-base_x / 10, (int)(-base_y / 1.5), base_x / 5, (int)(base_y * 1.5)));
        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            //button6.Enabled = false;
            Graphics graphics = tableLayoutPanel3.CreateGraphics();
            chart2.Titles[0].Visible = false;
            Functions.complex_re_paint(chart1, x_w, G, 1, sigma_G, omega_G, "G");
            SolidBrush smoke_brush = new SolidBrush(Color.WhiteSmoke);
            if (!moving_length.IsEmpty)
            {
                graphics.FillRectangle(smoke_brush, moving_length);
            }
            SolidBrush yellow_brush = new SolidBrush(Color.LightGray);
            int base_x = tableLayoutPanel3.Width / 9; // единицы измерения длинны
            int base_y = tableLayoutPanel3.Height / 9; // единицы измерения длинны
            sample = new Rectangle(4 * base_x, 6 * base_y, base_x, base_y);
            graphics.FillRectangle(yellow_brush, sample);
            chart2.Series.Clear();
            chart3.Series.Clear();
            ser = chart3.Series.Add("Acorr");
            ser.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            ser.BorderWidth = 2;
            Initialize_Filled();
            tic = 0;
            timer1.Enabled = true;            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            chart2.Titles[0].Text = "Образец + источник";
            chart2.Titles[0].Visible = true;
            Functions.complex_re_paint(chart2, x_w, G_K, 1, sigma_G, omega_G, "GK");
            button4.Enabled = false;
            button3.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            chart2.Titles[0].Text = "Спектр пропускания образца";
            Functions.complex_re_paint(chart1, x_w, G_K, 1, sigma_G, omega_G, "GK");
            chart2.Series.Clear();
            Functions.complex_re_paint(chart2, x_w, K, 1, sigma_K, omega_K, "K");
            button3.Enabled = false;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        
    }
}
