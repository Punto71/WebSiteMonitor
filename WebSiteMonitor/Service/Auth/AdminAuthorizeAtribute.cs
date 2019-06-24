using System.Security.Principal;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Net;
using System.Net.Http;
using WebSiteMonitor.Service.Models.Response;
using System;
using WebSiteMonitor.Service.Database;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service.Auth {
    public class AdminAuthorizeAtribute : CustomAuthorizeAtribute {

        protected override bool IsAuthorized(HttpActionContext actionContext) {
            var result = base.IsAuthorized(actionContext);
            if (result) {
                var sessionId = GetSessionIdFromRequest(actionContext);
                var session = SessionManager.Instance.GetSession(sessionId);
                return session.IsAdmin;
            }
            return false;
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext) {
            var response = actionContext.Request.CreateResponse(new ErrorResponse("Permission denied"));
            response.StatusCode = HttpStatusCode.MethodNotAllowed;
            actionContext.Response = response;
        }
    }
}
