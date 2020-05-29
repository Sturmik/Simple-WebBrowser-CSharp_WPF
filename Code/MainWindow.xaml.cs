using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleWeb
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> _favouritePages;
        private List<WebPage> _webPages;

        private ListBox _favList;

        public MainWindow()
        {
            _favouritePages = new Dictionary<string, string>();
            _webPages = new List<WebPage>();

            LoadFavList();

            try
            {
                FileStream toSave = new FileStream("FavList", FileMode.Open, FileAccess.Read, FileShare.Write);
                BinaryFormatter ser = new BinaryFormatter();
                _favouritePages = (Dictionary<string, string>)ser.Deserialize(toSave);
                toSave.Flush();
            } catch (Exception) { }

            InitializeComponent();
        }

        #region Methods

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _webPages.Add(new WebPage(webTabPages, _favouritePages));
            (webTabPages.Items[0] as TabItem).IsSelected = true;

            // Setting up list box with favourite web pages
            _favList = new ListBox();
            _favList.Background = new SolidColorBrush(Color.FromRgb(255, 210, 0));
            _favList.MouseDoubleClick += favList_MouseDoubleClick;
            _favList.FontSize = 14;
            _favList.Visibility = Visibility.Collapsed;
            mainDock.Children.Add(_favList);
        }

        private void buttonAddPage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _webPages.Add(new WebPage(webTabPages, _favouritePages));
            (webTabPages.Items[webTabPages.Items.Count - 2] as TabItem).IsSelected = true;
        }

        private void webTabPages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (webTabPages.SelectedIndex != -1)
                    _webPages[webTabPages.SelectedIndex].RefreshPage();
                if (webTabPages.Items.Count > 1)
                    (webTabPages.Items[webTabPages.Items.Count - 2] as TabItem).IsSelected = true;
            } catch (Exception) { }
        }

        private void buttonBrowser_Click(object sender, RoutedEventArgs e)
        {
            webTabPages.Visibility = Visibility.Visible;
            _favList.Visibility = Visibility.Collapsed;
        }

        private void buttonFavourites_Click(object sender, RoutedEventArgs e)
        {
            webTabPages.Visibility = Visibility.Collapsed;
            _favList.Visibility = Visibility.Visible;
            _favList.Items.Clear();
            foreach (var i in _favouritePages)
                _favList.Items.Add(i.Key);
        }

        private void favList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_favList.SelectedIndex == -1) return;
            _webPages.Add(new WebPage(webTabPages, _favouritePages, _favouritePages[(string)_favList.SelectedValue]));
            webTabPages.Visibility = Visibility.Visible;
            _favList.Visibility = Visibility.Collapsed;
            (webTabPages.Items[webTabPages.Items.Count - 2] as TabItem).IsSelected = true;
        }

        private void LoadFavList()
        {
            if (File.Exists("FavList.txt"))
            {
                FileStream toLoad = new FileStream("FavList.txt", FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    StreamReader toRead = new StreamReader(toLoad, Encoding.Default);
                    string[] fill = toRead.ReadToEnd().Split(' ');
                    for (int i = 0; i < fill.Length - 1; i+=2)
                    {
                        _favouritePages.Add(fill[i], fill[i + 1]);
                    }
                }  catch(Exception) { }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileStream toSave = new FileStream("FavList.txt", FileMode.Create, FileAccess.Write, FileShare.Write);
            StreamWriter toWrite = new StreamWriter(toSave, Encoding.Default);
            string[] favPages = _favouritePages.Keys.ToArray();
            for (int i = 0; i < _favouritePages.Count; i++)
            {
                toWrite.Write(favPages[i] + " ");
                toWrite.Write(_favouritePages[favPages[i]] + " ");
                toWrite.Flush();
            }
        }

        #endregion
    }

    /// <summary>
    /// Web page class
    /// </summary>
    public class WebPage
    {
        // Bool for text url
        private bool _chooseAll;

        // Favourites list
        private Dictionary<string, string> _favouritePages;

        private TabControl _tabControl;

        private DockPanel _formPage;
        private TabItem _tabItem;
        private TextBox _textUrl;
        private WebBrowser _webComp;

        public WebPage(TabControl addTo, Dictionary<string, string> favourites, string startWebSite = "http://www.google.com")
        {
            // Setting up favourite pages

            _favouritePages = favourites;

            // Adding tabControl reference

            _tabControl = addTo;

            // Forming panels

            DockPanel newPage = new DockPanel();
            _formPage = new DockPanel();
            _formPage.MinWidth = 300;
            _formPage.Margin = new Thickness(5);

            // Adding buttons for upper menu

            _formPage.Children.Add(new Button());
            _formPage.Children.Add(new Button());
            _formPage.Children.Add(new Button());
            _formPage.Children.Add(new Button());
            _formPage.Children.Add(new Button());
            _formPage.Children.Add(new Button());

            _textUrl = new TextBox();
            _formPage.Children.Add(_textUrl);

            int width = 20;
            int height = 20;

            (_formPage.Children[0] as Button).Margin = new Thickness(3);
            (_formPage.Children[0] as Button).Click += buttonClose_Click;
            (_formPage.Children[0] as Button).Background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/SimpleWeb;component/Resources/Close.png")));
            DockPanel.SetDock(_formPage.Children[0], Dock.Left);

            (_formPage.Children[1] as Button).Margin = new Thickness(5);
            (_formPage.Children[1] as Button).Click += buttonUndo_Click;
            (_formPage.Children[1] as Button).Background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/SimpleWeb;component/Resources/Undo.png")));
            DockPanel.SetDock(_formPage.Children[1], Dock.Left);

            (_formPage.Children[2] as Button).Margin = new Thickness(5);
            (_formPage.Children[2] as Button).Click += buttonRedo_Click;
            (_formPage.Children[2] as Button).Background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/SimpleWeb;component/Resources/Redo.png")));
            DockPanel.SetDock(_formPage.Children[2], Dock.Left);

            (_formPage.Children[3] as Button).Margin = new Thickness(5);
            (_formPage.Children[3] as Button).Click += buttonHome_Click;
            (_formPage.Children[3] as Button).Background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/SimpleWeb;component/Resources/Home.png")));
            DockPanel.SetDock(_formPage.Children[3], Dock.Left);

            (_formPage.Children[4] as Button).Margin = new Thickness(5);
            (_formPage.Children[4] as Button).Click += buttonFavourite_Click;
            (_formPage.Children[4] as Button).Background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/SimpleWeb;component/Resources/Favourite.png")));
            DockPanel.SetDock(_formPage.Children[4], Dock.Left);

            (_formPage.Children[5] as Button).Margin = new Thickness(5);
            (_formPage.Children[5] as Button).Click += buttonRefresh_Click;
            (_formPage.Children[5] as Button).Background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/SimpleWeb;component/Resources/Refr.png")));
            DockPanel.SetDock(_formPage.Children[5], Dock.Left);

            for (int i = 0; i < 6; i++)
            {
                (_formPage.Children[i] as Button).Width = width;
                (_formPage.Children[i] as Button).Height = height;
            }


            (_formPage.Children[0] as Button).Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));

            (_formPage.Children[4] as Button).Foreground = new SolidColorBrush(Color.FromRgb(255, 210, 0));

            (_formPage.Children[6] as TextBox).Margin = new Thickness(5);
            (_formPage.Children[6] as TextBox).Text = startWebSite;
            (_formPage.Children[6] as TextBox).KeyUp += txtUrl_KeyUp;
            (_formPage.Children[6] as TextBox).MouseEnter += txtUrl_MouseEnter;
            (_formPage.Children[6] as TextBox).GotMouseCapture += txtUrl_GotMouseCapture;
            DockPanel.SetDock(_formPage.Children[6], Dock.Right);

            DockPanel.SetDock(_formPage, Dock.Top);

            newPage.Children.Add(_formPage);

            // Border setup

            Border bord = new Border();
            bord.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            bord.BorderThickness = new Thickness(10);
            DockPanel.SetDock(bord, Dock.Top);

            newPage.Children.Add(bord);

            // Web browser setup

            _webComp = new WebBrowser();
            newPage.Children.Add(_webComp);

            (newPage.Children[2] as WebBrowser).Source = new Uri(startWebSite);
            (newPage.Children[2] as WebBrowser).Navigated += WebBrowser_Navigated;

            // Tab item to add 
            _tabItem = new TabItem();

            _tabItem.Content = newPage;

            addTo.Items.Insert(addTo.Items.Count - 1, _tabItem);
        }

        #region Methods

        // Method for external usage
        public void RefreshPage() { WebBrowser_Navigated(default, default); }

        private void WebBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            if (_webComp.Source == null) return;
            _tabItem.Header = _webComp.Source.Host;
            _textUrl.Text = _webComp.Source.ToString();
            if (_favouritePages.ContainsKey(_webComp.Source.Host.ToString()))
                _formPage.Background = new SolidColorBrush(Color.FromRgb(255, 210, 0));
            else
                _formPage.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        private void buttonUndo_Click(object sender, RoutedEventArgs e)
        {
            try { _webComp.GoBack(); } catch (Exception) { }
        }

        private void buttonRedo_Click(object sender, RoutedEventArgs e)
        {
            try { _webComp.GoForward(); } catch (Exception) { }
        }

        private void buttonHome_Click(object sender, RoutedEventArgs e)
        {
            _webComp.Source = new Uri("http://www.google.com");
            _tabItem.Header = _webComp.Source.Host;
            _textUrl.Text = _webComp.Source.ToString();
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            _webComp.Source = null;
            _tabControl.Items.Remove(_tabItem);
        }

        private void buttonFavourite_Click(object sender, RoutedEventArgs e)
        {
            if (_favouritePages.ContainsKey(_webComp.Source.Host.ToString()))
            {
                _formPage.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                _favouritePages.Remove(_webComp.Source.Host);
            }
            else
            {
                _formPage.Background = new SolidColorBrush(Color.FromRgb(255, 210, 0));
                _favouritePages.Add(_webComp.Source.Host, _webComp.Source.ToString());
            }
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            _webComp.Refresh();
        }

        private void txtUrl_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    _webComp.Navigate(new Uri(_textUrl.Text));
                    _tabItem.Header = _webComp.Source.Host;
                }
            }
            catch (Exception err) { MessageBox.Show(err.Message); _textUrl.Text = _webComp.Source.ToString(); }
        }

        private void txtUrl_MouseEnter(object sender, MouseEventArgs e)
        {
            _chooseAll = true;
        }

        private void txtUrl_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (_chooseAll == true)
                _textUrl.SelectAll();
            _chooseAll = false;
        }

        #endregion
    }
}
