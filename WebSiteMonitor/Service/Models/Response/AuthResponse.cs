using WebSiteMonitor.Service;
using WebSiteMonitor.Service.Auth;
using System;

namespace WebSiteMonitor.Service.Models.Response {
    public class AuthResponse : BaseResponse {
        public string access_token { get; set; }
        public string url { get; set; }
        public bool is_admin { get; set; }

        public AuthResponse(Session session, string responseUrl) {
            access_token = session.Id;
            is_admin = session.IsAdmin;
            url = responseUrl;
        }
    }
}
