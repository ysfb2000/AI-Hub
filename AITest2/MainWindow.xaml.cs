// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using AITest2.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AITest2
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {


        public MainWindow()
        {
            this.InitializeComponent();
            tabView.IsAddTabButtonVisible = false;
        }

        private TabViewItem CreateNewTab(string title)
        {
            TabViewItem newItem = new TabViewItem();

            newItem.Header = title;
            newItem.IconSource = new SymbolIconSource() { Symbol = Symbol.Document };

            // The content of the tab is often a frame that contains a page, though it could be any UIElement.
            Frame frame = new Frame();
            frame.Navigate(typeof(ChatGPT));
            newItem.Content = frame;

            return newItem;
        }

        private void TabView_AddButtonClick(TabView sender, object args)
        {
            sender.TabItems.Add(CreateNewTab("ChatGPT"));
        }

        private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
        }

        private void ChatGPT_Click(object sender, RoutedEventArgs e)
        {
            var tabItem = CreateNewTab("ChatGPT");
            tabView.TabItems.Add(tabItem);
            tabView.SelectedItem = tabItem;
        }
    }
}
