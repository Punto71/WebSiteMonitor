using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service.Models.Editors {
    public class PatternItem {
        public string mask { get; set; }
        public string allow { get; set; }

        public PatternItem(string _mask, string _allow) {
            mask = _mask;
            allow = _allow;
        }

        public static PatternItem getByType(Type type, int length) {
            if (SupportUtils.IsNumber(type)) {
                return new PatternItem(SupportUtils.RangeString("#", length), "integer");
            }
            return null;
        }
    }
}
