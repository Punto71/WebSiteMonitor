using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSiteMonitor.Service.Database;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service.Models.Editors {
    class EditorElementItem {

        private const int ELEMENT_HEIGHT = 60;

        /// <summary>
        /// Тип редактора
        /// </summary>
        public string view { get; set; }

        /// <summary>
        /// Отображаемое название
        /// </summary>
        public string label { get; set; }

        /// <summary>
        /// Имя для биндинга
        /// </summary>
        public string name { get; set; }

        public int height {
            get {
                return ELEMENT_HEIGHT;
            }
        }

        public bool timepicker { get; set; }

        public AttributesItem attributes { get; set; }

        private PatternItem pattern { get; set; }

        public List<IdValueItem> options { get; set; }

        public EditorElementItem(string tableName, string columnName, Type columnType) {
            name = columnName;
            label = TableKeys.GetColumnComment(tableName, columnName);
            var fkKey = TableKeys.GetColumnRelation(tableName, columnName);
            bool isFk = !string.IsNullOrEmpty(fkKey.columnName);
            view = getColumnEditType(columnType, isFk);
            var maxLength = TableKeys.GetColumnMaxLength(tableName, columnName);
            if (maxLength > 0) {
                attributes = new AttributesItem(maxLength);
                pattern = PatternItem.getByType(columnType, maxLength);
            }
            if (columnType == typeof(DateTime))
                timepicker = true;
        }

        private string getColumnEditType(Type type, bool isForeignColumn) {
            string colType = "text";
            if (isForeignColumn) {
                colType = "combo";
            } else if (type == typeof(DateTime)) {
                colType = "datepicker";
            } else if (type == typeof(bool)) {
                colType = "checkbox";
            }
            return colType;
        }
    }
}
