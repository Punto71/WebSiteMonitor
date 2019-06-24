using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service.Database
{
    public class TableManager
    {

        private static DateTime _defaultCacheDuration {
            get {
                return DateTime.Now.AddMinutes(90);
            }
        }

        #region Select

        public static DataTable GetTable(string name) {
            string cacheName = CreateTableCacheName(name);
            if (string.IsNullOrWhiteSpace(name) == false) {
                if (GlobalCache.Instance.ContainsKey(cacheName)) {
                    return GlobalCache.Instance[cacheName] as DataTable;
                }
                var order = GetOrderColumn(name);
                DataTable table = GetTableFromBase(name, order);
                SetTablePrimaryKey(table);
                GlobalCache.Instance.TryAdd(cacheName, table);
                return table;
            }
            return null;
        }

        private static string GetOrderColumn(string tableName) {
            if (tableName == NameDict.WEB_SITE)
                return NameDict.ORDER_NUMBER + " asc";
            if (tableName == NameDict.WEB_SITE)
                return NameDict.PING_DATE + " desc";
            return string.Empty;
        }

        private static string CreateTableCacheName(string tableName) {
            string cacheName = string.Empty;
            cacheName += tableName;
            return cacheName;
        }

        private static void SetTablePrimaryKey(DataTable table) {
            var pkList = TableKeys.GetTableKeys(table.TableName, TableKeys.KeyType.PrimaryKey);
            var pkColumns = new DataColumn[pkList.Count];
            for(int i=0; i<pkList.Count;i++){
                var colName = pkList[i];
                if (table.Columns.Contains(colName))
                    pkColumns[i] = table.Columns[colName];
            }
            table.PrimaryKey = pkColumns;
        }

        public static DataTable GetTableFromBase(string name, string orderCol = null) {
            if (string.IsNullOrWhiteSpace(name) == false) {
                string query = string.Format("select * from {0}",name);
                if (!string.IsNullOrWhiteSpace( orderCol)) {
                    query += string.Format(" order by {0}", orderCol);
                }
                var result = DataBaseConnector.GetTableByQuery(query);
                result.TableName = name;
                return result;
            }
            return new DataTable();
        }

        public static int SelectCount(string tableName, string columnName, object value) {
            return SelectCount(GetTable(tableName), columnName, value);
        }

        public static int SelectCount(DataTable table, string columnName, object value) {
            var rows = SelectRowsFromTable(table, columnName, value);
            return rows.Length;
        }

        public static object GetMaxValue(string tableName, string columnName) {
            var table = GetTable(tableName);
            var value = table.Compute(string.Format("MAX([{0}])", columnName), string.Empty);
            return value;
        }

        public static DataRow[] SelectRowsFromTable(string tableName, string columnName, object value) {
            DataTable table = GetTable(tableName);
            var res = SelectRowsFromTable(table, columnName, value);
            return res;
        }

        public static DataRow SelectRowByPrimaryKey(string tableName, object value) {
            var rows = SelectRowsByPrimaryKeys(tableName, new object[] { value });
            if (rows.Length == 1) {
                return rows[0];
            } else {
                //var message = getManyRowsError(rows.Length, tableName);
                return null;
            }
        }

        public static DataRow[] SelectRowsByPrimaryKey(DataTable table, object value) {
            return SelectRowsByPrimaryKeys(table, new object[] { value });
        }

        public static DataRow[] SelectRowsByPrimaryKeys(DataTable table, object[] values) {
            var query = CreateWhereString(table, values);
            return table.Select(query);
        }

        public static DataRow[] SelectRowsByPrimaryKeys(string tableName, object[] values) {
            var table = GetTable(tableName);
            return SelectRowsByPrimaryKeys(table, values);
        }

        public static DataRow[] SelectRowsFromTable(DataTable table, string columnName, object value) {
            string query = CreateQuery(table,columnName,value);
            var result = table.Select(query);
            return result;
        }

        public static List<string> SelectListFromTable(string tableName, string whereColumn, object value, string selectColumn) {
            var table = GetTable(tableName);
            return SelectListFromTable(table, whereColumn, value, selectColumn);
        }

        public static List<string> SelectListFromTable(DataTable table, string whereColumn, object value, string selectColumn) {
            var result = new List<string>();
            if (table.Columns.Contains(selectColumn)){
            var rows = SelectRowsFromTable(table, whereColumn, value);
            foreach (var row in rows)
                result.Add(row[whereColumn].ToString());
            }
            return result;
        }

        public static object SelectScalarFromTable(string tableName, string whereColumn, object value, string selectColumn) {
            var table = GetTable(tableName);
            return SelectScalarFromTable(table, whereColumn, value, selectColumn);
        }

        public static object SelectScalarFromTable(DataTable table, string whereColumn, object value, string selectColumn) {
            var query = CreateQuery(table,whereColumn,value);
            return SelectScalarFromTable(table, query, selectColumn);
        }

        public static object SelectScalarFromTable(string tableName, string query, string selectColumn) {
            var table = GetTable(tableName);
            return SelectScalarFromTable(table, query, selectColumn);
        }

        public static object SelectScalarFromTable(DataTable table, string query, string selectColumn) {
            var rows = table.Select(query);
            if (rows.Length == 1)
                return rows[0][selectColumn];
            else {
                string message = string.Format("При выборе по условию {0}", query);
                string addon = getManyRowsError(rows.Length,table.TableName);
                throw new KeyNotFoundException(message + addon);
            }
        }

        private static string getManyRowsError(int rowLength, string tableName) {
            var result = string.Empty;
            if (rowLength == 0)
                result = string.Format(" не найдено записей.\nПроверьте таблицу '{0}' на наличие необходимой записи.", tableName);
            else if (rowLength > 1) {
                result = string.Format(" найдено более 1 записи ({0}).\nПроверьте таблицу '{1}' на наличие дубликатов.",
                    rowLength, tableName);
            }
            return result;
        }

        public static string CreateQuery(DataTable table, string columnName, object value){
            var columnType = table.Columns[columnName].DataType;
            string query;
            if (value == DBNull.Value || value == null || string.IsNullOrEmpty(value.ToString())) {
                query = string.Format("[{0}] is null", columnName);
                if (columnType == typeof(string))
                    query += string.Format(" or [{0}] = ''", columnName);
            } else
                query = string.Format("[{0}] = '{1}'", columnName, value);
            return query;
        }

        #endregion

        #region Update

        public static void UpdateRecord(DataRow oldRow, DataRow newRow) {
            var table = oldRow.Table;
            if (DataBaseConnector.UpdateRecord(oldRow, newRow)) {
                UpdateRowFromTable(table, oldRow, newRow);
            }
        }

        public static void UpdateRecords(Dictionary<DataRow, DataRow> changes) {
            if (changes.Count != 0) {
                var firstRow = changes.Keys.First();
                var table = firstRow.Table;
                if (DataBaseConnector.UpdateRecords(changes)) {
                    foreach (var pair in changes)
                        UpdateRowFromTable(table, pair.Key, pair.Value);
                }
            }
        }

        public static void UpdateRowFromTable(DataTable table, DataRow oldRow, DataRow newRow) {
            var sourceTable = newRow.Table;
            string query = CreateWhereString(oldRow);
            var rows = table.Select(query);//выбрали строки
            foreach (var row in rows) {
                foreach (DataColumn column in table.Columns) {
                    string colName = column.ColumnName;
                    if (table.Columns.Contains(colName)) {//проверяем что бы была такая колонка 
                        var newValue = newRow[colName];
                        if (!row[colName].Equals(newValue))
                            row[colName] = newValue;
                    }
                }
            }
        }

        #endregion

        #region Insert

        public static DataRow CreateRow(string tableName) {
            var row = GetTable(tableName).NewRow();
            row[TableKeys.GetPrimaryKey(tableName)] = GetNewId(tableName);
            return row;
        }

        public static void InsertRow(DataRow newRow, int index = -1) {
            var table = newRow.Table;
            if (DataBaseConnector.InserteRecord(newRow)) {
                if (index > 0)
                    table.Rows.InsertAt(newRow, index);
                else
                    table.Rows.Add(newRow);
            }
        }

        public static bool InsertRows(List<DataRow> rows, bool addRowsToTable) {
            if (DataBaseConnector.InserteRecords(rows)) {
                if (addRowsToTable)
                    foreach (var row in rows)
                        row.Table.Rows.Add(row);
                return true;
            }
            return false;
        }

        #endregion

        #region Delete

        /// <summary>Удаялет строку или несколько строк найденых по условию</summary>
        public static void DeleteRows(string tableName, string columnName, string value) {
            var sourceRows = SelectRowsFromTable(tableName, columnName, value);
            string query = CreateDeleteQuery(tableName, columnName, value);
            if (DataBaseConnector.ExecuteNonQuery(query)) {
                DeleteRows(sourceRows);
            }
        }

        /// <summary>
        /// Удаляет строку из базы и из таблицы
        /// </summary>
        public static void DeleteRow(DataTable table, DataRow deletedRow) {
            var pkKey = TableKeys.GetPrimaryKey(table.TableName);
            string query = CreateDeleteQuery(table.TableName, pkKey, deletedRow[pkKey].ToString());
            if (DataBaseConnector.ExecuteNonQuery(query)) {
                DeleteRowFromTable(table, deletedRow);
            }
        }

        public static string CreateDeleteQuery(string tableName, string columnName, string value) {
            var source = GetTable(tableName);
            var columnType = source.Columns[columnName].DataType;
            string query;
            if (columnType == typeof(DateTime)) {
                var date = DateTime.Parse(value);
                query = string.Format("delete from {0} where {1} = ADDDATE('{2}',0)",
                    tableName, columnName, date.ToString("yyyy.MM.dd"));
            } else {
                if (SupportUtils.IsFloatNumber(columnType))
                    value = value.Replace(',', '.');
                query = string.Format("delete from {0} where {1} = '{2}'", tableName, columnName, value);
            }
            return query;
        }

        /// <summary>Удаляет переданные строки из родительской таблицы(не из базы) </summary>
        public static void DeleteRows(DataRow[] rows) {
                for (int i = 0; i < rows.Length; i++) {
                rows[i].Table.Rows.Remove(rows[i]);
            }
        }

        /// <summary>Удаляет строку из таблицы (не из базы) даже если стрка не пренадлежит таблице</summary>
        public static void DeleteRowFromTable(DataTable table, DataRow deletedRow) {
            var list = new List<DataTable>() { table };
            DeleteRowsFromTable(list, deletedRow);
        }

        /// <summary>Удаляет строку из таблиц (не из базы) даже если стрка не пренадлежит таблице</summary>
        public static void DeleteRowsFromTable(List<DataTable> tables, DataRow deletedRow) {
            string query = CreateWhereString(deletedRow);
            foreach (DataTable table in tables) {
                if (table == deletedRow.Table)
                    table.Rows.Remove(deletedRow);
                else {
                    var rows = table.Select(query);
                    for (int i = 0; i < rows.Length; i++) {
                        table.Rows.Remove(rows[i]);
                    }
                }
            }
        }

        #endregion

        #region Support

        public static int GetNewId(string tableName) {
            var id = GetMaxValue(tableName, NameDict.ID);
            if (id == DBNull.Value)
                return 1;
            else
                return (int)id + 1;
        }

        public static void CreateColumn(string tableName, DataColumn column, bool isNotNull, string quality) {
            string query = string.Format("ALTER TABLE {0} ADD {1} {2}{4} {3}",
                tableName, column.ColumnName, column.DataType.Name.ToUpper(), isNotNull ? "NOT NULL" : "", quality);
            DataBaseConnector.ExecuteNonQuery(query);
        }

        public static string CreateWhereString(DataRow row) {
            var query = new StringBuilder();
            var sourceTable = row.Table;
            var pkList = TableKeys.GetTableKeys(sourceTable.TableName, TableKeys.KeyType.PrimaryKey);
            for (int i = 0; i < pkList.Count; i++) {
                var column = pkList[i];
                query.AppendFormat("[{0}] = '{1}'", column, row[column]);
                if (i != pkList.Count - 1)
                    query.Append(" and ");
            }
            return query.ToString();
        }

        public static string CreateWhereString(DataTable table,object[] values) {
            var query = new StringBuilder();
            var pkList = TableKeys.GetTableKeys(table.TableName, TableKeys.KeyType.PrimaryKey);
            if (values.Length == pkList.Count) {
                for (int i = 0; i < pkList.Count; i++) {
                    var column = pkList[i];
                    query.AppendFormat("[{0}] = '{1}'", column, values[i]);
                    if (i != pkList.Count - 1)
                        query.Append(" and ");
                }
            }
            return query.ToString();
        }

        public static void AddRowToTable(DataTable table, DataRow newRow) {
            var list = new List<DataTable>(1) { table };
            AddRowToTable(list, newRow);
        }

        public static void AddRowToTable(List<DataTable> tables, DataRow newRow) {
            var sourceTable = newRow.Table;
            foreach (DataTable table in tables) {
                DataRow row = table.NewRow();
                bool isEmpty = true;
                foreach (DataColumn column in sourceTable.Columns) {
                    string colName = column.ColumnName;
                    if (table.Columns.Contains(colName) && table.Columns[colName].DataType.Equals(column.DataType)) {
                        isEmpty = false;
                        row[colName] = newRow[column];
                    }
                }
                if (!isEmpty)
                    table.Rows.Add(row);
            }
        }

        public static string InsertTableRows(DataTable sourceTable, string destTableName) {
            string message = string.Empty;
            DataTable destTable = GetTable(destTableName);
            var newRows = new List<DataRow>(sourceTable.Rows.Count);
            var rowId = GetNewId(destTableName);
            foreach (DataRow row in sourceTable.Rows) {
                try {
                    var decodeRow = KeyValueSwitch.DecodeRow(row, destTable);
                    if (destTable.Columns.Contains("ID"))
                        decodeRow["ID"] = rowId++;
                    else
                        SetNewId(destTableName, decodeRow);
                    newRows.Add(decodeRow);
                    destTable.Rows.Add(decodeRow);
                } catch (Exception ex) {
                    message += "\n" + ex.Message + " Строка в файле " + sourceTable.Rows.IndexOf(row);
                }
            }
            if (string.IsNullOrEmpty(message)) {
                try {
                    DataBaseConnector.InserteRecords(newRows);
                    foreach (var row in newRows)
                        AddRowToTable(destTable, row);
                } catch (Exception ex) {
                }

            } else
                foreach (var row in newRows)
                    destTable.Rows.Remove(row);
            return message;
        }

        private static void SetNewId(string tableName, DataRow currentRow) {
            string aiColumn = null;
            var aiDict = TableKeys.GetTableKeys(tableName, TableKeys.KeyType.AutoIncrement);
            if (aiDict.Count != 0)
                aiColumn = aiDict[0];
            if (aiColumn != null) {
                var newId = TableManager.GetMaxValue(tableName, aiColumn);
                currentRow[aiColumn] = newId;
            }
        }

        public static bool TableIsCached(string tableName) {
            string cacheName = CreateTableCacheName(tableName);
            return GlobalCache.Instance.ContainsKey(cacheName);
        }

        public static void ClearCache() {
            GlobalCache.Instance.Clear();
        }

        public static void ClearCache(string tableName) {
            object tmp;
            string cacheName = CreateTableCacheName(tableName);
            GlobalCache.Instance.TryRemove(cacheName,out tmp);
        }
        #endregion
    }
}