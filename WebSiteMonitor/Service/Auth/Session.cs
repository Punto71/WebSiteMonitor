using System;

namespace WebSiteMonitor.Service.Auth {
	
    public class Session {

        private const int SESSION_LIFE_TIME = 20 * 60 * 1000;
        private readonly string _id;

        private readonly string _userSessionKey;

        private readonly string _userId;
        private readonly string _userName;
        private readonly bool _isAdmin;

        private DateTime _lastRefreshTime;

        public Session(string userId, string userName, string password, bool isAdmin) {
            _userId = userId;
            _userName = userName;
            _isAdmin = isAdmin;
            _userSessionKey = CreateUserSessionKey(userName, password);
            _id = Guid.NewGuid().ToString();
            _lastRefreshTime = DateTime.Now;
        }

        public bool IsAlive {
            get { return _lastRefreshTime.AddMilliseconds(SESSION_LIFE_TIME) >= DateTime.Now; }
        }

        public string Id {
            get { return _id; }
        }

        public string UserName {
            get { return _userName; }
        }

        public string UserId {
            get { return _userId; }
        }

        public bool IsAdmin {
            get { return _isAdmin; }
        }

        public string UserSessionKey {
            get { return _userSessionKey; }
        }

        public void Refresh() {
            _lastRefreshTime = DateTime.Now;
        }

        public static string CreateUserSessionKey(string userName, string password) {
            return userName + password;
        }

        public override string ToString() {
            return string.Format("id: {0},  lastRefreshTime: {1}, isAlive {2}", Id, _lastRefreshTime, IsAlive);
        }
    }
}