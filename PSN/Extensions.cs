using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Text;
using System.Web;

namespace PSN.Extensions
{
    /// <summary>
    /// Расширения для стандартных классов.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Extensions
    {
        private static string ERROR_ENUM_NOT_FOUND = "Значение перечисления не найдено!";
        private static string ERROR_ENUM_IN_VALUE_EMPTY = "Входной параметр не должен быть пустым!";
        private static string ERROR_SQLCMD_NOT_INITIALIZED = "Поле CommandText не инициализировано!";
        private static Dictionary<string, string> CUSTOM_EXCEPTION_MESSAGE = new Dictionary<string,string>()
                                { {"REFERENCE CONSTRAINT 'FK_", "Попытка нарушения целостности данных.\nСуществует запись, которая ссылается на эту."},
                                  {"CHECK_DATEINTERVAL","Уже есть данные в указанном временном интервале!"},
                                  {"CHK_D_INTRVL","Уже есть данные в указанном временном интервале!"},
                                  {"FK__","Есть данные, ссылающиеся на эту запись!"},
                                  {"CANNOT INSERT THE VALUE NULL INTO COLUMN","Пустое поле недопустимо!"},
                                  {"ORA-02290: CHECK CONSTRAINT","Требуется указать обязательные поля!"},
                                  {"ORA-01400: CANNOT","Требуется указать обязательные поля!"},
                                  {"CHILD RECORD FOUND","Имеются данные, ссылающиеся на эту запись!"}
                                };
        
        #region Строки
        /// <summary>
        /// Количество указанных символов в строке.
        /// </summary>
        /// <param name="str">Строка в которой ведется подсчёт.</param>
        /// <param name="c">Символ, количество которого требуется подсчитать.</param>
        /// <returns>Число вхождений символа в строку.</returns>
        public static int CharsCount(this string str, char c)
        {
            int counter = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == c)
                {
                    counter++;
                }                    
            }
            return counter;
        }

        /// <summary>
        /// Удаление запятой из конца строки (если она там есть).
        /// </summary>
        /// <param name="str">Строка, над которой производятся манипуляции.</param>
        /// <returns>Строка без запятой.</returns>
        public static string TrimCommaAtEnd(this string str)
        {
            return str.EndsWith(",") ? str.Remove(str.LastIndexOf(",")) : str;
        }

        /// <summary>
        /// Получить MD5 хэш строки.
        /// </summary>
        /// <param name="str">Исходная строка</param>
        /// <returns>MD5 хэш.</returns>
        public static string GetMD5Hash(this string str)
        {
            return Crypto.GetMD5Hash(str);
        }

        /// <summary>
        /// Закодировать содержимое строки в BASE64.
        /// </summary>
        /// <param name="str">Исходная строка.</param>
        /// <returns>Кодированная в BASE64 строка.</returns>
        public static string ToBASE64(this string str)
        {
            return Crypto.ConvertToBase64(str);
        }

        /// <summary>
        /// Раскодировать содержимое строки из BASE64.
        /// </summary>
        /// <param name="str">Исходная строка.</param>
        /// <returns>Раскодированная из BASE64 строка.</returns>
        public static string FromBASE64(this string str)
        {
            return Crypto.ConvertFromBase64(str);
        }

        /// <summary>
        /// Преобразование значения строки к значению указанного перечисления.
        /// </summary>
        /// <typeparam name="T">Тип перечисления.</typeparam>
        /// <param name="value">Строковое представление значения перечисления.</param>
        /// <returns>Значение перечисления.</returns>
        public static T ToEnum<T>(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception(ERROR_ENUM_IN_VALUE_EMPTY);
            }
            T result;
            try
            {
                object obj = Enum.Parse(typeof(T), value, true);
                result = (T)obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        #endregion Строки

        #region Работа с БД

        /// <summary>
        /// Возвращает массив SQL параметров, инициализированных данными из полей заданного объекта obj.
        /// </summary>
        /// <param name="cmd">Команда SQL с инициализированным полем CommandText.
        /// Имена параметров в запросе должны совпадать с именами полей объекта obj.</param>
        /// <param name="obj">Объект, из полей которого подставляются значения в параметры запроса.</param>
        /// <returns>Массив SQL параметров, заполненный данными из объекта obj.</returns>
        public static SqlParameter[] GetSQLParameters(this SqlCommand cmd, object obj)
        {
            List<SqlParameter> result = new List<SqlParameter>();
            string query = cmd.CommandText;
            
            if (string.IsNullOrEmpty(query))
            {
                throw new Exception(ERROR_SQLCMD_NOT_INITIALIZED);
            }

            Type t = obj.GetType();
            PropertyInfo[] propInfos = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo pinf in propInfos)
            {
                if (query.Contains("@" + pinf.Name))
                {
                    SqlParameter p = new SqlParameter("@" + pinf.Name, pinf.GetValue(obj, null));
                    if (pinf.PropertyType == typeof(System.Int32) && (int)pinf.GetValue(obj, null) < 0)
                    {
                        p.Value = DBNull.Value;
                    }
                    if (pinf.PropertyType == typeof(System.String) && string.IsNullOrEmpty((pinf.GetValue(obj, null) == null) ? "" : pinf.GetValue(obj, null).ToString()))
                    {
                        p.Value = DBNull.Value;
                    }
                    if (pinf.PropertyType == typeof(System.DateTime) && ((DateTime)pinf.GetValue(obj, null)).Equals(DateTime.MinValue))
                    {
                        p.Value = DBNull.Value;
                    }
                    if (pinf.PropertyType == typeof(System.Double) && ((Double)pinf.GetValue(obj, null)).Equals(Double.NaN))
                    {
                        p.Value = DBNull.Value;
                    }

                    result.Add(p);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Заполнение значений параметров SqlCommand по их имени из объекта obj.
        /// </summary>
        /// <param name="cmd">Команда SQL с инициализированным полем CommandText.
        /// Имена параметров в запросе должны совпадать с именами полей объекта obj.</param>
        /// <param name="obj">Объект, из полей которого подставляются значения в параметры запроса.</param>
        public static void FillParameters(this SqlCommand cmd, object obj)
        {
            cmd.Parameters.Clear();
            try
            {
                SqlParameter[] prms = GetSQLParameters(cmd, obj);
                if (prms != null)
                {
                    cmd.Parameters.AddRange(prms);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Возвращает запрос для подсчёта количества строк, которое вернет команда с текущим текстом sql-запроса.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns>Запрос для вычисления количества строк.</returns>
        public static string GetCountQuery(this SqlCommand cmd)
        {
            string result = "";
            if (!string.IsNullOrEmpty(cmd.CommandText))
            {
                int from_clause_occurence_index = cmd.CommandText.ToUpper().IndexOf(" FROM ");
                int orderby_clause_occurence_index = cmd.CommandText.ToUpper().LastIndexOf("ORDER BY ");
                int cnt_query_tail_len = ((orderby_clause_occurence_index == -1) ? cmd.CommandText.Length : orderby_clause_occurence_index) - from_clause_occurence_index;

                string count_query = "SELECT count(*) cnt " + cmd.CommandText.Substring(from_clause_occurence_index, cnt_query_tail_len);
            }
            return result;
        }

        /// <summary>
        /// Инициализирует свойства объекта obj, данными полученными в первой строке результатов запроса.
        /// Имена инициализируемых полей должны совпадать с названиями столбцов данных.
        /// Если данные в результате отсутствуют, то значения полей объекта остаются неизменными. 
        /// </summary>
        /// <param name="drd">SqlDataReader из которого получаются результаты запроса.</param>
        /// <param name="obj">Объект, значения полей которого требуется обновить.</param>
        /// <returns>Истина - поля объекта obj обновились, ложь - остались прежними или привелись к значениям по-умолчанию.</returns>
        public static bool FillParameters(this SqlDataReader drd, object obj)
        {
            bool result = false;
            if (drd.HasRows)
            {
                drd.Read();
                Type t = obj.GetType();
                PropertyInfo[] pInfos = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo pinf in pInfos)
                {
                    string p_name = pinf.Name;
                    for (int i = 0, ccnt = drd.FieldCount; i < ccnt; i++)
                    {
                        if (drd.GetName(i).Equals(p_name))
                        {
                            object tmp = drd[i];
                            if (tmp != null && !tmp.GetType().Equals(typeof(DBNull)))
                            {
                                pinf.SetValue(obj, tmp, null);
                                result = true;
                            }
                            else
                            {
                                if (pinf.PropertyType == typeof(System.Int32))
                                {
                                    pinf.SetValue(obj, -1, null);
                                    break;
                                }
                                if (pinf.PropertyType == typeof(System.String))
                                {
                                    pinf.SetValue(obj, "", null);
                                    break;
                                }
                                if (pinf.PropertyType == typeof(System.DateTime))
                                {
                                    pinf.SetValue(obj, DateTime.MinValue, null);
                                    break;
                                }
                                if (pinf.PropertyType == typeof(System.Double))
                                {
                                    pinf.SetValue(obj, Double.NaN, null);
                                    break;
                                }
                                if (pinf.PropertyType == typeof(System.Boolean))
                                {
                                    pinf.SetValue(obj, false, null);
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return result;
        }

        #endregion Работа с БД

        #region Данные
        /// <summary>
        /// Можно ли трактовать строку как пустую?
        /// </summary>
        /// <param name="self">Проверяемая строка</param>
        /// <param name="meaning_columns_count">Количество значащих столбцов таблицы (начиная с нулевого), по которым определяется пустота строки.</param>
        /// <returns>Истина - считается, что строка условно "пуста".</returns>
        public static bool IsEmpty(this System.Data.DataRow self, int meaning_columns_count = 1)
        {
            bool result = true;
            for (int i = 0; i < meaning_columns_count; i++)
            {
                result &= self[i] == null || self[i].GetType() == typeof(DBNull) || string.IsNullOrEmpty(self[i].ToString());
            }
            return result;
        }
        #endregion Данные

        #region Исключения

        /// <summary>
        /// Запись информации об исключении в файл журналирования настроек велосипеда PSN.
        /// </summary>
        /// <param name="ex">Исключение, информацию о котором нужно запротоколировать.</param>
        public static void WriteToPSNLog(this Exception ex)
        {
            LogProvider.WriteToLog("", ex);
        }
        /// <summary>
        /// Запись информации об исключении в файл журналирования настроек велосипеда PSN.
        /// </summary>
        /// <param name="ex">Исключение, информацию о котором нужно запротоколировать.</param>
        /// <param name="message">Сообщение для дополнительной информации в файле журналирования.</param>
        public static void WriteToPSNLog(this Exception ex, string message)
        {
            LogProvider.WriteToLog(message, ex);
        }

        /// <summary>
        /// Сериализует информацию об исключении в JSON нотации.
        /// </summary>
        /// <param name="ex">Текущее исключение.</param>
        /// <returns>Тащемта, можно отдавать сразу клиенту на обработку, например.</returns>
        public static string SerializeToJSON(this Exception ex)
        {
            string result = "{";
            result += "\"ErrorInfo\":{\"Message\":\"" + GetCustomExceptionMessage(ex.Message).Replace("\"", "\"\"") + "\",";
            result += "\"InnerExceptionMessage\":\"" + ((ex.InnerException == null) ? "" : ex.InnerException.Message.Replace("\"","\"\"") ) + "\",";
            result += "\"Source\":\"" + (string.IsNullOrEmpty(ex.Source) ? "" : ex.Source.Replace("\"", "\"\"")) + "\"";
            result += "}}";
            return result;
        }

        /// <summary>
        /// Получение пользовательского сообщения в исключении.
        /// </summary>
        /// <param name="message">Exception.Message</param>
        /// <returns></returns>
        public static string GetCustomExceptionMessage(string message)
        {
            string result = message.ToUpper();
            foreach (System.Collections.Generic.KeyValuePair<string,string> k in CUSTOM_EXCEPTION_MESSAGE)
            {
                if (message.Contains(k.Key))
                {
                    result = CUSTOM_EXCEPTION_MESSAGE[k.Key];
                    break;
                }
            }
            return result;
        }

        #endregion Исключения

        #region Веб-контекст

        /// <summary>
        /// Значение параметра (как GET так и POST) из веб-контекста. Имя параметра в контексте должно быть уникальным.
        /// </summary>
        /// <typeparam name="T">Тип запрашиваемого параметра.</typeparam>
        /// <param name="context">Текущий веб-контекст.</param>
        /// <param name="parameter_name">Имя параметра.</param>
        /// <returns>Значение параметра. Если параметр не найден, то значение по умолчанию для указанного типа.</returns>
        public static T GetParameter<T>(this HttpContext context, string parameter_name)
        {
            T result = default(T);
            System.Collections.Specialized.NameValueCollection values = WebSupplyFunctions.GetAllRequestParameters(context.Request);
            
            if (values.AllKeys.Contains(parameter_name)){
            try
                {
                    Type t = typeof(T);
                    switch (t.Name)
                    {
                        case "Int32":
                            result = (T)(object)Convert.ToInt32(values[parameter_name]);
                            break;
                        case "Single":
                            result = (T)(object)Convert.ToSingle(values[parameter_name]);
                            break;
                        case "Double":
                            result = (T)(object)Convert.ToDouble(values[parameter_name]);
                            break;
                        case "DateTime":
                            result = (T)(object)Convert.ToDateTime(values[parameter_name]);
                            break;
                        case "String":
                            result = (T)(object)values[parameter_name];
                            break;
                        default:
                            result = (T)(object)values[parameter_name];
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return result;
        }

        /// <summary>
        /// Сохранение на диск сервера файла, переданного на сервер посредством POST запроса.
        /// Например, с использованием fileuploader.js.
        /// </summary>
        /// <param name="context">Текущий веб-конекст.</param>
        /// <param name="full_file_path">Полный путь к файлу на сервере.
        /// Если такой уже существует - всё окончится печально для существовавшего.</param>
        public static void SaveUploadedFile(this HttpContext context, string full_file_path)
        {
            // Обработка файлов передающихся multipart
            if (context.Request.Files.Count > 0)
            {
                context.Request.Files[0].SaveAs(full_file_path);
                context.Request.InputStream.Flush();
                context.Request.InputStream.Close();
            }
            else // А это octet-stream
            {
                byte[] buffer = new byte[524288];
                using (FileStream fstream = new FileStream(full_file_path, FileMode.CreateNew, FileAccess.Write))
                {
                    int read;
                    while ((read = context.Request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fstream.Write(buffer, 0, read);
                    }
                }
                context.Request.InputStream.Flush();
                context.Request.InputStream.Close();
            }
        }
        #endregion Веб-контекст

        #region Общее
        /// <summary>
        /// Добавление элемента в коллекцию
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T AddTo<T>(this T self, ICollection<T> collection)
        {
            collection.Add(self);
            return self;
        }
        /// <summary>
        /// Проверка на вхождение элемента в заданную коллекцию
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsAnyOf<T>(this T self, params T[] collection)
        {
            return collection.Contains(self);
        }        
        #endregion
    }
}
