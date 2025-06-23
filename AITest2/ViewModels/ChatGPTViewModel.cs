using AITest2.Models;
using WiredBrainCoffee.CustomersApp.ViewModel;
using System.Collections.ObjectModel;
using static OpenAI.ObjectModels.StaticValues;

namespace AITest2.ViewModels
{
    internal class ChatGPTViewModel : ViewModelBase
    {
        private bool _submitIsEnable = true;
        private bool _cancelIsEnable = false;
        private bool _sessionIsEnable = false;
        private string _chatText = string.Empty;

        public ObservableCollection<SessionListItem> sessionList;

        private SessionListItem _currentSession;

        public ChatGPTViewModel()
        {
            sessionList = new ObservableCollection<SessionListItem>();
        }

        public string chatText
        {
            get { return _chatText; }
            set
            {
                _chatText = value;
                RaisePropertyChanged();
            }
        }

        public bool submitIsEnable
        {
            get { return _submitIsEnable; }
            set
            {
                _submitIsEnable = value;
                RaisePropertyChanged();
            }
        }

        public bool cancelIsEnable
        {
            get { return _cancelIsEnable; }
            set
            {
                _cancelIsEnable = value;
                RaisePropertyChanged();
            }
        }

        public SessionListItem currentSession
        {
            get { return _currentSession; }
            set
            {
                _currentSession = value;

                if (_currentSession != null)
                {
                    sessionIsEnable = true;
                }
                else
                {
                    sessionIsEnable = false;
                }

                RaisePropertyChanged();
            }
        }

        public bool sessionIsEnable
        {
            get { return _sessionIsEnable; }
            set
            {
                _sessionIsEnable = value;
                RaisePropertyChanged();
            }
        }

        public string showMarkdownTextFromCurrentSession()
        {
            if (currentSession != null)
            {
                var s = string.Empty;
                var list = currentSession.listMessage;

                var i = 0;

                foreach ( var item in list )
                {
                    if (item.Role == ChatMessageRoles.System) continue;

                    if (i % 2 == 0)
                    {
                        if (i > 0) s += Config.MarkdownEnter;
                        s += "#####";
                        s += item.Content;
                        s += Config.MarkdownEnter;
                    }
                    else {
                        s += item.Content;
                        s += Config.MarkdownEnter;
                    }

                    i++;
                }

                return s;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
