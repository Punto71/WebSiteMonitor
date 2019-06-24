using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text;
using System.IO;
using WebSiteMonitor.Service.Support;
using System.Configuration;

namespace WebSiteMonitor.Service.Database {
    public class DataBaseConnector {

        static Dictionary<Type, FbDbType> _typeConvertBeforeExecDict = new Dictionary<Type, FbDbType>(){
            {typeof(String),FbDbType.VarChar},
            {typeof(Int32),FbDbType.Integer},
            {typeof(Decimal),FbDbType.Decimal},
            {typeof(DateTime),FbDbType.TimeStamp},
            {typeof(Int64),FbDbType.BigInt},
            {typeof(Boolean),FbDbType.SmallInt},
            {typeof(Double),FbDbType.Double}
        };

        static Dictionary<Type, Type> _typeConvertAfterExecDict = new Dictionary<Type, Type>(){
            {typeof(Int16),typeof(Boolean)},
        };

        private static FbConnection getFbdConnection() {
            var configConnectionString = ConfigurationManager.ConnectionStrings["FbdConnectionString"].ConnectionString;
            var connectionString = new FbConnectionStringBuilder(configConnectionString);
            var currentPath = Directory.GetCurrentDirectory();
            //connectionString.Database = Path.Combine(currentPath,connectionString.Database);
            var fbdConnection = new FbConnection(connectionString.ToString());
            return fbdConnection;
        }

        private static FbDbType ConvertType(Type type) {
            if (_typeConvertBeforeExecDict.ContainsKey(type))
                return _typeConvertBeforeExecDict[type];
            return FbDbType.VarChar;
        }

        private static FbParameter CreateParameter(DataColumn column, object value, int index) {
            string paramName = "@" + index.ToString();
            var fbdType = ConvertType(column.DataType);
            var parameter = new FbParameter(paramName, fbdType);
            parameter.SourceColumn = column.ColumnName;
            parameter.Value = value;
            return parameter;
        }

        #region Выполнение запросов

        public static DataTable GetTableByQuery(string query) {
            var fbdConnection = getFbdConnection();
            var adapter = new FbDataAdapter(query, fbdConnection);
            var tab = new DataTable();
            adapter.SelectCommand.Connection = fbdConnection;
            try {
                if (fbdConnection.State == ConnectionState.Closed)
                    fbdConnection.Open();
                adapter.Fill(tab);
                ChangeTypesAfterRead(tab);
                return tab;
            } catch (Exception e) {
                throw e;
            } finally { fbdConnection.Close(); }
        }

        private static void ChangeTypesAfterRead(DataTable table) {
            for (int i = 0; i < table.Columns.Count; i++) {
                var column = table.Columns[i];
                if (_typeConvertAfterExecDict.ContainsKey(column.DataType)) {
                    var columnName = column.ColumnName;
                    var tmpColumn = new DataColumn(columnName + "_TMP", _typeConvertAfterExecDict[column.DataType]);
                    table.Columns.Add(tmpColumn);
                    foreach (DataRow row in table.Rows) {
                        row[tmpColumn] = SupportUtils.ChangeType(row[column], tmpColumn.DataType);
                    }
                    table.Columns.Remove(column);
                    tmpColumn.ColumnName = columnName;
                }
            }
        }

        public static bool ExecuteNonQuery(string query) {
            var command = new FbCommand(query);
            var res = ExecuteNonQuery(command);
            return res;
        }

        public static bool ExecuteNonQuery(FbCommand command) {
            var fbdConnection = getFbdConnection();
            command.Connection = fbdConnection;
            try {
                fbdConnection.Open();
                command.ExecuteNonQuery();
                return true;
            } catch (Exception e) {
                throw e;
            } finally { fbdConnection.Close(); }
        }

        public static bool ExecuteWithTransaction(FbCommand[] commands) {
            if (commands.Length != 0) {
                var fbdConnection = getFbdConnection();
                foreach (var com in commands)
                    if (com != null)
                        com.Connection = fbdConnection;
                FbTransaction trans;
                fbdConnection.Open();
                trans = fbdConnection.BeginTransaction();
                try {
                    foreach (FbCommand com in commands)
                        if (com != null)
                            com.Transaction = trans;
                    foreach (FbCommand com in commands)
                        if (com != null)
                            com.ExecuteNonQuery();
                    trans.Commit();
                } catch (Exception e) {
                    trans.Rollback();
                    throw e;
                } finally { fbdConnection.Close(); }
            }
            return true;
        }

        public static object SelectScalar(string tableName, string whereColunmn, object value, string selectColumn) {
            string query = string.Format("select {0} from {1} where {2} = '{3}'", selectColumn, tableName, whereColunmn, value);
            var result = ExecuteScalar(query);
            return result;
        }

        public static object ExecuteScalar(string query) {
            var command = new FbCommand(query);
            var res = ExecuteScalar(command);
            return res;
        }

