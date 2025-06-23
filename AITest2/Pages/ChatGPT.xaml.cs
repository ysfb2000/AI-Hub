// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using AITest2.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AITest2.Models;
using AITest2.Pages.Dialogs;
using System.Collections.ObjectModel;
using Windows.Storage;
using System.Text.Json;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;

namespace AITest2.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatGPT : Page
    {
        readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        ChatGPTViewModel viewModel = new ChatGPTViewModel();
        CancellationTokenSource cts = new CancellationTokenSource();


        public ChatGPT()
        {
            this.InitializeComponent();

            richChat.Header5FontSize = 14;
            richChat.Header5Foreground = new SolidColorBrush(Colors.Green);
            richChat.Header5FontWeight = FontWeights.Normal;

            loadSessionFromFile();
            DeleteSessionButton.IsEnabled = false;
        }

        private async void Submit_click(object sender, RoutedEventArgs e)
        {
            

            var a1 = this.A1.Text.Trim();
            this.A1.Text = string.Empty;

            if (a1 == string.Empty)
            {
                return;
            }

            viewModel.submitIsEnable = false;
            viewModel.cancelIsEnable = true;

            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = this.txtSecretKey.Text,

            });


            await Task.Run(() =>
            {
                dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                {
                    if (viewModel.chatText.Length > 1)
                    {
                        // 添加Markdown的回车
                        viewModel.chatText += Config.MarkdownEnter;
                    }

                    // 将a1设置为绿色
                     var b1 = "#####" + a1;

                    viewModel.chatText += b1;
                    // 添加回车
                    viewModel.chatText += Config.MarkdownEnter;

                    viewModel.currentSession.listMessage.Add(ChatMessage.FromUser(a1));
                });
            });

            await Task.Delay(10);

            var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
            {
                Messages = viewModel.currentSession.listMessage,
                Model = "gpt-4.1",
                User = "a1",
                Temperature = (float?)0.9,
                MaxTokens = 800
            });

            await Task.Delay(10);

            var tempResponse = "";

            try
            {

                await foreach (var completion in completionResult.WithCancellation(cts.Token))
                {

                    if (completion.Successful)
                    {
                        dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                        {
                            viewModel.chatText += completion.Choices.First().Message.Content;
                            tempResponse += completion.Choices.First().Message.Content;
                        });

                        await Task.Delay(5);
                    }
                    else
                    {
                        if (completion.Error == null)
                        {
                            throw new Exception("Unknown Error");
                        }

                        viewModel.chatText += $"{completion.Error.Code}: {completion.Error.Message}";

                    }
                }


                // 流已经结束了
                viewModel.submitIsEnable = true;
                viewModel.cancelIsEnable = false;


            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233029) return;
                viewModel.chatText += $"Network error: {ex.Message}";
            }



            // 将AI回复的文本内容添加到聊天记录中
            viewModel.currentSession.listMessage.Add(ChatMessage.FromAssistant(tempResponse));
        }

        private void A1_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {

        }

        private void Cancel_click(object sender, RoutedEventArgs e)
        {
            viewModel.submitIsEnable = true;
            viewModel.cancelIsEnable = false;
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
        }

        private async void Save_click(object sender, RoutedEventArgs e)
        {
            foreach (var item in viewModel.sessionList) {
                // 保存聊天记录到Session\ChatGPT
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Session", CreationCollisionOption.OpenIfExists);
                folder = await folder.CreateFolderAsync("ChatGPT", CreationCollisionOption.OpenIfExists);
                var file = await folder.CreateFileAsync(item.path, CreationCollisionOption.ReplaceExisting);

                // 将session转为Json
                var json = JsonSerializer.Serialize(item);
                await FileIO.WriteTextAsync(file, json);
            }
        }

        private async void SessionDelete_click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = this.XamlRoot;
            dialog.Title = "Are you sure to delete the session?";
            dialog.PrimaryButtonText = "Yes";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = null;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var item = sessionListView.SelectedItem as SessionListItem;

                if (viewModel.currentSession == item)
                {
                    viewModel.currentSession = null;
                }

                // Remove the file in the path of Session\ChatGPT
                var folder = ApplicationData.Current.LocalFolder.CreateFolderAsync("Session", CreationCollisionOption.OpenIfExists).AsTask().Result;
                folder = folder.CreateFolderAsync("ChatGPT", CreationCollisionOption.OpenIfExists).AsTask().Result;

                var isExist = folder.TryGetItemAsync(item.path).AsTask().Result != null;
                if (isExist)
                {
                    var file = folder.GetFileAsync(item.path).AsTask().Result;
                    file.DeleteAsync().AsTask().Wait();
                }

                viewModel.sessionList.Remove(item);
                viewModel.chatText = string.Empty;
            }
        }

        private void SessionList_Change(object sender, SelectionChangedEventArgs e)
        {
            if (sessionListView.SelectedItem == null)
            {
                DeleteSessionButton.IsEnabled = false;
            } else
            {
                DeleteSessionButton.IsEnabled = true;
            }
        }

        private void loadSessionFromFile()
        {
            viewModel.sessionList = new ObservableCollection<SessionListItem>();

            // 读取Session/ChatGPT目录下的文件列表
            var sessionFolder = ApplicationData.Current.LocalFolder.CreateFolderAsync("Session", CreationCollisionOption.OpenIfExists).AsTask().Result;
            var chatGPTFolder = sessionFolder.CreateFolderAsync("ChatGPT", CreationCollisionOption.OpenIfExists).AsTask().Result;
            var files = chatGPTFolder.GetFilesAsync().AsTask().Result;

            foreach (var file in files)
            {
                // 用JSON格式读取文件内容
                var json = FileIO.ReadTextAsync(file).AsTask().Result;
                var session = JsonSerializer.Deserialize<SessionModel>(json);

                var sessionListItem = new SessionListItem()
                {
                    title = session.title,
                    path = file.Name,
                    listMessage = session.listMessage
                };

                viewModel.sessionList.Add(sessionListItem);
            }
        }

        private void SessionList_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            viewModel.currentSession = sessionListView.SelectedItem as SessionListItem;
            viewModel.chatText = viewModel.showMarkdownTextFromCurrentSession();
        }

        private async void AddSession_click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = this.XamlRoot;
            dialog.Title = "Add a new session";
            dialog.PrimaryButtonText = "Save";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = new SaveSessionDialog();

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var s = (dialog.Content as SaveSessionDialog).content.Trim();
                if (s == string.Empty)
                {
                    return;
                }

                SessionListItem item = new SessionListItem();
                item.title = s;
                item.path = Guid.NewGuid().ToString() + ".txt";
                item.listMessage = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("You are a helpful assistant.Please answer in Chinese.")
                };

                viewModel.currentSession = item;
                viewModel.sessionList.Add(item);

                sessionListView.SelectedItem = item;
                viewModel.chatText = viewModel.showMarkdownTextFromCurrentSession();
            }
        }
    }
}
