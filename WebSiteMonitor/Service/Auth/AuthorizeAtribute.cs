using System.Security.Principal;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Net;
using System.Net.Http;
using WebSiteMonitor.Service.Models.Response;

namespace WebSiteMonitor.Service.Auth {

    public class CustomAuthorizeAtribute : AuthorizeAttribute {

        protected override bool IsAuthorized(HttpActionContext actionContext) {
            var sessionId = GetSessionIdFromRequest(actionContext);
            if (!string.IsNullOrWhiteSpace(sessionId)) {
                var session = SessionManager.Instance.GetSession(sessionId);
                if (session != null && session.IsAlive) {
                    actionContext.RequestContext.Principal = new GenericPrincipal(new GenericIdentity(session.UserName), new string[] { });
                    session.Refresh();
                    return true;
                }
            }
            return false;
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext) {
            var response = actionContext.Request.CreateResponse(new ErrorResponse("Authorization error, try to login again", "login.html"));
            response.StatusCode = HttpStatusCode.Unauthorized;
            actionContext.Response = response;
        }

        public static string GetSessionIdFromRequest(HttpActionContext actionContext) {
            if (actionContext != null && actionContext.Request != null &&
                actionContext.Request.Headers != null && actionContext.Request.Headers.Authorization != null)
                return actionContext.Request.Headers.Authorization.Parameter;
            else
                return null;
        }
    }
}
