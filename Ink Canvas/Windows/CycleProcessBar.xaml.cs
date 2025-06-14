using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ink_Canvas.ProcessBars
{
    /// <summary>
    /// Interaction logic for CycleProcessBar1.xaml
    /// </summary>
    public partial class CycleProcessBar : UserControl
    {
        public CycleProcessBar()
        {
            InitializeComponent();
            IsPaused = false;
        }

        public bool IsPaused
        {
            set { SetRingColor(value); }
        }

        private void SetRingColor(bool isPaused)
        {
            if (isPaused)
            {
                myCycleProcessBar.Stroke = new SolidColorBrush(StringToColor("#FF1A71C8"));
            }
            else
            {
                myCycleProcessBar.Stroke = new SolidColorBrush(StringToColor("#FF0067C1"));
            }
        }

        private Color StringToColor(string colorStr)
        {
            Byte[] argb = new Byte[4];
            for (int i = 0; i < 4; i++)
            {
                char[] charArray = colorStr.Substring(i * 2 + 1, 2).ToCharArray();
                //string str = "11";
                Byte b1 = toByte(charArray[0]);
                Byte b2 = toByte(charArray[1]);
                argb[i] = (Byte)(b2 | (b1 << 4));
            }
            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);//#FFFFFFFF
        }

        private static byte toByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }

        public double CurrentValue
        {
            set { SetValue(value); }
        }

        public static readonly DependencyProperty PercentValueProperty =
            DependencyProperty.Register("PercentValue", typeof(double), typeof(CycleProcessBar), new PropertyMetadata(0.0, OnPercentValueChanged));

        public double PercentValue
        {
            get { return (double)GetValue(PercentValueProperty); }
            set { SetValue(PercentValueProperty, value); }
        }

        private static void OnPercentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CycleProcessBar)d;
            control.UpdateProgress();
        }

        private void UpdateProgress()
        {
            double percentValue = PercentValue;
            if (percentValue > 1) percentValue = 1;
            if (percentValue < 0) percentValue = 0;

            /******************************************
            Square matrix side length is 34, half length is 17
            Ring radius is 14, so 3 pixels from the border
            Ring stroke is 3 pixels
            ******************************************/
            double angel = percentValue * 360; // angle

            double radius = 14; // ring radius

            // starting point
            double startX = 17 + radius * Math.Cos(0);
            double startY = 17 + radius * Math.Sin(0);

            // ending point
            double endX = 17 + radius * Math.Cos(angel * Math.PI / 180);
            double endY = 17 + radius * Math.Sin(angel * Math.PI / 180);

            // number display
            TextBlockPercent.Text = (percentValue * 100).ToString("F0") + "%";

            /***********************************************
            * 整个环形进度条使用Path来绘制，采用三角函数来计算
            * 环形根据角度来分别绘制，以90度划分，方便计算比例
            ***********************************************/

            bool isLagreCircle = false; //是否优势弧，即大于180度的弧形

            //小于90度
            if (angel <= 90)
            {
                /*****************
                          *
                          *   *
                          * * ra
                   * * * * * * * * *
                          *
                          *
                          *
                ******************/
                double ra = (90 - angel) * Math.PI / 180; // radians
                endX = 17 + Math.Cos(ra) * radius; // cosine x-coordinate
                endY = 17 + radius - Math.Sin(ra) * radius; // sine y-coordinate
            }

            else if (angel <= 180)
            {
                /*****************
                          *
                          *  
                          * 
                   * * * * * * * * *
                          * * ra
                          *  *
                          *   *
                ******************/

                double ra = (angel - 90) * Math.PI / 180; // radians
                endX = 17 + Math.Cos(ra) * radius; // cosine x-coordinate
                endY = 17 + radius + Math.Sin(ra) * radius; // sine y-coordinate
            }

            else if (angel <= 270)
            {
                /*****************
                          *
                          *  
                          * 
                   * * * * * * * * *
                        * *
                       *ra*
                      *   *
                ******************/
                isLagreCircle = true; // major arc
                double ra = (angel - 180) * Math.PI / 180;
                endX = 17 - Math.Sin(ra) * radius;
                endY = 17 + radius + Math.Cos(ra) * radius;
            }

            else if (angel < 360)
            {
                /*****************
                      *   *
                       *  *  
                     ra * * 
                   * * * * * * * * *
                          *
                          *
                          *
                ******************/
                isLagreCircle = true; // major arc
                double ra = (angel - 270) * Math.PI / 180;
                endX = 17 - Math.Cos(ra) * radius;
                endY = 17 + radius - Math.Sin(ra) * radius;
            }
            else
            {
                isLagreCircle = true; // major arc
                endX = 17 - 0.001; // avoid overlapping with start point to prevent non-circular drawing
                endY = 17;
            }

            Point arcEndPt = new Point(endX, endY); // end point
            Size arcSize = new Size(radius, radius);
            SweepDirection direction = SweepDirection.Clockwise; // clockwise arc
            // arc
            ArcSegment arcsegment = new ArcSegment(arcEndPt, arcSize, 0, isLagreCircle, direction, true);

            // shape collection
            PathSegmentCollection pathsegmentCollection = new PathSegmentCollection();
            pathsegmentCollection.Add(arcsegment);

            // path description
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(startX, startY); // start point
            pathFigure.Segments = pathsegmentCollection;

            // path description collection
            PathFigureCollection pathFigureCollection = new PathFigureCollection();
            pathFigureCollection.Add(pathFigure);

            // complex shape
            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = pathFigureCollection;

            //Data assignment
            myCycleProcessBar.Data = pathGeometry;
            // reach 100% then close the whole
            if (angel == 360)
            {
                myCycleProcessBar.Data = Geometry.Parse(myCycleProcessBar.Data.ToString() + " z");
            }
        }
    }
}