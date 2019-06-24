using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSiteMonitor.Service.Database;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service.Models.Editors {
    class EditorItem {

        public List<EditorElementItem> elements { get; set; }

        public Dictionary<string, string> rules { get; set; }

        public Dictionary<string,object> item { get; set; }

        public EditorItem(DataRow _item) {
            item = createItem(_item);
        }

        public static EditorItem createByDataRow(DataRow _item) {
            var result = new EditorItem(_item);
            var table = _item.Table;
            result.elements = new List<EditorElementItem>(table.Columns.Count);
            result.rules = new Dictionary<string, string>();
            var tableName = _item.Table.TableName;
            var notNullKeys = TableKeys.GetTableKeys(tableName, TableKeys.KeyType.NotNull);
            var primaryKey = TableKeys.GetPrimaryKey(tableName);
            foreach (DataColumn column in _item.Table.Columns) {
                if (column.ColumnName != primaryKey) {
                    var editor = new EditorElementItem(tableName, column.ColumnName, column.DataType);
                    result.elements.Add(editor);
                    var rule = getRule(column.ColumnName, column.DataType, notNullKeys.Contains(column.ColumnName));
                    if (!string.IsNullOrEmpty(rule))
                        result.rules.Add(column.ColumnName, rule);
                }
            }
            return result;
        }

        private static string getRule(string columnName, Type dataType, bool isNotNull) {
            if (SupportUtils.IsNumber(dataType))
                return "isNumber";
            if (isNotNull)
                return "isNotEmpty";
            return string.Empty;
        }

        private static Tuple<string, string> createRule(string column, string value) {
            return new Tuple<string, string>(column, value);
        }

        private Dictionary<string, object> createItem(DataRow row) {
            var columns = row.Table.Columns;
            var result = new Dictionary<string, object>(columns.Count);
            foreach (DataColumn col in columns) {
                result.Add(col.ColumnName, row[col]);
            }
            return result;
        }
    }
}
