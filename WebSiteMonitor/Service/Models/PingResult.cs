using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSiteMonitor.Service.Database;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service.Models {
    public class PingResult {

        const string CACHE_NAME = "PingResultCache";

        public enum WebSiteState {
            online = 1,
            offline = 2,
            notChecked = 3
        }

        private WebSiteState _lastState;
        private DateTime? _lastCheckDate;
        private int _avgResponseTime;
        private int _prevResponseTime;
        private int _pingCount;
        private int _isOnlineCount;
        private ConcurrentDictionary<DateTime, int> _pingData;

        [JsonProperty("state")]
        public string LastState {
            get {
                var value = TableManager.SelectScalarFromTable(NameDict.WEB_SITE_STATE, NameDict.ID, (int)_lastState, NameDict.NAME);
                return value.ToString();
            }
        }

        [JsonProperty("last_date")]
        public DateTime? LastCheckDate {
            get {
                return _lastCheckDate;
            }
        }

        [JsonProperty("avg_response_time")]
        public int AvgResponseTime {
            get {
                return _avgResponseTime;
            }
        }

        [JsonProperty("availability_pct")]
        public int AvailabilityPct {
            get {
                if (_pingCount > 0)
                    return (int)(((double)_isOnlineCount / (double)_pingCount) * 100);
                else
                    return 0;
            }
        }

        public ConcurrentDictionary<DateTime, int> PingData {
            get {
                return _pingData;
            }
        }

        public PingResult(ConcurrentDictionary<DateTime, int> pingData, int prevResponseTime) {
            _lastState = WebSiteState.notChecked;
            _pingData = pingData;
            _prevResponseTime = prevResponseTime;
            _pingCount = pingData.Count;
            _isOnlineCount = pingData.Count(t => t.Value > 0);
            var sumValue = pingData.Values.Sum();
            if (_isOnlineCount > 0)
                _avgResponseTime = sumValue / _isOnlineCount;
        }

        private static ConcurrentDictionary<int, PingResult> GetOrCreateCache() {
            object cache;
            GlobalCache.Instance.TryGetValue(CACHE_NAME, out cache);
            var dict = cache as ConcurrentDictionary<int, PingResult>;
            if (dict == null) {
                dict = new ConcurrentDictionary<int, PingResult>();
                GlobalCache.Instance.TryAdd(CACHE_NAME, dict);
            }
            return dict;
        }

        public static PingResult GetOrAddPingResult(int webSiteId) {
            var item = GetPingResult(webSiteId);
            if (item != null)
                return item;
            else {
                var dict = GetOrCreateCache();
                item = CreatePingResult(webSiteId);
                dict.TryAdd(webSiteId, item);
                return item;
            }
        }

        private static PingResult CreatePingResult(int webSiteId) {
            try {
                var rows = TableManager.SelectRowsFromTable(NameDict.PING_RESULT, NameDict.WEB_SITE_ID, webSiteId);
                if (rows.Any()) {
                    var dataAsEnum = rows.Select(t => new KeyValuePair<DateTime, int>((DateTime)t[NameDict.PING_DATE], (int)t[NameDict.PING]));
                    var pingData = new ConcurrentDictionary<DateTime, int>(dataAsEnum);
                    var prevResponseTime = (int)rows.Last()[NameDict.PING];
                    return new PingResult(pingData, prevResponseTime);
                }
            } catch (InvalidOperationException ex) {
                Logger.AddError(ex.Message);
            }
            return new PingResult(new ConcurrentDictionary<DateTime, int>(), 0);
        }

        public static void ClearPingResult(int webSiteId) {
            var query = TableManager.CreateDeleteQuery(NameDict.PING_RESULT, NameDict.WEB_SITE_ID, webSiteId.ToString());
            if (DataBaseConnector.ExecuteNonQuery(query)) {
                TableManager.ClearCache(NameDict.PING_RESULT);
                var dict = GetOrCreateCache();
                PingResult tmp;
                if (dict.ContainsKey(webSiteId))
                    dict.TryRemove(webSiteId, out tmp);
                GetOrAddPingResult(webSiteId);
            }
        }

        public static PingResult GetPingResult(int webSiteId) {
            var dict = GetOrCreateCache();
            if (dict.ContainsKey(webSiteId))
                return dict[webSiteId];
            return null;
        }

        public static void SavePingResult(int webSiteId, int pingResult) {
            var table = TableManager.GetTable(NameDict.PING_RESULT);
            var row = table.NewRow();
            var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            row[NameDict.WEB_SITE_ID] = webSiteId;
            row[NameDict.PING_DATE] = date;
            row[NameDict.PING] = pingResult;
            try {
                if (DataBaseConnector.InserteRecord(row)) {
                    var data = GetOrAddPingResult(webSiteId);
                    data._pingData.TryAdd(date, pingResult);
                    var state = pingResult > 0 ? WebSiteState.online : WebSiteState.offline;
                    var item = GetOrAddPingResult(webSiteId);
                    if (pingResult > 0)
                        item._avgResponseTime = item.CalcAvgResponseTime(pingResult);
                    SavePingResultState(webSiteId, state);
                }
            } catch (FbException ex) {
                Logger.AddError(ex.Message);
            }
        }

        public static void SavePingResultState(int webSiteId, WebSiteState state) {
            var item = GetOrAddPingResult(webSiteId);
            if (state != WebSiteState.notChecked)
                item._pingCount++;
            if (state == WebSiteState.online)
                item._isOnlineCount++;
            item._lastState = state;
            item._lastCheckDate = DateTime.Now;
        }

        public int CalcAvgResponseTime(int newValue) {
            //s[n]=( (n-1)*s[n-1]+x[n] )/n
            var newAvgResponseTime = (_isOnlineCount * _avgResponseTime + newValue) / (_isOnlineCount + 1);
            return newAvgResponseTime;
        }
    }
}
