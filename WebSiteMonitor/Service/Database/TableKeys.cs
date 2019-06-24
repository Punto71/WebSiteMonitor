using WebSiteMonitor.Service.Support;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace WebSiteMonitor.Service.Database
{
    public class TableKeys
    {
        #region Структуры

        public struct Key
        {
            public string tableName;
            public string columnName;
        }

        public struct Relation
        {
            public string columnName;
            public string referencedTable;
            public string referencedColumn;
        }

        public struct FkDicts
        {
            /// <summary>ключ - имяТаблицы~имяКолонки</summary>
            public Dictionary<string, Key> keyDict;
            /// <summary>ключ - имяТаблицы</summary>
            public Dictionary<string, List<Relation>> relationDict;
            public Dictionary<string, List<string>> columnDict;
        }

        public enum KeyType { AutoIncrement, NotNull, ForeignKey, PrimaryKey, None }

        #endregion

        const string FK_DICT_NAME = "keys_foreignKeys";
        const string PK_DICT_NAME = "keys_primaryKeys";
        const string AI_DICT_NAME = "keys_autoIncrements";
        const string NN_DICT_NAME = "keys_notNullColumns";
        const string UQ_DICT_NAME = "keys_uniqueColumns";
        const string ML_DICT_NAME = "keys_columnsMaxLength";
        const string COMMENT_DICT_NAME = "keys_columnsComment";
        const string SPLITTER = "~";

        delegate Dictionary<string, Dictionary<string, object>> getDictFromBase();

        private static DateTime _defaultCacheDuration {
            get {
                return DateTime.Now.AddMinutes(90);
            }
        }
        
        /// <summary>Проверяет есть ли у строки зависимости с другими таблицами</summary>
        /// <returns>true - есть зависиомсти, false - нет</returns>
        public static bool CheckConstraint(DataRow row) {
            var table = row.Table;
            var checkedTable = table.TableName;
            var fkList = GetForeignKeys();
            var relationDict = fkList.relationDict;
            foreach (var tabName in relationDict.Keys) {
                foreach (var relation in relationDict[tabName]) {
                    if (relation.referencedTable.Equals(checkedTable)) {
                        if (table.Columns.Contains(relation.referencedColumn)) {
                            var value = row[relation.referencedColumn];
                            var count = TableManager.SelectCount(tabName, relation.columnName, value);
                            if (count > 0)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        #region FK

        public static FkDicts GetForeignKeys() {
            var globalCache = GlobalCache.Instance;
            if (globalCache != null) {
                FkDicts fkDicts;
                if (globalCache.ContainsKey(FK_DICT_NAME)) {
                    fkDicts = (FkDicts)globalCache[FK_DICT_NAME];
                } else {
                    fkDicts = GetForeignKeysFromBase();
                    globalCache.TryAdd(FK_DICT_NAME, fkDicts);
                }
                return fkDicts;
            }
            return new FkDicts();
        }

        private static FkDicts GetForeignKeysFromBase() {
            var fkDicts = new FkDicts();
            fkDicts.keyDict = new Dictionary<string, Key>();
            fkDicts.relationDict = new Dictionary<string, List<Relation>>();
            fkDicts.columnDict = new Dictionary<string, List<string>>();
            string query = @"SELECT
	(detail_relation_constraints.rdb$relation_name) AS table_name,
    (detail_index_segments.rdb$field_name) AS field_name,
    (master_relation_constraints.rdb$relation_name) AS reference_table,
    (master_index_segments.rdb$field_name) AS fk_field
FROM
    rdb$relation_constraints detail_relation_constraints
    JOIN rdb$index_segments detail_index_segments ON detail_relation_constraints.rdb$index_name = detail_index_segments.rdb$index_name 
    JOIN rdb$ref_constraints ON detail_relation_constraints.rdb$constraint_name = rdb$ref_constraints.rdb$constraint_name -- Master indeksas
    JOIN rdb$relation_constraints master_relation_constraints ON rdb$ref_constraints.rdb$const_name_uq = master_relation_constraints.rdb$constraint_name
    JOIN rdb$index_segments master_index_segments ON master_relation_constraints.rdb$index_name = master_index_segments.rdb$index_name 
WHERE
    detail_relation_constraints.rdb$constraint_type = 'FOREIGN KEY'";
            var fkTable = DataBaseConnector.GetTableByQuery(query);
            foreach (DataRow row in fkTable.Rows) {
                string table = row[0].ToString().Replace(" ","");
                string column = row[1].ToString().Replace(" ", "");
                string refTable = row[2].ToString().Replace(" ", "");
                string refColumn = row[3].ToString().Replace(" ", "");
                AddValuesToFkDics(table, column, refTable, refColumn, fkDicts);
            }
            return fkDicts;
        }

        private static void AddValuesToFkDics(string tableName, string columnName, string refTable, string refColumn, FkDicts dicts) {
            string key = CreateFkDictKey(tableName, columnName);
            Key refKey = new Key { tableName = refTable, columnName = refColumn };
            Relation relation = new Relation { columnName = columnName, referencedTable = refTable, referencedColumn = refColumn };
            List<Relation> relationList;
            List<string> columnList;
            //Relation list
            if (dicts.relationDict.ContainsKey(tableName))
                relationList = dicts.relationDict[tableName];
            else {
                relationList = new List<Relation>();
                dicts.relationDict.Add(tableName, relationList);
            }
            //Column List
            if (dicts.columnDict.ContainsKey(tableName))
                columnList = dicts.columnDict[tableName];
            else {
                columnList = new List<string>();
                dicts.columnDict.Add(tableName, columnList);
            }
            //Relation List
            if (relationList.Contains(relation) == false)
                relationList.Add(relation);
            //Keys list
            if (dicts.keyDict.ContainsKey(key) == false)
                dicts.keyDict.Add(key, refKey);
            //Column List
            if (columnList.Contains(columnName) == false)
                columnList.Add(columnName);
        }

        private static void CopyKeys(string sourceTable, string destTable, FkDicts dicts) {
            if (dicts.relationDict.ContainsKey(sourceTable)) {//получаем исходник
                var sourceList = dicts.relationDict[sourceTable];
                List<Relation> destList;
                if (dicts.relationDict.ContainsKey(destTable))
                    destList = dicts.relationDict[destTable];
                else {
                    destList = new List<Relation>();
                    dicts.relationDict.Add(destTable, destList);
                }
                Key refKey;
                string key;
                foreach (var relation in sourceList) {
                    if (destList.Contains(relation) == false)
                        destList.Add(relation);
                    key = CreateFkDictKey(destTable, relation.columnName);
                    refKey = new Key { tableName = relation.referencedTable, columnName = relation.referencedColumn };
                    if (dicts.keyDict.ContainsKey(key) == false)
                        dicts.keyDict.Add(key, refKey);
                }
            }
        }

        public static Key GetColumnRelation(string tableName, string columnName, object value = null) {
            var keyDic = CreateFkDictKey(tableName, columnName);
            var dict = GetForeignKeys().keyDict;
            if (dict.ContainsKey(keyDic))
                return dict[keyDic];
            return new Key();
        }

        public static string CreateFkDictKey(string tableName, string columnName) {
            return tableName + SPLITTER + columnName;
        }

        #endregion

        #region PK

        public static string GetPrimaryKey(string tableName) {
            var keys = GetTableKeys(tableName, KeyType.PrimaryKey);
            if (keys.Count != 1)
                throw new Exception("В новой структуре не может быть несоклько первичных ключей");
            return keys[0];
        }

        public static Dictionary<string, List<string>> GetPrimaryKeys() {
            var globalCache = GlobalCache.Instance;
            Dictionary<string, List<string>> pkDict;
            if (globalCache.ContainsKey(PK_DICT_NAME)) {
                pkDict = (Dictionary<string, List<string>>)globalCache[PK_DICT_NAME];
            } else {
                pkDict = GetPrimaryKeysFromBase();
                globalCache.TryAdd(PK_DICT_NAME, pkDict);
            }
            return pkDict;
        }

        private static Dictionary<string, List<string>> GetPrimaryKeysFromBase() {
            string query = @"select
    rc.rdb$relation_name as table_name,
    sg.rdb$field_name as field_name
from
    rdb$indices ix
    left join rdb$index_segments sg on ix.rdb$index_name = sg.rdb$index_name
    left join rdb$relation_constraints rc on rc.rdb$index_name = ix.rdb$index_name
where
    rc.rdb$constraint_type = 'PRIMARY KEY'";
            var pkDict = LoadTableColumnsKeyDict(query);
            return pkDict;
        }

        #endregion

        #region AI

        public static Dictionary<string, List<string>> GetAutoIncrement() {
            return GetPrimaryKeys();
        }

        #endregion

        #region NN

        public static Dictionary<string, List<string>> GetNotNullColumns() {
            var globalCache = GlobalCache.Instance;
            Dictionary<string, List<string>> nnDict;
            if (globalCache.ContainsKey(NN_DICT_NAME)) {
                nnDict = (Dictionary<string, List<string>>)globalCache[NN_DICT_NAME];
            } else {
                nnDict = GetNotNullFromBase();
                globalCache.TryAdd(NN_DICT_NAME, nnDict);
            }
            return nnDict;
        }

        private static Dictionary<string, List<string>> GetNotNullFromBase() {
            string query = @"select f.rdb$relation_name, f.rdb$field_name
            from rdb$relation_fields f
            join rdb$relations r on f.rdb$relation_name = r.rdb$relation_name
            and r.rdb$view_blr is null 
            and (r.rdb$system_flag is null or r.rdb$system_flag = 0)
            and f.rdb$null_flag = 1
            order by 1, f.rdb$field_position";
            var nnDict = LoadTableColumnsKeyDict(query);
            return nnDict;
        }

        #endregion

        #region Uniq

        public static List<string> GetUniqColumns(string tableName) {
            var dict = GetUniqColumns();
            if (dict.ContainsKey(tableName))
                return dict[tableName];
            return new List<string>();
        }

        public static Dictionary<string, List<string>> GetUniqColumns() {
            var globalCache = GlobalCache.Instance;
            Dictionary<string, List<string>> uniqDict;
            if (globalCache.ContainsKey(UQ_DICT_NAME)) {
                uniqDict = (Dictionary<string, List<string>>)globalCache[UQ_DICT_NAME];
            } else {
                uniqDict = GetUniqColumnsFromBase();
                globalCache.TryAdd(UQ_DICT_NAME, uniqDict);
            }
            return uniqDict;
        }

        private static Dictionary<string, List<string>> GetUniqColumnsFromBase() {
            string query = @"select i.rdb$relation_name, ix.rdb$field_name
            from rdb$indices i, rdb$index_segments ix
            where i.rdb$unique_flag = 1
            and i.rdb$index_inactive is null
            and not exists(select 1 from rdb$relation_constraints rc where rc.rdb$index_name = i.rdb$index_name)
            and i.rdb$system_flag = 0
            and ix.rdb$index_name = i.rdb$index_name";
            var dict = LoadTableColumnsKeyDict(query);
            return dict;
        }

        #endregion

        #region Max length

        private static Dictionary<string, Dictionary<string, object>> GetMaxLendthFromBase() {
            string query = @"select
            rf.rdb$relation_name,
            rf.rdb$field_name,
            case f.rdb$field_type
                    when 8 then 9
                    else f.rdb$field_length / 4
                    end
            from rdb$relation_fields rf
            join rdb$fields f on (f.rdb$field_name = rf.rdb$field_source)
            where (coalesce(rf.rdb$system_flag, 0) = 0)";
            var dict = new Dictionary<string, Dictionary<string, object>>();
            DataTable table = DataBaseConnector.GetTableByQuery(query);
            foreach (DataRow row in table.Rows) {
                var tableName = row[0].ToString().Replace(" ", "");
                var columnName = row[1].ToString().Replace(" ", "");
                var length = Convert.ToInt32(row[2]);
                AddValueToDict(tableName, columnName, length, dict);
            }
            return dict;
        }

        public static int GetColumnMaxLength(string tableName, string columnName) {
            var dict = GetDict(ML_DICT_NAME, GetMaxLendthFromBase);
            if (dict.ContainsKey(tableName) && dict[tableName].ContainsKey(columnName))
                return Convert.ToInt32(dict[tableName][columnName]);
            return 0;
        }

        #endregion

        #region Comment

        private static Dictionary<string, Dictionary<string, object>> GetCommentsFromBase() {
            string query = @"select
            rf.rdb$relation_name,
            rf.rdb$field_name,
            rf.rdb$description
            from rdb$relation_fields rf
            where (coalesce(rf.rdb$system_flag, 0) = 0) and rf.rdb$description is not null";
            var dict = new Dictionary<string, Dictionary<string, object>>();
            DataTable table = DataBaseConnector.GetTableByQuery(query);
            foreach (DataRow row in table.Rows) {
                var tableName = row[0].ToString().Replace(" ", "");
                var columnName = row[1].ToString().Replace(" ", "");
                var comment = row[2].ToString();
                AddValueToDict(tableName, columnName, comment, dict);
            }
            return dict;
        }

        public static string GetColumnComment(string tableName, string columnName) {
            var dict = GetDict(COMMENT_DICT_NAME, GetCommentsFromBase);
            if (dict.ContainsKey(tableName) && dict[tableName].ContainsKey(columnName))
                return dict[tableName][columnName].ToString();
            return string.Empty;
        }

        #endregion
            
        #region support metods

        private static Dictionary<string, Dictionary<string, object>> GetDict(string cacheName, getDictFromBase func) {
            var globalCache = GlobalCache.Instance;
            Dictionary<string, Dictionary<string, object>> dict;
            if (globalCache.ContainsKey(cacheName)) {
                dict = (Dictionary<string, Dictionary<string, object>>)globalCache[cacheName];
            } else {
                dict = func();
                globalCache.TryAdd(cacheName, dict);
            }
            return dict;
        }

        static Dictionary<string, List<string>> LoadTableColumnsKeyDict(string query) {
            var dict = new Dictionary<string, List<string>>();
            DataTable table = DataBaseConnector.GetTableByQuery(query);
            foreach (DataRow row in table.Rows) {
                string tableName = row[0].ToString().Replace(" ","");
                string columnName = row[1].ToString().Replace(" ", "");
                AddValueToDict(tableName, columnName, dict);
            }
            return dict;
        }

        static void CopyKeys(string sourceTable, string destTable, Dictionary<string, List<string>> dict) {
            if (dict.ContainsKey(sourceTable)) {
                List<string> sourceList = dict[sourceTable];
                List<string> destList;
                if (dict.ContainsKey(destTable))
                    destList = dict[destTable];
                else {
                    destList = new List<string>();
                    dict.Add(destTable, destList);
                }
                foreach (var column in sourceList) {
                    if (destList.Contains(column) == false)
                        destList.Add(column);
                }
            }
        }

        static void AddValueToDict(string tableName, string columnName, Dictionary<string, List<string>> dict) {
            List<string> list;
            if (dict.ContainsKey(tableName))
                list = dict[tableName];
            else {
                list = new List<string>();
                dict.Add(tableName, list);
            }
            if (!list.Contains(columnName))
                list.Add(columnName);
        }

        static void AddValueToDict(string tableName, string columnName, object value, Dictionary<string, Dictionary<string, object>> dict) {
            Dictionary<string, object> list;
            if (dict.ContainsKey(tableName))
                list = dict[tableName];
            else {
                list = new Dictionary<string, object>();
                dict.Add(tableName, list);
            }
            if (!list.ContainsKey(columnName))
                list.Add(columnName, value);
        }

        #endregion

        #region KeyType

        public static bool CheckColumnKeyType(string tableName, string columnName, KeyType type) {
            if (type == KeyType.ForeignKey) {
                var dict = GetForeignKeys().keyDict;
                var key = CreateFkDictKey(tableName, columnName);
                return dict.ContainsKey(key);
            } else {
                var list = GetTableKeys(tableName, type);
                return list.Contains(columnName);
            }
        }

        public static List<string> GetTableKeys(string tableName, KeyType type) {
            Dictionary<string, List<string>> dict = null;
            switch (type) {
                case KeyType.AutoIncrement:
                    dict = GetAutoIncrement();
                    break;
                case KeyType.NotNull:
                    dict = GetNotNullColumns();
                    break;
                case KeyType.PrimaryKey:
                    dict = GetPrimaryKeys();
                    break;
                case KeyType.ForeignKey:
                    dict = GetForeignKeys().columnDict;
                    break;
            }
            if (dict != null)
                if (dict.ContainsKey(tableName))
                    return dict[tableName];
            return new List<string>();
        }

        #endregion
    }
}