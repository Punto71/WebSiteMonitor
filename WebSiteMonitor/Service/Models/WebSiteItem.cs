using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSiteMonitor.Service.Database;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service.Models {
    public class WebSiteItem {
        
        private readonly int _id;
        private readonly string _name;
        private readonly string _url;
        private int _pingInterval;
        private int _orderNumber;

        [JsonProperty("id")]
        public int Id {
            get {
                return _id;
            }
        }

        [JsonProperty("name")]
        public string Name {
            get {
                return _name;
            }
        }

        [JsonProperty("url")]
        public string Url {
            get {
                return _url;
            }
        }

        [JsonIgnore]
        public int PingInterval {
            get {
                return _pingInterval;
            }
            set {
                _pingInterval = value;
            }
        }

        private PingResult PingInfoItem {
            get {
                return PingResult.GetOrAddPingResult(Id);
            }
        }

        [JsonProperty("state")]
        public string LastState {
            get {
                return PingInfoItem.LastState;
            }
        }

        [JsonProperty("last_date")]
        public DateTime? LastCheckDate {
            get {
                return PingInfoItem.LastCheckDate;
            }
        }

        [JsonProperty("avg_response_time")]
        public int AvgResponseTime {
            get {
                return PingInfoItem.AvgResponseTime;
            }
        }

        [JsonProperty("availability_pct")]
        public int AvailabilityPct {
            get {
                return PingInfoItem.AvailabilityPct;
            }
        }

        [JsonProperty("order_number")]
        public int OrderNumber {
            get {
                return _orderNumber;
            }
        }

        public WebSiteItem(int id, string name, string url, int pingInterval) {
            _id = id;
            _name = name;
            _url = url;
            _pingInterval = pingInterval;
        }

        public WebSiteItem(DataRow row) {
            _id = (int)row[NameDict.ID];
            _name = (string)row[NameDict.NAME];
            _url = (string)row[NameDict.URL];
            _pingInterval = (int)row[NameDict.PING_INTERVAL];
            _orderNumber = (int)row[NameDict.ORDER_NUMBER];
        }

        public static List<WebSiteItem> GetAll() {
            var result = TableManager.GetTable(NameDict.WEB_SITE).Select().Select(t => new WebSiteItem(t)).OrderBy(t=>t._orderNumber).ToList();
            return result;
        }
    }
}
