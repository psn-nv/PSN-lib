using System;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace PSN
{
    /// <summary>
    /// Обеспечение ведения синхронного однопоточного журналирования. При использовании этого функционала необходимо создавать каталог для логов.
    /// </summary>
    public static class LogProvider
    {
        private const string log_extension = ".log";
        private static string logfiles_path = "Logs";

        /// <summary>
        /// Запись информации в лог-файл.
        /// </summary>
        /// <param name="_message">Пользовательское сообщение</param>
        public static void WriteToLog(string _message)
        {
            WriteToLog(_message, null, 2, true, true);
        }
        /// <summary>
        /// Запись информации в лог-файл.
        /// </summary>
        /// <param name="_message">Пользовательское сообщение</param>
        /// <param name="_ex">Информация об исключении</param>
        public static void WriteToLog(string _message, Exception _ex)
        {
            WriteToLog(_message, _ex, 2, true, true);
        }

        /// <summary>
        /// Запись информации в лог-файл. Используется для отладочного режима.
        /// </summary>
        /// <param name="_message">Пользовательское сообщение.</param>
        /// <param name="_isDebugMode">Сохранять ли пользовательское сообщение в файл?</param>
        public static void WriteToLog(string _message, bool _isDebugMode)
        {
            WriteToLog(_message, null, 2, true, _isDebugMode);
        }

        /// <summary>
        /// Запись информации в лог-файл.
        /// </summary>
        /// <param name="_message">Пользовательское сообщение</param>
        /// <param name="_max_file_size">Максимальный размер в МБайтах текущего лог-файла.</param>
        public static void WriteToLog(string _message, byte _max_file_size)
        {
            WriteToLog(_message, null, _max_file_size, true, true);
        }
        /// <summary>
        /// Запись информации в лог-файл.
        /// </summary>
        /// <param name="_message">Пользовательское сообщение</param>
        /// <param name="_ex">Информация об исключении</param>
        /// <param name="_max_log_size">Максимальный размер в МБайтах текущего лог-файла.</param>
        /// <param name="_need_time">Указывать ли в строке время, когда было записано сообщение.</param>
        /// <param name="_isDebugMode">Производить ли запись в файл вообще.</param>
        public static void WriteToLog(string _message, Exception _ex, byte _max_log_size, bool _need_time, bool _isDebugMode)
        {
            // в общем случае сообщение будет сохраняться в файл, но вот эта проверка сделана для отладочной версии этой функции.
            // чтоб каждый раз не выискивать в исходниках отладочные сообщения. Теперь достаточно задать значение одной переменной,
            // и вызывать запись в лог с этим значением.
            if (_isDebugMode) 
            {
                //string log_file = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + Path.DirectorySeparatorChar + "log.log";
                string log_file = GetLogNameForCurrentUser();

                if (log_file.Length == 0)
                {
                    throw new Exception("Файл логов не задан.");
                }

                string message_to_file = (_need_time) ? DateTime.Now.ToString("yyyyMMdd HHmmss") + " " : "";
                message_to_file += _message + " ";
                message_to_file += GetCurrentPage();

                if (_ex != null)
                {
                    message_to_file += _ex.Message;
                    message_to_file += (_ex.InnerException == null) ? "" : _ex.InnerException.Message + " ";
                    message_to_file += string.IsNullOrEmpty(_ex.Source) ? "" : _ex.Source;
                    message_to_file += Environment.NewLine + _ex.ToString();
                }

                try
                {
                    // если уже существует лог-файл и при необходимости требуется его усечение по размеру
                    // то будем производить проверку на этот самый размер (в МБ)
                    if (_max_log_size > 0 && File.Exists(log_file))
                    {
                        FileInfo finfo = new FileInfo(log_file);
                        if (finfo.Length > _max_log_size * 1024 * 1024)
                        {
                            // в случае, если этот самый размер превышен, то старый файл переименуем.
                            File.Move(log_file, Path.GetDirectoryName(log_file) + Path.DirectorySeparatorChar + "log" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log");
                        }
                    }

                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(log_file, true))
                    {
                        file.WriteLine(message_to_file);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        private static string GetLogNameForCurrentUser()
        {
            string result = "";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                result = Path.Combine(HttpContext.Current.Server.MapPath("~/"), logfiles_path);
                if (!Directory.Exists(result))
                {
                    Directory.CreateDirectory(result);
                }
                result = Path.Combine(result, HttpContext.Current.Request.LogonUserIdentity.Name.Replace('\\', '_') + log_extension);
            }
            else
            {
                result = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), logfiles_path);
                if (!Directory.Exists(result))
                {
                    Directory.CreateDirectory(result);
                }
                result = Path.Combine(result, Environment.UserName + log_extension);
            }
            return result;
        }

        /// <summary>
        /// Каталог логов для текущего окружения.
        /// </summary>
        /// <returns>Полный путь к каталогу логов.</returns>
        public static string GetPathForLogs()
        {
            string result = "";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                result = Path.Combine(HttpContext.Current.Server.MapPath("~/"), logfiles_path);
            }
            else
            {
                result = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), logfiles_path);
            }
            return result;
        }

        private static string GetCurrentPage()
        {
            string result = "";
            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.Url != null)
                {
                    if (HttpContext.Current.Request.Url != null && HttpContext.Current.Request.Url.PathAndQuery != null)
                    {
                        result += HttpContext.Current.Request.Url.PathAndQuery + " ";
                    }
                    if (HttpContext.Current.Request.UrlReferrer != null && HttpContext.Current.Request.UrlReferrer.PathAndQuery != null)
                    {
                        result += HttpContext.Current.Request.UrlReferrer.PathAndQuery + " ";
                    }
                }
            }
            catch (Exception ex)
            {
                result = " " + ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Информация о контексте среды выполнения.
        /// </summary>
        /// <returns>Для приложений на веб-сервере: адрес страницы, для обычных программ - </returns>
        public static string GetEnviromentContext()
        {
            string result = GetCurrentPage();
            
            result += String.IsNullOrEmpty(Environment.StackTrace) ? " " + Environment.StackTrace : "";
            result += String.IsNullOrEmpty(Environment.CurrentDirectory) ? " " + Environment.CurrentDirectory : "";
            result += String.IsNullOrEmpty(Environment.UserName) ? " " + Environment.UserName : "";
            result += (Environment.Version != null) ? " " + Environment.Version.ToString() : "";
            
            return result;
        }
    }
}
