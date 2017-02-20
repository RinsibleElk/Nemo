using System.Windows;
using System.Windows.Controls;

namespace Nemo.Infra
{
    public static class WebBrowserHelper
    {
        public static readonly DependencyProperty BodyProperty =
            DependencyProperty.RegisterAttached("Body", typeof(string), typeof(WebBrowserHelper),
                new PropertyMetadata(OnBodyChanged));

        public static string GetBody(DependencyObject dependencyObject)
        {
            return (string) dependencyObject.GetValue(BodyProperty);
        }

        public static void SetBody(DependencyObject dependencyObject, string body)
        {
            dependencyObject.SetValue(BodyProperty, body);
        }

        private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var webBrowser = (WebBrowser) d;
            webBrowser.NavigateToString((string) e.NewValue);
        }

        public static readonly DependencyProperty ShadowHeightProperty =
            DependencyProperty.RegisterAttached(
                "ShadowHeight",
                typeof(string),
                typeof(WebBrowserHelper),
                new UIPropertyMetadata(null, ShadowHeightPropertyChanged));

        public static string GetShadowHeight(DependencyObject obj)
        {
            return (string)obj.GetValue(ShadowHeightProperty);
        }
        public static void SetShadowHeight(DependencyObject obj, string value)
        {
            obj.SetValue(ShadowHeightProperty, value);
        }
        public static void ShadowHeightPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            WebBrowser browser = obj as WebBrowser;
            double shadowHeight;
            string str = args.NewValue as string;
            if (browser != null && str != null && double.TryParse(str, out shadowHeight))
            {
                browser.Height = shadowHeight;
            }
        }

        public static readonly DependencyProperty ShadowWidthProperty =
            DependencyProperty.RegisterAttached(
                "ShadowWidth",
                typeof(string),
                typeof(WebBrowserHelper),
                new UIPropertyMetadata(null, ShadowWidthPropertyChanged));

        public static string GetShadowWidth(DependencyObject obj)
        {
            return (string)obj.GetValue(ShadowWidthProperty);
        }
        public static void SetShadowWidth(DependencyObject obj, string value)
        {
            obj.SetValue(ShadowWidthProperty, value);
        }
        public static void ShadowWidthPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            WebBrowser browser = obj as WebBrowser;
            double shadowWidth;
            string str = args.NewValue as string;
            if (browser != null && str != null && double.TryParse(str, out shadowWidth))
            {
                browser.Width = shadowWidth;
            }
        }
    }
}