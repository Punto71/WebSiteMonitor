using WebSiteMonitor.Service.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebSiteMonitor.Service.Controllers {
    public class BaseController : ApiController {

        protected HttpResponseMessage CreateResponse(HttpStatusCode code, object item = null) {
            if (item == null)
                return ControllerContext.Request.CreateResponse(code);
            else
                return ControllerContext.Request.CreateResponse(code, item);
        }

        protected HttpResponseMessage CreateResponse(object item) {
            if (item == null)
                return ControllerContext.Request.CreateResponse(HttpStatusCode.NotFound);
            else
                return ControllerContext.Request.CreateResponse(HttpStatusCode.OK, item);
        }

        protected string GetCurrentUserId() {
            return SessionManager.Instance.GetSessionByUserName(User.Identity.Name).UserId;
        }

        protected Session GetCurrentSession() {
            var sessionId = this.ControllerContext.Request.Headers.Authorization.Parameter;
            var session = SessionManager.Instance.GetSession(sessionId);
            return session;
        }
    }
}