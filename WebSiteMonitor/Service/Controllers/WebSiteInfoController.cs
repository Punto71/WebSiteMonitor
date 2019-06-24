using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using WebSiteMonitor.Service.Auth;
using WebSiteMonitor.Service.Database;
using WebSiteMonitor.Service.Models;
using WebSiteMonitor.Service.Models.Response;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service.Controllers {

    [RoutePrefix("website")]
    public class WebSiteInfoController : BaseController {

        /// <summary>
        /// Получение информации по всем секциям и конференциям
        /// </summary>
        [Route("all")]
        [CustomAuthorizeAtribute]
        [HttpGet]
        public HttpResponseMessage GetAll() {
            var result = WebSiteItem.GetAll();
            return CreateResponse(HttpStatusCode.OK, result);
        }

        [Route("changes")]
        [CustomAuthorizeAtribute]
        [HttpPost]
        public HttpResponseMessage GetChenges([FromBody] List<int> idList) {
            if (idList != null && idList.Count > 0) {
                var result = new List<ChangesResponse>(idList.Count);
                foreach (var id in idList) {
                    var item = PingResult.GetPingResult(id);
                    var isRemoved = TableManager.SelectCount(NameDict.WEB_SITE, NameDict.ID, id) == 0;
                    result.Add(new ChangesResponse(id, isRemoved, item));
                }
                return CreateResponse(HttpStatusCode.OK, result);
            } else {
                return CreateResponse(HttpStatusCode.BadRequest, "Invalid parameters");

            }
        }

        [Route("{id}/clear_history")]
        [AdminAuthorizeAtribute]
        [HttpGet]
        public HttpResponseMessage GetChenges(int id) {
            if (id > 0) {
                PingResult.ClearPingResult(id);
                return CreateResponse(HttpStatusCode.OK);
            } else {
                return CreateResponse(HttpStatusCode.BadRequest, "Invalid parameters");

            }
        }

        [Route("{id}/details_count")]
        [CustomAuthorizeAtribute]
        [HttpPost]
        public HttpResponseMessage GetDetails(int id, [FromBody] int count) {
            if (id > 0 && count > 0) {
                var data = PingResult.GetPingResult(id);
                if (data != null) {
                    var result = data.PingData.OrderByDescending(t => t.Key).Take(count).ToList();
                    return CreateResponse(HttpStatusCode.OK, result);
                }
                return CreateResponse(HttpStatusCode.OK);
            } else {
                return CreateResponse(HttpStatusCode.BadRequest, "Invalid parameters");
            }
        }

        [Route("{id}/details_date")]
        [CustomAuthorizeAtribute]
        [HttpPost]
        public HttpResponseMessage GetDetails(int id, [FromBody] DateTime date) {
            if (id > 0) {
                var data = PingResult.GetPingResult(id);
                if (data != null) {
                    var result = data.PingData.Where(t => t.Key > date).OrderBy(t => t.Key).ToList();
                    return CreateResponse(HttpStatusCode.OK, result);
                }
                return CreateResponse(HttpStatusCode.OK);
            } else {
                return CreateResponse(HttpStatusCode.BadRequest, "Invalid parameters");
            }
        }
    }
}
