using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSiteMonitor.Service.Models.Response {
    public class ChangesResponse : BaseResponse {
        public int id;
        public bool is_removed;
        public PingResult changes;

        public ChangesResponse(int websiteId, bool isRemoved = false, PingResult lastResults = null) {
            id = websiteId;
            is_removed = isRemoved;
            changes = lastResults;
        }
    }
}
