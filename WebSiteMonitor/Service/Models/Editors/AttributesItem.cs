using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSiteMonitor.Service.Models.Editors {
    public class AttributesItem {
        public int maxlength { get; set; }

        public AttributesItem(int maxLength) {
            maxlength = maxLength;
        }
    }
}
