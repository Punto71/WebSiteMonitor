using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Security.Authentication;
using System.Security.Claims;
using WebSiteMonitor.Service.Models;
using WebSiteMonitor.Service.Auth;
using WebSiteMonitor.Service.Models.Response;
using WebSiteMonitor.Service.Controllers;

namespace WebSiteMonitor.Service.Controllers {

    public class LoginController : BaseController {

        [Route("login")]
        [HttpGet]
        public HttpResponseMessage LogIn(string username, string password) {
            try {
                var session = SessionManager.Instance.LogIn(username, password);
                return CreateResponse(HttpStatusCode.OK, new AuthResponse(session, "home.html"));
            } catch (AuthenticationException ex) {
                return CreateResponse(HttpStatusCode.Unauthorized, new ErrorResponse(ex.Message,"login.html"));
            }
        }

        [Route("logout")]
        [CustomAuthorizeAtribute]
        [HttpGet]
        public HttpResponseMessage LogOut() {
            var session = GetCurrentSession();
            if (session != null) {
                SessionManager.Instance.LogOut(session);
                return new HttpResponseMessage(HttpStatusCode.OK);
            } else {
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
        }

        [Route("refresh")]
        [CustomAuthorizeAtribute]
        [HttpGet]
        public HttpResponseMessage RefreshSession() {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
