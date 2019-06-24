using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSiteMonitor.Service.Support {
    public class GlobalCache {

        private static ConcurrentDictionary<string, object> _instance;

        public static ConcurrentDictionary<string, object> Instance {
            get {
                if (_instance == null) {
                    _instance = new ConcurrentDictionary<string, object>();
                }
                return _instance;
            }
        }
    }
}
