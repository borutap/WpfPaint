using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool drawRectangle = false;
        private bool drawEllipse = false;
        private Shape myShape = new Rectangle();
        private Point drawStart;
        private Random generator = new Random();
        private DropShadowEffect dropShadowEffect;
        private string language;
        private readonly ResourceManager resManager;
        private CultureInfo currentCulture;
        private BitmapImage imageGB;
        private BitmapImage imagePL;
        private List<Shape> selected;
        private Shape lastSelected;

        public struct MyBrush
        {
            public Brush brush { get; set; }
            public Brush textBrush { get; set; }
            public string name { get; set; }
        }

        public List<MyBrush> brushes { get; set; }
        public string Test { get; set; } = "dupa";
        public object ShapeWidth
        {
            get
            {
                if (lastSelected != null)
                    return (int)lastSelected.Width;
                return string.Empty;
            }
            set
            {
                string str = value as string;
                if (str == "")
                    return;
                lastSelected.Width = double.Parse(str);
            }
        }

        public object ShapeHeight
        {
            get
            {
                if (lastSelected != null)
                    return (int)lastSelected.Height;
                return string.Empty;
            }
            set
            {
                string str = value as string;
                if (str == "")
                    return;
                lastSelected.Height = double.Parse(str);
            }
        }
        private Shape LastSelected
        {
            get
            {
                return lastSelected;
            }
            set
            {
                lastSelected = value;
                if (lastSelected == null)
                {
                    disableControls();
                    return;
                }
                width.Text = lastSelected.Width.ToString();
                height.Text = lastSelected.Height.ToString();
                width.IsEnabled = true;
                height.IsEnabled = true;
                randomColors.IsEnabled = true;
                delete.IsEnabled = true;
                comboBox.IsEnabled = true;
                SelectInCombo(lastSelected.Fill);
                slider.IsEnabled = true;
                slider.Value = (double)lastSelected.Tag;
            }
        }

        private void disableControls()
        {
            width.Text = "";
            height.Text = "";
            width.IsEnabled = false;
            height.IsEnabled = false;
            randomColors.IsEnabled = false;
            delete.IsEnabled = false;
            comboBox.SelectedItem = null;
            comboBox.IsEnabled = false;
            slider.Value = 0;
            slider.IsEnabled = false;
        }

        public MainWindow()
        {
            InitializeComponent();
            language = "en-GB";
            resManager = Resource1.ResourceManager;
            currentCulture = new CultureInfo(language);
            TranslationSource.Instance.CurrentCulture = currentCulture;
            imagePL = new BitmapImage(new Uri("img/poland.png", UriKind.Relative));
            imageGB = new BitmapImage(new Uri("img/united-kingdom.png", UriKind.Relative));
            flag.Source = imageGB;
            selected = new List<Shape>();
            brushes = GetBrushes();
            DataContext = this;
        }

        private Brush PickRandomBrush(Random rnd) // https://stackoverflow.com/a/27549801
        {
            Brush result = Brushes.Transparent;
            Type brushesType = typeof(Brushes);
            PropertyInfo[] properties = brushesType.GetProperties();
            int random = rnd.Next(properties.Length);
            result = (Brush)properties[random].GetValue(null, null);
            return result;
        }

        private List<MyBrush> GetBrushes()
        {
            var ret = new List<MyBrush>();
            foreach (var brush in typeof(Brushes)
                    .GetProperties(BindingFlags.Static | BindingFlags.Public))
            {
                var currentBrush = brush.GetValue(null) as Brush;
                var color = ((SolidColorBrush)currentBrush).Color;
                double determine = color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
                var textColor = determine < 186 ? Brushes.White : Brushes.Black;
                var currentName = brush.Name;
                if (currentBrush != Brushes.Transparent)
                {

                    var el = new MyBrush()
                    {
                        brush = currentBrush,
                        textBrush = textColor,
                        name = Regex.Replace(currentName, "([a-z])([A-Z])", "$1 $2")
                    };
                    ret.Add(el);
                }
            }
            return ret;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Shape random;
            double canvasWidth = myCanvas.ActualWidth;
            double canvasHeight = myCanvas.ActualHeight;
            for (int i = 0; i < 4; i++)
            {
                int randInt = generator.Next(0, 2);
                int Width, Height;                
                random = randInt == 0 ? new Rectangle() : new Ellipse();
                Width = generator.Next(60, (int)canvasWidth / 2);
                Height = generator.Next(60, (int)canvasHeight / 2);
                random.Fill = PickRandomBrush(generator);
                random.Width = Width;
                random.Height = Height;
                Canvas.SetLeft(random, generator.NextDouble() * (canvasWidth - Width));
                Canvas.SetTop(random, generator.NextDouble() * (canvasHeight - Height));
                AddEventHandlers(random);
                random.Tag = (double)0; // no glow
                myCanvas.Children.Add(random);
            }
            dropShadowEffect = new DropShadowEffect
            {
                ShadowDepth = 0,
                Color = Colors.White,
                Opacity = 1,
                BlurRadius = 50,
                Direction = 270
            };
            disableControls();
        }

        private void AddEventHandlers(Shape shape)
        {
            shape.MouseEnter += CommonMouseEnter;
            shape.MouseLeave += CommonMouseLeave;
            shape.MouseRightButtonDown += CommonMouseRightButtonDown;
            shape.MouseLeftButtonDown += CommonMouseLeftButtonDown;
            shape.MouseUp += CommonMouseUp;
            shape.MouseMove += CommonMouseMove;
        }

        private void CommonMouseLeftButtonDown(object s, MouseEventArgs args)
        {
            if (drawRectangle || drawEllipse)
                return;
            var shape = (Shape)s;
            if (!selected.Contains(shape))
            {
                Canvas.SetZIndex(shape, 1);
                shape.Effect = dropShadowEffect;
                Deselect();
                selected.Add(shape);
                LastSelected = shape;
            }
            drawStart = args.GetPosition(shape);
            Mouse.OverrideCursor = Cursors.ScrollAll;
            shape.CaptureMouse();
        }

        private void CommonMouseUp(object s, MouseEventArgs args)
        {
            var element = (UIElement)s;
            element.ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        private void CommonMouseMove(object sender, MouseEventArgs args)
        {
            if (args.LeftButton == MouseButtonState.Pressed)
            {
                var p = args.GetPosition(myCanvas);
                var thisElement = (Shape)sender;
                var prevX = Canvas.GetLeft(thisElement);
                var prevY = Canvas.GetTop(thisElement);
                Canvas.SetLeft(thisElement, p.X - drawStart.X);
                Canvas.SetTop(thisElement, p.Y - drawStart.Y);
                var X = Canvas.GetLeft(thisElement);
                var Y = Canvas.GetTop(thisElement);
                foreach (var element in selected)
                {
                    if (element != thisElement)
                    {
                        Canvas.SetLeft(element, X - prevX + Canvas.GetLeft(element));
                        Canvas.SetTop(element, Y - prevY + Canvas.GetTop(element));
                    }
                }
            }
        }
        private void CommonMouseEnter(object s, MouseEventArgs args)
        {
            Mouse.OverrideCursor = Cursors.Hand;
        }

        private void CommonMouseLeave(object s, MouseEventArgs args)
        {
            if (drawRectangle || drawEllipse)
            {
                Mouse.OverrideCursor = Cursors.Cross;
                return;
            }
            Mouse.OverrideCursor = null;
        }

        private void myCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!drawRectangle && !drawEllipse)
                return;

            myCanvas.CaptureMouse();
            drawStart = e.GetPosition(myCanvas);

            myShape = drawRectangle ? new Rectangle() : new Ellipse();
            myShape.Fill = PickRandomBrush(generator);
            myShape.Tag = (double)0;

            myCanvas.Children.Add(myShape);
        }

        private void myCanvas_MouseMove(object sender, MouseEventArgs e) // https://stackoverflow.com/a/47408792
        {
            if (!drawRectangle && !drawEllipse)
                return;

            if (!myCanvas.IsMouseCaptured)
                return;

            Point p = e.GetPosition(myCanvas);

            double minX = Math.Min(p.X, drawStart.X);
            double minY = Math.Min(p.Y, drawStart.Y);
            double maxX = Math.Max(p.X, drawStart.X);
            double maxY = Math.Max(p.Y, drawStart.Y);

            Canvas.SetTop(myShape, minY);
            Canvas.SetLeft(myShape, minX);

            double height = maxY - minY;
            double width = maxX - minX;

            myShape.Height = Math.Abs(height);
            myShape.Width = Math.Abs(width);
        }

        private void CommonMouseRightButtonDown(object s, MouseEventArgs args)
        {
            if (myCanvas.IsMouseCaptured)
                myCanvas.ReleaseMouseCapture();
            Shape shape = s as Shape;
            if (!selected.Contains(shape))
            {
                Canvas.SetZIndex(shape, 1);
                shape.Effect = dropShadowEffect;
                selected.Add(shape);
                LastSelected = shape;
                return;
            }
            Canvas.SetZIndex(shape, 0);
            shape.Effect = null;
            selected.Remove(shape);
            LastSelected = selected.Count > 0 ? selected[selected.Count - 1] : null;
        }

        private void SelectInCombo(Brush brush)
        {
            int index = 0;
            int i = 0;
            foreach (var el in brushes)
            {
                if (el.brush == brush)
                    index = i;
                i++;
            }
            comboBox.SelectedIndex = index;
        }

        private void Deselect()
        {
            foreach (var shape in selected)
            {         
                Canvas.SetZIndex(shape, 0);
                shape.Effect = null;
            }
            selected.Clear();
            LastSelected = null;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            foreach (var el in selected)
                myCanvas.Children.Remove(el);

            LastSelected = null;
            selected.Clear();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            foreach (var el in selected)
                el.Fill = PickRandomBrush(generator);

            if (lastSelected != null)
                SelectInCombo(lastSelected.Fill);
        }

        private void Rectangle_Click(object sender, RoutedEventArgs e)
        {
            Deselect();
            Mouse.OverrideCursor = Cursors.Cross;
            drawRectangle = true;
        }

        private void Ellipse_Click(object sender, RoutedEventArgs e)
        {
            Deselect();
            Mouse.OverrideCursor = Cursors.Cross;
            drawEllipse = true;
        }        

        private void myCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            myCanvas.ReleaseMouseCapture();
            AddEventHandlers(myShape);
            Mouse.OverrideCursor = null;
            drawRectangle = false;
            drawEllipse = false;
            myShape = new Rectangle();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (language == "en-GB")
            {
                language = "pl-PL";
                flag.Source = imagePL;

            }
            else
            {
                language = "en-GB";
                flag.Source = imageGB;
            }
            currentCulture = new CultureInfo(language);
            TranslationSource.Instance.CurrentCulture = currentCulture;
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lastSelected == null)
                return;
            lastSelected.RenderTransformOrigin = new Point(0.5, 0.5);
            lastSelected.RenderTransform = new RotateTransform(e.NewValue);
            lastSelected.Tag = e.NewValue;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lastSelected == null)
                return;
            MyBrush val = (MyBrush)comboBox.SelectedItem;
            lastSelected.Fill = val.brush;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG file (*.png)|*.png";
            saveFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            if (saveFileDialog.ShowDialog() == true)
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)myCanvas.RenderSize.Width,
                    (int)myCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Default);
                rtb.Render(myCanvas);

                BitmapEncoder pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

                using var fs = File.OpenWrite(saveFileDialog.FileName);
                pngEncoder.Save(fs);
            }
        }
    }
}
