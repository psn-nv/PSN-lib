using System;
using System.Data;
using System.IO;

/// <summary>
/// Спасибо неизвестному товарищу, написавшему этот класс для работы с DBF -
/// теперь не придётся страдать фигней, настраивая ODBC и прочие OleDB..
/// </summary>
public class DBF
{
    /// <summary>
    /// Сохранение данных в файл формата dbf. Если уже файл существует, то он перезатирается.
    /// </summary>
    /// <param name="DT">Сохраняемые данные.</param>
    /// <param name="Folder">Каталог для сохранения файла. Имя файла определяется по имени сохраняемой таблицы.</param>
    public static void Save(DataTable DT, string Folder)
    {
        // Создаю таблицу
        string file_full_path = Path.Combine(Folder, string.Format("{0}.DBF", DT.TableName));
        if (File.Exists(file_full_path))
        {
            File.Delete(file_full_path);
        }        
        FileStream FS = new FileStream(file_full_path, FileMode.Create);
        // Формат dBASE III 2.0
        byte[] buffer = new byte[] { 0x03, 0x63, 0x04, 0x04 }; // Заголовок  4 байта
        FS.Write(buffer, 0, buffer.Length);
        buffer = new byte[]{
                       (byte)(((DT.Rows.Count % 0x1000000) % 0x10000) % 0x100),
                       (byte)(((DT.Rows.Count % 0x1000000) % 0x10000) / 0x100),
                       (byte)(( DT.Rows.Count % 0x1000000) / 0x10000),
                       (byte)(  DT.Rows.Count / 0x1000000)
                      }; // Word32 -> кол-во строк 5-8 байты
        FS.Write(buffer, 0, buffer.Length);
        int i = (DT.Columns.Count + 1) * 32 + 1; // Изврат
        buffer = new byte[]{
                       (byte)( i % 0x100),
                       (byte)( i / 0x100)
                      }; // Word16 -> кол-во колонок с извратом 9-10 байты
        FS.Write(buffer, 0, buffer.Length);
        string[] FieldName = new string[DT.Columns.Count]; // Массив названий полей
        string[] FieldType = new string[DT.Columns.Count]; // Массив типов полей
        byte[] FieldSize = new byte[DT.Columns.Count]; // Массив размеров полей
        byte[] FieldDigs = new byte[DT.Columns.Count]; // Массив размеров дробной части
        int s = 1; // Считаю длину заголовка
        foreach (DataColumn C in DT.Columns)
        {
            System.Text.Encoding src = System.Text.Encoding.GetEncoding(1251);
            System.Text.Encoding trg = System.Text.Encoding.GetEncoding(866);
            System.Text.Decoder dec = src.GetDecoder();
            byte[] ba = trg.GetBytes(C.ColumnName.ToUpper()); // Имя колонки
            int len = dec.GetCharCount(ba, 0, ba.Length);
            char[] ca = new char[len];
            dec.GetChars(ba, 0, ba.Length, ca, 0);
            string l = new string(ca);

            while (l.Length < 10) { l = l + (char)0; } // Подгоняю по размеру (10 байт)
            FieldName[C.Ordinal] = l.Substring(0, 10) + (char)0; // Результат
            FieldType[C.Ordinal] = "C";
            FieldSize[C.Ordinal] = 50;
            FieldDigs[C.Ordinal] = 0;
            switch (C.DataType.ToString())
            {
                case "System.String":
                    {
                        DataTable tmpDT = DT.Copy();
                        tmpDT.Columns.Add("StringLengthMathColumn", Type.GetType("System.Int32"), "LEN(" + C.ColumnName + ")");
                        DataRow[] DR = tmpDT.Select("", "StringLengthMathColumn DESC");
                        if (DR.Length > 0)
                        {
                            if (DR[0]["StringLengthMathColumn"].ToString() != "")
                            {
                                int n = (int)DR[0]["StringLengthMathColumn"];
                                if (n > 255)
                                    FieldSize[C.Ordinal] = 255;
                                else
                                    FieldSize[C.Ordinal] = (byte)n;
                            }
                            if (FieldSize[C.Ordinal] == 0)
                                FieldSize[C.Ordinal] = 1;
                        }
                        break;
                    }
                case "System.Boolean": { FieldType[C.Ordinal] = "L"; FieldSize[C.Ordinal] = 1; break; }
                case "System.Byte": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 1; break; }
                case "System.DateTime": { FieldType[C.Ordinal] = "D"; FieldSize[C.Ordinal] = 8; break; }
                case "System.Decimal": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 38; FieldDigs[C.Ordinal] = 5; break; }
                case "System.Double": { FieldType[C.Ordinal] = "F"; FieldSize[C.Ordinal] = 38; FieldDigs[C.Ordinal] = 5; break; }
                case "System.Int16": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 6; break; }
                case "System.Int32": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 11; break; }
                case "System.Int64": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 21; break; }
                case "System.SByte": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 6; break; }
                case "System.Single": { FieldType[C.Ordinal] = "F"; FieldSize[C.Ordinal] = 38; FieldDigs[C.Ordinal] = 5; break; }
                case "System.UInt16": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 6; break; }
                case "System.UInt32": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 11; break; }
                case "System.UInt64": { FieldType[C.Ordinal] = "N"; FieldSize[C.Ordinal] = 21; break; }
            }
            s = s + FieldSize[C.Ordinal];
        }
        buffer = new byte[]{
                       (byte)(s % 0x100),
                       (byte)(s / 0x100)
                      }; // Пишу длину заголовка 11-12 байты
        FS.Write(buffer, 0, buffer.Length);
        for (int j = 0; j < 20; j++) { FS.WriteByte(0x00); } // Пишу всякий хлам - 20 байт,
        // Итого: 32 байта - базовый заголовок DBF
        // Заполняю заголовок
        foreach (DataColumn C in DT.Columns)
        {
            buffer = System.Text.Encoding.Default.GetBytes(FieldName[C.Ordinal]); // Название поля
            FS.Write(buffer, 0, buffer.Length);
            buffer = new byte[]{
                        System.Text.Encoding.ASCII.GetBytes(FieldType[C.Ordinal])[0],
                        0x00,
                        0x00,
                        0x00,
                        0x00
                       }; // Размер
            FS.Write(buffer, 0, buffer.Length);
            buffer = new byte[]{
                        FieldSize[C.Ordinal],
                        FieldDigs[C.Ordinal]
                       }; // Размерность
            FS.Write(buffer, 0, buffer.Length);
            buffer = new byte[]{0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00}; // 14 нулей
            FS.Write(buffer, 0, buffer.Length);
        }
        FS.WriteByte(0x0D); // Конец описания таблицы
        System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("en-US", false).DateTimeFormat;
        System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
        string Spaces = "";
        while (Spaces.Length < 255) Spaces = Spaces + " ";
        foreach (DataRow R in DT.Rows)
        {
            FS.WriteByte(0x20); // Пишу данные
            foreach (DataColumn C in DT.Columns)
            {
                string l = R[C].ToString();
                if (l != "") // Проверка на NULL
                {
                    switch (FieldType[C.Ordinal])
                    {
                        case "L":
                            {
                                l = bool.Parse(l).ToString();
                                break;
                            }
                        case "N":
                            {
                                l = decimal.Parse(l).ToString(nfi);
                                break;
                            }
                        case "F":
                            {
                                l = float.Parse(l).ToString(nfi);
                                break;
                            }
                        case "D":
                            {
                                l = DateTime.Parse(l).ToString("yyyyMMdd", dfi);
                                break;
                            }
                        default: l = l.Trim() + Spaces; break;
                    }
                }
                else
                {
                    if (FieldType[C.Ordinal] == "C"
                     || FieldType[C.Ordinal] == "D")
                        l = Spaces;
                }
                while (l.Length < FieldSize[C.Ordinal]) { l = l + (char)0x00; }
                l = l.Substring(0, FieldSize[C.Ordinal]); // Корректирую размер
                buffer = System.Text.Encoding.GetEncoding(866).GetBytes(l); // Записываю в кодировке (MS-DOS Russian)
                FS.Write(buffer, 0, buffer.Length);
            }
        }
        FS.WriteByte(0x1A); // Конец данных
        FS.Close();
    }

    /// <summary>
    /// Загрузка данных из файла формата dbf. Без всяких проверок.
    /// </summary>
    /// <param name="FileName">Полный путь к обрабатываемому файлу.</param>
    /// <returns>Данные из файла</returns>
    public static DataTable Load(string FileName)
    {
        DataTable DT = new DataTable();
        FileStream FS = new System.IO.FileStream(FileName, FileMode.Open);
        byte[] buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
        FS.Position = 4; FS.Read(buffer, 0, buffer.Length);
        int RowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000);
        buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
        FS.Position = 8; FS.Read(buffer, 0, buffer.Length);
        int FieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
        string[] FieldName = new string[FieldCount]; // Массив названий полей
        string[] FieldType = new string[FieldCount]; // Массив типов полей
        byte[] FieldSize = new byte[FieldCount]; // Массив размеров полей
        byte[] FieldDigs = new byte[FieldCount]; // Массив размеров дробной части
        buffer = new byte[32 * FieldCount]; // Описание полей: 32 байтa * кол-во, начиная с 33-го
        FS.Position = 32; FS.Read(buffer, 0, buffer.Length);
        int FieldsLength = 0;
        for (int i = 0; i < FieldCount; i++)
        {
            // Заголовки
            FieldName[i] = System.Text.Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new char[] { (char)0x00 });
            FieldType[i] = "" + (char)buffer[i * 32 + 11];
            FieldSize[i] = buffer[i * 32 + 16];
            FieldDigs[i] = buffer[i * 32 + 17];
            FieldsLength = FieldsLength + FieldSize[i];
            // Создаю колонки
            switch (FieldType[i])
            {
                case "L": DT.Columns.Add(FieldName[i], Type.GetType("System.Boolean")); break;
                case "D": DT.Columns.Add(FieldName[i], Type.GetType("System.DateTime")); break;
                case "N":
                    {
                        if (FieldDigs[i] == 0)
                            DT.Columns.Add(FieldName[i], Type.GetType("System.Int32"));
                        else
                            DT.Columns.Add(FieldName[i], Type.GetType("System.Decimal"));
                        break;
                    }
                case "F": DT.Columns.Add(FieldName[i], Type.GetType("System.Double")); break;
                default: DT.Columns.Add(FieldName[i], Type.GetType("System.String")); break;
            }
        }
        FS.ReadByte(); // Пропускаю разделитель схемы и данных
        System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("en-US", false).DateTimeFormat;
        System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
        buffer = new byte[FieldsLength];
        DT.BeginLoadData();
        for (int j = 0; j < RowsCount; j++)
        {
            FS.ReadByte(); // Пропускаю стартовый байт элемента данных
            FS.Read(buffer, 0, buffer.Length);
            System.Data.DataRow R = DT.NewRow();
            int Index = 0;
            for (int i = 0; i < FieldCount; i++)
            {
                string l = System.Text.Encoding.GetEncoding(866).GetString(buffer, Index, FieldSize[i]).TrimEnd(new char[] { (char)0x00 }).TrimEnd(new char[] { (char)0x20 });
                Index = Index + FieldSize[i];
                if (l != "")
                    switch (FieldType[i])
                    {
                        case "L": R[i] = l == "T" ? true : false; break;
                        case "D": R[i] = DateTime.ParseExact(l, "yyyyMMdd", dfi); break;
                        case "N":
                            {
                                if (FieldDigs[i] == 0)
                                    R[i] = int.Parse(l, nfi);
                                else
                                    R[i] = decimal.Parse(l, nfi);
                                break;
                            }
                        case "F": R[i] = double.Parse(l, nfi); break;
                        default: R[i] = l; break;
                    }
                else
                    R[i] = DBNull.Value;
            }
            DT.Rows.Add(R);
        }
        DT.EndLoadData();
        FS.Close();
        return DT;
    }

    /// <summary>
    /// Дополнить существующий файл новыми данными.
    /// </summary>
    /// <param name="DT">Добавляемые данные.</param>
    /// <param name="FileName">Полный путь к файлу.</param>
    public static void Append(DataTable DT, string FileName)
    {
        DataTable table = DBF.Load(FileName);
        table.BeginLoadData();
        foreach (DataRow r in DT.Rows)
        {
            table.Rows.Add(r.ItemArray);
        }
        table.EndLoadData();
        table.TableName = Path.GetFileNameWithoutExtension(FileName);
        DBF.Save(table, Path.GetDirectoryName(FileName));
    }
}