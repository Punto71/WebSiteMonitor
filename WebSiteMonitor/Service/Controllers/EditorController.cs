using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WebSiteMonitor.Service.Database;
using WebSiteMonitor.Service.Support;
using WebSiteMonitor.Service.Models;
using WebSiteMonitor.Service.Models.Editors;
using WebSiteMonitor.Service.Auth;

namespace WebSiteMonitor.Service.Controllers {
    [RoutePrefix("editor")]
    public class EditorController : BaseController {

        private static List<string> _allowEditEntityList = new List<string>() {
            NameDict.WEB_SITE
        };

        /// <summary>
        /// Создание пустого элемента
        /// </summary>
        [Route("new/{entity}")]
        [AdminAuthorizeAtribute]
        [HttpGet]
        public HttpResponseMessage CreateNew(string entity) {
            DataRow row = null;
            var table = TableManager.GetTable(entity.ToUpper());
            if (table != null && CheckEntityForEdit(entity))
                row = table.NewRow();
            return PrepareResponce(row);
        }

        /// <summary>
        /// Редактирование существующиего элемента
        /// </summary>
        [Route("edit/{entity}/{id}")]
        [AdminAuthorizeAtribute]
        [HttpGet]
        public HttpResponseMessage CreateNew(string entity, int id) {
            DataRow row = null;
            if (CheckEntityForEdit(entity))
                row = TableManager.SelectRowByPrimaryKey(entity.ToUpper(), id);
            return PrepareResponce(row);
        }

        /// <summary>
        /// Редактирование существующиего элемента
        /// </summary>
        [Route("delete/{entity}/{id}")]
        [AdminAuthorizeAtribute]
        [HttpGet]
        public HttpResponseMessage Delete(string entity, int id) {
            DataRow row = null;
            if (CheckEntityForEdit(entity)) {
                row = TableManager.SelectRowByPrimaryKey(entity.ToUpper(), id);
                if (row != null) {
                    BeforeDelete(row, id);
                    TableManager.DeleteRow(row.Table, row);
                    return CreateResponse(HttpStatusCode.OK);
                }
            }
            return CreateResponse(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Сохранение элемента
        /// </summary>
        [Route("save/{entity}")]
        [AdminAuthorizeAtribute]
        [HttpPut]
        public HttpResponseMessage Save(string entity) {
            var tableName = entity.ToUpper();
            var body = Request.Content.ReadAsStringAsync().Result;
            var text = HttpUtility.UrlDecode(body);
            var table = TableManager.GetTable(tableName);
            text = text.IndexOf("data=") == 0 ? text.Remove(0, 5) : text;
            var obj = JsonConvert.DeserializeObject(text) as JObject;
            if (obj != null && CheckEntityForEdit(entity)) {
                var newRow = ParseResponse(obj, table.NewRow());
                var isNew = false;
                if (isNew = newRow[NameDict.ID] == DBNull.Value)
                    newRow[NameDict.ID] = TableManager.GetNewId(tableName);
                var id = newRow[NameDict.ID];
                string message = null;
                try {
                    if (isNew)
                        TableManager.InsertRow(newRow);
                    else
                        TableManager.UpdateRecord(TableManager.SelectRowByPrimaryKey(tableName, id), newRow);
                    message = AfterSave(newRow, obj, isNew);
                } catch (FbException ex) {
                    if (ex.ErrorCode == 335544349){
                        var uniqColumns = TableKeys.GetUniqColumns(tableName).Select(t => TableKeys.GetColumnComment(tableName, t)).ToList();
                        message = "Violation of uniqueness by " + string.Join(", ",uniqColumns);
                    }
                }
                if (message != null)
                    return CreateResponse(HttpStatusCode.Conflict, message);
                return CreateResponse(HttpStatusCode.OK, id);
            }
            return PrepareResponce(null);
        }

        private static void BeforeDelete(DataRow deletedRow, int itemId) {
            switch (deletedRow.Table.TableName) {
                case NameDict.WEB_SITE:
                    PingWorker.Instance.StopWorker(itemId);
                    PingResult.ClearPingResult(itemId);
                    break;
            }
        }

        private static string AfterSave(DataRow savedRow, JObject savedObject, bool isNewItem) {
            switch (savedRow.Table.TableName) {
                case NameDict.WEB_SITE:
                    var item = new WebSiteItem(savedRow);
                    if (PingWorker.Instance.Contains(item)) {
                        PingWorker.Instance.ReinitWorker(item);
                    } else {
                        PingWorker.Instance.AddWebSite(item);
                    }
                    break;
            }
            return null;
        }

        private static DataRow ParseResponse(JObject obj, DataRow row) {
            var startIndexColumns = new SortedDictionary<int, string>();
            foreach (DataColumn col in row.Table.Columns) {
                if (obj[col.ColumnName] != null) {
                    var type = col.DataType;
                    var value = obj[col.ColumnName];
                    if (value.Type != JTokenType.Null)
                        row[col] = Convert.ChangeType(value, type);
                }

            }
            return row;
        }

        private bool CheckEntityForEdit(string entityName) {
            return _allowEditEntityList.Contains(entityName.ToUpper());
        }

        private HttpResponseMessage PrepareResponce(DataRow row) {
            if (row != null)
                return CreateResponse(HttpStatusCode.OK, EditorItem.createByDataRow(row));
            else
                return CreateResponse(HttpStatusCode.NotFound);
        }
    }
}
