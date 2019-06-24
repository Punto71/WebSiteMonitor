using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace WebSiteMonitor.Service.Support
{
    public class SupportUtils
    {
        static string[] MONTHS = new string[] { "янв.","февр.","марта","апр.","май","июнь","июль","авг.", "сент.","окт.","нояб.","дек."};

        public static bool IsNumber(object value) {
            if (value is double || value is int || value is short || value is long || value is decimal || value is float)
                return true;
            return false;
        }

        public static bool IsNumber(Type type) {
            if (type == typeof(double)
                || type == typeof(int)
                || type == typeof(short)
                || type == typeof(long)
                || type == typeof(decimal)
                || type == typeof(float))
                return true;
            return false;
        }

        public static bool IsFloatNumber(object value) {
            if (value is double || value is decimal || value is float)
                return true;
            return false;
        }

        public static bool IsFloatNumber(Type type) {
            if (type == typeof(double)
                || type == typeof(decimal)
                || type == typeof(float))
                return true;
            return false;
        }

        public static void DoEvents(Action action = null) {
            if (action == null)
                action = new Action(() => {});
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, action);
        }

        //public static int GetStringSize(string text, Font font) {
        //    using (var bmp = new Bitmap(1, 1))
        //    using (Graphics g = Graphics.FromImage(bmp)) {
        //        var size = new Unit(g.MeasureString(text, font).Width);
        //        return (int)size.Value;
        //    }
        //}

        public static string GetGreatherWordInText(string text) {
            var words = text.Split(' ');
            if (text.Length > 0) {
                var bigWord = words[0];
                foreach (var word in words) {
                    if (word.Length > bigWord.Length)
                        bigWord = word;
                }
                return bigWord;
            }
            return text;
        }

        public static object ChangeType(object value, Type type) {
            if (IsFloatNumber(type) && value is string) {
                if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",") {
                    value = value.ToString().Replace('.', ',');
                }
                value = Convert.ToDecimal(value);
                if (type == typeof(decimal))
                    return value;
            } else if (type == typeof(DateTime) && Regex.IsMatch(value.ToString(), "[а-я]")) {
                value = PrepareDateTimeToConvert(value.ToString());
            }
            return Convert.ChangeType(value, type);
        }

        public static object TryChangeType(object value, Type type, object defaultValue) {
            try {
                return ChangeType(value, type);
            } catch (Exception ex) {

            }
            return defaultValue;
        }

        public static string RangeString(string value, int count) {
            return string.Join("",Enumerable.Range(0, count).Select(t => "#").ToArray());
        }

        public static string PrepareDateTimeToConvert(string value) {
            var monthIndex = 1;
            var hasMonth = false;
            foreach (var month in MONTHS) {
                if (hasMonth = value.Contains(month)) {
                    value = value.Replace(month, monthIndex.ToString("00"));
                    break;
                }
                monthIndex++;
            }
            return value;
        }

        public static object ParseDateForCsvLoader(object value) {
            try {
                var strVal = PrepareDateTimeToConvert(value.ToString());
                var date = DateTime.Parse(strVal.Split('.')[0]);
                date = date.AddMilliseconds(double.Parse(strVal.Split('.')[1]));
                return date;
            } catch (Exception ex) {

            }
            return DBNull.Value;
        }

        public static string GetTextFromStream(Stream stream) {
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            stream.Position = 0;
            string text = Encoding.UTF8.GetString(bytes);
            return text;
        }

        public static DataTable RowsArrayToTable(DataTable parentTable, DataRow[] rows) {
            var newTable = parentTable.Clone();
            foreach (var row in rows)
                newTable.Rows.Add(row.ItemArray);
            return newTable;
        }

        public static bool? ToBoolean(object value) {
            if (value == null || value == DBNull.Value)
                return null;
            else return (bool)value;
        }

        public static decimal ToDecimal(object value, decimal defaultValue = 0) {
            if (value == null || value == DBNull.Value)
                return defaultValue;
            return Convert.ToDecimal(value);
        }

        public static string ToUnderScore(string pascalCaseText) {
            var result = Regex.Replace(pascalCaseText, @"(\p{Ll})(\p{Lu})", "$1_$2").ToUpper();
            return result;
        }

        public static double ConvertToMesure(int fromMeasure, int toMeasure, double value) {
            double result = 0;

            return result;
        }

        public static string GetFileCrc(string filePath) {
            using (var md5 = MD5.Create())
            using (var stream = System.IO.File.OpenRead(filePath))
                return ToHex(md5.ComputeHash(stream), true);
        }

        private static string ToHex(byte[] bytes, bool upperCase) {
            StringBuilder result = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
            return result.ToString();
        }

    }
}