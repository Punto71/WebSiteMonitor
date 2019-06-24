using WebSiteMonitor.Service.Support;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;

namespace WebSiteMonitor.Service.Database {
    public class KeyValueSwitch {
        const string KEY_VALUE_DICT = "keys_keyValueCache";
        const string VALUE_KEY_DICT = "keys_valueKeyCache";
        const string SPLITTER = "~";
        const string KEY_VALUE_PRFIX = "KeyValue";
        const string EXTENSION_VALUE_PREFIX = "_ext";

        #region Значение по ключу

        public static string GetValueByKey(string tableName, string columnName, object key, bool extension = false) {
            string value = string.Empty;
            var relation = TableKeys.GetColumnRelation(tableName, columnName,key);
            if (string.IsNullOrEmpty(relation.columnName) == false) {
                var cacheDict = GetValuesCache();
                var cacheKey = CreateCacheKey(tableName, columnName, key,true, extension);
                if (cacheDict.ContainsKey(cacheKey))
                    value = cacheDict[cacheKey];
                else {
                    var table = TableManager.GetTable(relation.tableName);
                    string valueColName = GetValueColumnName(table, relation.columnName);
                    try {
                        if (extension)
                            value = SelectValueByKey(table, relation.columnName, key, valueColName);
                        else
                            value = TableManager.SelectScalarFromTable(table, relation.columnName, key, valueColName).ToString();
                        cacheDict.Add(cacheKey, value.ToString());
                    } catch {
                        //TODO вывод в лог
                    }
                }
            }
            return value;
        }

        static Dictionary<string, string> GetValuesCache() {
            var globalCache = GlobalCache.Instance;
            Dictionary<string, string> cacheDict;
            if (globalCache.ContainsKey(KEY_VALUE_DICT))
                cacheDict = globalCache[KEY_VALUE_DICT] as Dictionary<string, string>;
            else {
                cacheDict = new Dictionary<string, string>();
                globalCache.TryAdd(KEY_VALUE_DICT, cacheDict);
            }
            return cacheDict;
        }

        private static string SelectValueByKey(DataTable relationTable, string idColName, object key, string valueColumnName) {
            string result = string.Empty;
            var row = TableManager.SelectRowsFromTable(relationTable, idColName, key)[0];
            result = GetRowStringView(row, idColName, valueColumnName);
            return result;
        }

        private static string GetRowStringView(DataRow row, string idColName, string valueColumnName) {
            string result = string.Empty;
            switch (valueColumnName) {
                default:
                    result = row[valueColumnName].ToString();
                    break;
            }
            return result;
        }

        #endregion

        #region Ключ по значению

        /// <param name="tableName">Текущая таблица</param>
        /// <param name="colName">Название текущей колонки</param>
        public static object GetKeyByValue(string tableName, string colName, object value) {
            object result = value;
            var relation = TableKeys.GetColumnRelation(tableName, colName);
            if (string.IsNullOrWhiteSpace(relation.columnName) == false) {
                int key;
                if (value is int)//если пришел уже ключ
                    result = value;
                else if (int.TryParse(value.ToString(), out key))//пробуем парсить ключ
                    result = key;
                else {
                    var cacheDict = GetKeysCache();
                    var cacheKey = CreateCacheKey(tableName, colName, value, false,false);
                    if (cacheDict.ContainsKey(cacheKey))//если закешировано
                        result = cacheDict[cacheKey];
                    else {
                        var relationTable = TableManager.GetTable(relation.tableName);
                        var valueColumnName = GetValueColumnName(relationTable, relation.columnName);
                        object selectObj = SelectKeyByValue(relationTable, relation.columnName, value, valueColumnName);
                        if (selectObj != DBNull.Value) {
                            result = selectObj;
                            cacheDict.Add(cacheKey, result);
                        }
                    }
                }
            }
            return result;
        }

        static Dictionary<string, object> GetKeysCache() {
            var globalCache = GlobalCache.Instance;
            Dictionary<string, object> cacheDict;
            if (globalCache.ContainsKey(VALUE_KEY_DICT))
                cacheDict = globalCache[VALUE_KEY_DICT] as Dictionary<string, object>;
            else {
                cacheDict = new Dictionary<string, object>();
                globalCache.TryAdd(VALUE_KEY_DICT, cacheDict);
            }
            return cacheDict;
        }

        private static object SelectKeyByValue(DataTable relationTable, string idColName, object value, string valueColumnName) {
            object result = -1;
            string stringValue = value.ToString();
            if ((int)result < 1)
                result = (int)TableManager.SelectScalarFromTable(relationTable, valueColumnName, value, idColName);
            return result;
        }

        static string GetBracketsValue(string value) {
            Regex regex = new Regex(@"\((?<Value>[^)]*)\)");
            var result = value;
            var match = regex.Match(value);
            int count = match.Groups.Count;
            if (count > 1)
                result = match.Groups[count - 1].Value;
            return result;
        }

