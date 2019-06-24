using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WebSiteMonitor {
    public class Logger {

        public static event EventHandler<string> MessageListChanged;

        private static List<string> _messageList;

        public static List<string> MessageList {
            get {
                if (_messageList == null)
                    _messageList = new List<string>();
                return _messageList;
            }
        }


        public static void AddError(string text) {
            AddMessage(CreateMessage(text, "ERROR"));
        }

        public static void AddInfo(string text) {
            AddMessage(CreateMessage(text, "INFO"));
        }

        public static void AddWarning(string text) {
            AddMessage(CreateMessage(text, "WARNING"));
        }

        private static void AddMessage(string message) {
            MessageList.Add(message);
            if (MessageListChanged != null)
                MessageListChanged.Invoke(MessageList, message);
        }

        private static string CreateMessage(string text, string type) {
            return string.Format("{0} [{1}] - {2}", DateTime.Now, type, text);
        }
    }
}