        public static object ExecuteScalar(FbCommand command) {
            var fbdConnection = getFbdConnection();
            command.Connection = fbdConnection;
            object value = null;
            try {
                fbdConnection.Open();
                value = command.ExecuteScalar();
                return value;
            } catch (Exception e) {
                throw e;
            } finally { fbdConnection.Close(); }
        }

        #endregion

        #region Select

        public static int SelectCount(string tableName, string columnName, object value) {
            string query;
            if (value == null || value == DBNull.Value)
                query = string.Format("select count(1) from {0} where {1} is null", tableName, columnName);
            else
                query = string.Format("select count(1) from {0} where {1} = '{2}'", tableName, columnName, value);
            var count = ExecuteScalar(query);
            var result = -1;
            if (SupportUtils.IsNumber(count)) {
                if (count is int)
                    result = (int)count;
                else
                    result = Convert.ToInt32(count);
            }
            return result;
        }

        #endregion

        #region Update

        public static bool UpdateRecord(DataRow oldRow, DataRow newRow) {
            var command = CreateUpdateCommand(oldRow, newRow);
            if (!string.IsNullOrWhiteSpace(command.CommandText))
                return ExecuteNonQuery(command);
            return true;
        }

        public static bool UpdateRecords(Dictionary<DataRow, DataRow> oldNewRows) {
            var commands = new FbCommand[oldNewRows.Count];
            int iter = 0;
            foreach (DataRow oldRow in oldNewRows.Keys) {
                var command = CreateUpdateCommand(oldRow, oldNewRows[oldRow]);
                if (!string.IsNullOrWhiteSpace(command.CommandText))
                    commands[iter] = command;
                iter++;
            }
            return ExecuteWithTransaction(commands);
        }

        private static FbCommand CreateUpdateCommand(DataRow oldRow, DataRow newRow) {
            var table = oldRow.Table;
            var pkList = TableKeys.GetTableKeys(table.TableName, TableKeys.KeyType.PrimaryKey);
            var query = new StringBuilder();
            var command = new FbCommand();
            int paramIndex = 1;
            query.AppendFormat("update {0} set", table.TableName);
            foreach (DataColumn column in table.Columns) {
                if (!oldRow[column].Equals(newRow[column.ColumnName])) {//если значение поменялось
                    var parameter = CreateParameter(column, newRow[column.ColumnName], paramIndex);
                    query.AppendFormat(" {0} = {1},", column.ColumnName, parameter.ParameterName);
                    command.Parameters.Add(parameter);
                    paramIndex++;
                }
            }
            if (command.Parameters.Count != 0) {
                query.Remove(query.Length - 1, 1);
                query.Append(" where ");
                for (int i = 0; i < pkList.Count; i++) {
                    var column = table.Columns[pkList[i]];
                    var parameter = CreateParameter(column, oldRow[column], paramIndex);
                    paramIndex++;
                    command.Parameters.Add(parameter);
                    query.AppendFormat("{0} = {1}", column.ColumnName, parameter.ParameterName);
                    if (i != pkList.Count - 1)
                        query.Append(" and ");
                }
                command.CommandText = query.ToString();
            }
            return command;
        }

        #endregion

        #region Insert

        public static bool InserteRecord(DataRow newRow) {
            var command = CreateInsertCommand(newRow);
            if (!string.IsNullOrWhiteSpace(command.CommandText))
                return ExecuteNonQuery(command);
            return true;
        }

        public static bool InserteRecords(List<DataRow> newRows) {
            var commands = new FbCommand[newRows.Count];
            for (int i = 0; i < newRows.Count; i++) {
                var command = CreateInsertCommand(newRows[i]);
                if (!string.IsNullOrWhiteSpace(command.CommandText))
                    commands[i] = command;
            }
            return ExecuteWithTransaction(commands);
        }

        private static FbCommand CreateInsertCommand(DataRow newRow) {
            var table = newRow.Table;
            var nnList = TableKeys.GetTableKeys(table.TableName, TableKeys.KeyType.NotNull);
            var query = new StringBuilder();
            var command = new FbCommand();
            int paramIndex = 1;
            query.AppendFormat("insert into {0} (", table.TableName);
            foreach (DataColumn column in table.Columns) {
                var value = newRow[column];
                if (value == DBNull.Value && nnList.Contains(column.ColumnName)) {//если недопустимое нулевое поле
                    //TODO error
                } else {
                    var parameter = CreateParameter(column, value, paramIndex);
                    query.AppendFormat(" {0},", column.ColumnName);
                    command.Parameters.Add(parameter);
                    paramIndex++;
                }
            }
            if (command.Parameters.Count != 0) {
                query.Remove(query.Length - 1, 1);
                query.Append(" ) values ( ");
                foreach (FbParameter parameter in command.Parameters) {
                    query.AppendFormat("{0},", parameter.ParameterName);
                }
                query.Remove(query.Length - 1, 1);
                query.Append(")");
                command.CommandText = query.ToString();
            }
            return command;
        }

        #endregion
    }
}