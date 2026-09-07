using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace MarcaoBoostWpf
{
    static class AppConfig
    {
        public const string ApiUrl = "https://marcao-boost-api.marcao-boost.workers.dev";
    }

    sealed class ApiResult
    {
        public bool Ok;
        public int Status;
        public Dictionary<string, object> Data;
        public string Error;
    }

    static class Api
    {
        static readonly HttpClient Client = new HttpClient { BaseAddress = new Uri(AppConfig.ApiUrl), Timeout = TimeSpan.FromSeconds(20) };
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        public static string Token;
        static Api() { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; }

        public static async Task<ApiResult> Get(string path)
        {
            return await Send(HttpMethod.Get, path, null, null);
        }
        public static async Task<ApiResult> Post(string path, object value, string bootstrap)
        {
            return await Send(HttpMethod.Post, path, value, bootstrap);
        }
        public static async Task<ApiResult> Patch(string path, object value)
        {
            return await Send(new HttpMethod("PATCH"), path, value, null);
        }
        static async Task<ApiResult> Send(HttpMethod method, string path, object value, string bootstrap)
        {
            try
            {
                using (HttpRequestMessage req = new HttpRequestMessage(method, path))
                {
                    if (!string.IsNullOrEmpty(Token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                    if (!string.IsNullOrEmpty(bootstrap)) req.Headers.Add("x-bootstrap-key", bootstrap);
                    if (value != null) req.Content = new StringContent(Json.Serialize(value), Encoding.UTF8, "application/json");
                    using (HttpResponseMessage res = await Client.SendAsync(req))
                    {
                        string raw = await res.Content.ReadAsStringAsync(); Dictionary<string, object> data = null;
                        try { data = Json.Deserialize<Dictionary<string, object>>(raw); } catch { }
                        string error = data != null && data.ContainsKey("error") ? Convert.ToString(data["error"]) : (res.IsSuccessStatusCode ? null : "O serviço não respondeu corretamente.");
                        return new ApiResult { Ok = res.IsSuccessStatusCode, Status = (int)res.StatusCode, Data = data, Error = error };
                    }
                }
            }
            catch (Exception ex) { return new ApiResult { Ok = false, Error = "Não foi possível conectar ao serviço. Verifique sua internet.\n" + ex.Message }; }
        }
    }

    static class Ui
    {
        public static readonly Color Bg = Color.FromRgb(7, 13, 23), Nav = Color.FromRgb(9, 18, 31), Surface = Color.FromRgb(16, 27, 44), Surface2 = Color.FromRgb(21, 35, 55), Text = Color.FromRgb(240, 247, 255), Muted = Color.FromRgb(145, 163, 188), Lime = Color.FromRgb(156, 255, 67), Blue = Color.FromRgb(82, 145, 255), Amber = Color.FromRgb(255, 190, 78), Red = Color.FromRgb(255, 95, 110);
        public static SolidColorBrush Brush(Color c) { return new SolidColorBrush(c); }
        public static TextBlock TextBlock(string text, double size, Color color, FontWeight weight)
        {
            return new TextBlock { Text = text, FontFamily = new FontFamily("Segoe UI"), FontSize = size, Foreground = Brush(color), FontWeight = weight, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        }
        public static Image BrandImage(double size)
        {
            Image image = new Image { Width = size, Height = size, Stretch = Stretch.Uniform };
            try { using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream("MarcaoBoostIcon.png")) { if (s != null) { BitmapImage b = new BitmapImage(); b.BeginInit(); b.CacheOption = BitmapCacheOption.OnLoad; b.StreamSource = s; b.EndInit(); image.Source = b; } } } catch { }
            return image;
        }
        public static ImageSource WindowIcon()
        {
            Image i = BrandImage(32); return i.Source;
        }
        public static ControlTemplate RoundedButtonTemplate(double radius)
        {
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center); presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetBinding(ContentPresenter.ContentProperty, new Binding("Content") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) }); border.AppendChild(presenter);
            return new ControlTemplate(typeof(Button)) { VisualTree = border };
        }
        public static Button Button(string text, bool accent, double width)
        {
            Color rest = accent ? Lime : Surface2, over = accent ? Color.FromRgb(190, 255, 126) : Color.FromRgb(34, 54, 80);
            SolidColorBrush bg = Brush(rest);
            Button b = new Button { Content = text, Width = width, Height = 42, Background = bg, Foreground = Brush(accent ? Bg : Text), BorderBrush = accent ? Brushes.Transparent : Brush(Color.FromRgb(48, 66, 90)), BorderThickness = new Thickness(1), FontFamily = new FontFamily("Segoe UI Semibold"), FontSize = 13, Cursor = Cursors.Hand, Template = RoundedButtonTemplate(21) };
            b.MouseEnter += delegate { bg.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(over, TimeSpan.FromMilliseconds(140))); };
            b.MouseLeave += delegate { bg.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(rest, TimeSpan.FromMilliseconds(180))); };
            b.PreviewMouseDown += delegate { b.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(.72, TimeSpan.FromMilliseconds(70))); };
            b.PreviewMouseUp += delegate { b.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(120))); };
            return b;
        }
        public static Border Field(Control input, string label)
        {
            StackPanel stack = new StackPanel(); stack.Children.Add(TextBlock(label, 12, Muted, FontWeights.SemiBold)); input.Margin = new Thickness(0, 7, 0, 0); input.Height = 28; input.FontFamily = new FontFamily("Segoe UI"); input.FontSize = 14; input.Foreground = Brush(Text); input.Background = Brushes.Transparent; input.BorderThickness = new Thickness(0); stack.Children.Add(input);
            return new Border { Background = Brush(Surface2), BorderBrush = Brush(Color.FromRgb(47, 65, 89)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16), Padding = new Thickness(16, 10, 16, 8), Margin = new Thickness(0, 0, 0, 12), Child = stack };
        }
        public static Border Card(UIElement child, Thickness margin)
        {
            return new Border { Background = Brush(Surface), BorderBrush = Brush(Color.FromRgb(38, 54, 75)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(22), Padding = new Thickness(22), Margin = margin, Child = child };
        }
        public static void Enter(UIElement element)
        {
            TranslateTransform move = new TranslateTransform(22, 0); element.RenderTransform = move; element.Opacity = 0;
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(22, 0, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        }
        public static void ConfigureWindow(Window window, string title, double width, double height)
        {
            window.Title = title; window.Width = width; window.Height = height; window.MinWidth = Math.Min(980, width); window.MinHeight = Math.Min(620, height); window.WindowStartupLocation = WindowStartupLocation.CenterScreen; window.Background = Brush(Bg); window.Foreground = Brush(Text); window.FontFamily = new FontFamily("Segoe UI"); window.Icon = WindowIcon();
            window.Closing += delegate { if (Application.Current != null && Application.Current.MainWindow == window) Application.Current.Shutdown(); };
            window.Closed += delegate { if (Application.Current != null && (Application.Current.MainWindow == null || Application.Current.MainWindow == window)) Application.Current.Shutdown(); };
        }
        public static UIElement Chrome(Window window, UIElement body)
        {
            window.WindowStyle = WindowStyle.None; window.ResizeMode = ResizeMode.CanResize;
            System.Windows.Shell.WindowChrome.SetWindowChrome(window, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(7), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0) });
            Grid shell = new Grid { Background = Brush(Bg) }; shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) }); shell.RowDefinitions.Add(new RowDefinition());
            Grid bar = new Grid { Background = Brush(Nav) }; bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) }); bar.ColumnDefinitions.Add(new ColumnDefinition()); bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Image icon = BrandImage(22); icon.Margin = new Thickness(13, 8, 9, 8); bar.Children.Add(icon);
            Border drag = new Border { Background = Brushes.Transparent }; TextBlock caption = TextBlock(window.Title, 13, Text, FontWeights.SemiBold); caption.Margin = new Thickness(0, 0, 0, 1); drag.Child = caption; Grid.SetColumn(drag, 1); bar.Children.Add(drag);
            drag.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; else window.DragMove(); };
            StackPanel controls = new StackPanel { Orientation = Orientation.Horizontal }; Button min = TitleButton("—"), max = TitleButton("□"), close = TitleButton("×"); close.Foreground = Brush(Color.FromRgb(255, 165, 174));
            min.Click += delegate { window.WindowState = WindowState.Minimized; }; max.Click += delegate { window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }; close.Click += delegate { window.Close(); };
            controls.Children.Add(min); controls.Children.Add(max); controls.Children.Add(close); Grid.SetColumn(controls, 2); bar.Children.Add(controls); shell.Children.Add(bar); Grid.SetRow(body, 1); shell.Children.Add(body); return shell;
        }
        static Button TitleButton(string text)
        {
            Button button = new Button { Content = text, Width = 46, Height = 32, Margin = new Thickness(0, 4, 4, 4), Foreground = Brush(Text), Background = Brushes.Transparent, BorderThickness = new Thickness(0), FontFamily = new FontFamily("Segoe UI Semibold"), FontSize = 15, Cursor = Cursors.Hand, Template = RoundedButtonTemplate(9) };
            button.MouseEnter += delegate { button.Background = Brush(Surface2); }; button.MouseLeave += delegate { button.Background = Brushes.Transparent; }; return button;
        }
        public static ScrollViewer InvisibleScroll(UIElement content)
        {
            return new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, PanningMode = PanningMode.VerticalOnly };
        }
        public static void SuccessSound() { try { System.Media.SystemSounds.Asterisk.Play(); } catch { } }
        public static void ErrorSound() { try { System.Media.SystemSounds.Hand.Play(); } catch { } }
        public static string Get(Dictionary<string, object> d, string key) { return d != null && d.ContainsKey(key) ? Convert.ToString(d[key]) : ""; }
    }
}