        static string GetValueBeforBrackets(string value) {
            string bracketsValue = string.Format("({0})", GetBracketsValue(value));
            string result = value.Replace(bracketsValue, string.Empty);
            result = result.TrimEnd();
            return result;
        }

        #endregion

        #region Перекодировка строк

        public static DataRow DecodeRow(DataRow sourceRow, string tableName) {
            var destTable = TableManager.GetTable(tableName);
            return DecodeRow(sourceRow, destTable);
        }

        public static DataRow DecodeRow(DataRow sourceRow, DataTable destTable) {
            var resultRow = destTable.NewRow();
            var fkDict = TableKeys.GetTableKeys(destTable.TableName, TableKeys.KeyType.ForeignKey);
            foreach (DataColumn column in sourceRow.Table.Columns) {
                if (destTable.Columns.Contains(column.ColumnName)) {
                    var value = sourceRow[column];
                    if (value is string && string.IsNullOrWhiteSpace((string)value))
                        value = DBNull.Value;
                    if (value != null && value != DBNull.Value) {
                        if (fkDict.Contains(column.ColumnName))
                            value = GetKeyByValue(destTable.TableName, column.ColumnName, value);
                        else
                            value = Convert.ChangeType(value, destTable.Columns[column.ColumnName].DataType);
                    }
                    resultRow[column.ColumnName] = value;    
                }
            }
            return resultRow;
        }

        #endregion

        #region Список значений

        public static SortedSet<string> GetTableItems(string tableName, string columnName) {
            DataTable tab = TableManager.GetTable(tableName);
            bool isFk = TableKeys.CheckColumnKeyType(tableName, columnName, TableKeys.KeyType.ForeignKey);
            var keysList = GetUniqueSet(tab, columnName);
            var valuesList = new SortedSet<string>();
            foreach (var value in keysList) {
                if (isFk)
                    valuesList.Add(GetValueByKey(tableName, columnName, value, true));
                else
                    valuesList.Add(value.ToString());
            }
            if (valuesList.Contains("") == false)
                valuesList.Add("");
            return valuesList;
        }

        public static SortedDictionary<string, int> GetTableKeyItems(string tableName, string columnName, bool addNullItem = true) {
            DataTable tab = TableManager.GetTable(tableName);
            bool isFk = TableKeys.CheckColumnKeyType(tableName, columnName, TableKeys.KeyType.ForeignKey);
            var result = new SortedDictionary<string, int>();
            if (isFk) {
                var keysList = GetUniqueSet(tab, columnName);
                foreach (var key in keysList) {
                    var value = GetValueByKey(tableName, columnName, key, true);
                    if (result.ContainsKey(value) == false)
                        result.Add(value,(int)key);
                }
                if (result.ContainsKey("") == false && addNullItem)
                    result.Add("", -1);
            }
            return result;
        }

        public static SortedDictionary<string, int> GetRelativeItemsSource(string tableName, string columnName, bool addNullItem) {
            var relation = TableKeys.GetColumnRelation(tableName, columnName);
            var result = new SortedDictionary<string, int>();
            if (string.IsNullOrEmpty(relation.tableName) == false) {
                var table = TableManager.GetTable(relation.tableName);
                string valueColName = GetValueColumnName(table, relation.columnName);
                DataRow[] rows = table.Select();
                foreach (DataRow row in rows) {
                    var key = (int)row[relation.columnName];
                    var value = GetRowStringView(row, relation.columnName, valueColName);
                    result.Add(value, key);
                }
            }
            if (addNullItem)
                result.Add("", -1);
            return result;
        }

        private static HashSet<object> GetUniqueSet(DataTable table, string columnName) {
            var keysList = new HashSet<object>();
            foreach (DataRow row in table.Rows) {
                var value = row[columnName];
                if (value != DBNull.Value)
                    keysList.Add(value);
            }
            return keysList;
        }

        #endregion

        public static string GetValueColumnName(DataTable relationTable, string idColumnName) {
            int valueColumnIndex = relationTable.Columns.IndexOf(idColumnName) + 1;
            var valueColumnName = relationTable.Columns[valueColumnIndex].ColumnName;
            return valueColumnName;
        }

        static string CreateCacheKey(string tableName, string columnName, object value, bool isKeyValue, bool isExtension) {
            string prefix = isKeyValue ? KEY_VALUE_DICT : VALUE_KEY_DICT;
            string extPrefix = isExtension ? EXTENSION_VALUE_PREFIX : string.Empty;
            string cacheKey = string.Format("{0}{1}{2}{1}{3}{1}{4}{1}{5}", KEY_VALUE_PRFIX, SPLITTER, tableName, columnName, value, extPrefix);
            return cacheKey;
        }
    }
}