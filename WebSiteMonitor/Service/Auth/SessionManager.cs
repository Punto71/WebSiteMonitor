using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Security.Authentication;
using WebSiteMonitor.Service.Database;
using WebSiteMonitor.Service.Support;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace WebSiteMonitor.Service.Auth {

    public class SessionManager : IDisposable {

        const int CHECK_INTERVAL = 1000;
        private static SessionManager _instance;

        public static SessionManager Instance {
            get {
                if (_instance == null)
                    _instance = new SessionManager();
                return _instance;
            }
        }

        public void Run() {
            _sessionWatcher.WorkerSupportsCancellation = true;
            _sessionWatcher.RunWorkerCompleted += (s, e) => { };
            _sessionWatcher.DoWork += StartSessionWatcher;
            _sessionWatcher.RunWorkerAsync();
        }

        ConcurrentDictionary<string, Session> _sessionsList;
        BackgroundWorker _sessionWatcher;

        private SessionManager() {
            _sessionsList = new ConcurrentDictionary<string, Session>();
            _sessionWatcher = new BackgroundWorker();
        }

        private void StartSessionWatcher(object sender, DoWorkEventArgs e) {
            var worker = sender as BackgroundWorker;
            while (worker.CancellationPending == false) {
                Thread.Sleep(CHECK_INTERVAL);
                try {
                    var enumerator = _sessionsList.GetEnumerator();
                    while (enumerator.MoveNext()) {
                        try {
                            var session = enumerator.Current.Value;
                            if (!session.IsAlive) {
                                LogOut(session);
                            }
                        } catch (Exception ex) {
                            var message = string.Format("Session watcher error: can get current session state or logout. Session: {0}", enumerator.Current.Value);
                            Logger.AddError(message);
                        }
                    }
                } catch (Exception ex) {
                    var message = string.Format("Session watcher error: can get sessions enumerator or moveNext. Session list: {0}", string.Join(" ;", _sessionsList));
                    Logger.AddError(message);
                }
            }
        }

        public Session LogIn(string login, string passMd5) {
            var session = GetSession(login, passMd5);
            if (session == null) {
                var userRows = TableManager.SelectRowsFromTable(NameDict.USERS, NameDict.LOGIN, login);
                if (userRows.Length == 1 && userRows[0][NameDict.PASSWORD].ToString() == passMd5) {
                    session = new Session(userRows[0][NameDict.ID].ToString(), login, passMd5, (bool)userRows[0][NameDict.IS_ADMIN]);
                    if (!_sessionsList.TryAdd(session.Id, session)) {
                        throw new Exception("Cant save session " + session);
                    }
                } else {
                    throw new AuthenticationException("Invalid username or password");
                }
            } else {
                session.Refresh();
            }
            return session;
        }

        public Session GetSessionByUserName(string userName) {
            return _sessionsList.Values.FirstOrDefault(t => t.UserName == userName);
        }

        public void LogOut(Session session) {
            if (session != null) {
                if (!_sessionsList.TryRemove(session.Id,out session)) {
                    throw new Exception("Cant remove session " + session);
                }
            }
        }

        public Session GetSession(string sessionId) {
            Session session = null;
            if (!string.IsNullOrWhiteSpace(sessionId) && _sessionsList.ContainsKey(sessionId)) {
                session = _sessionsList[sessionId];
            }
            return session;
        }

        private Session GetSession(string userName, string passMd5) {
            var key = Session.CreateUserSessionKey(userName, passMd5);
            var session = _sessionsList.Values.SingleOrDefault(t => t.UserSessionKey == key);
            return session;
        }

        public void Dispose() {
            _sessionWatcher.CancelAsync();
            while (_sessionWatcher.IsBusy)
                SupportUtils.DoEvents();
        }
    }
}
