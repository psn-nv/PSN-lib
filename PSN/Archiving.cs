using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Packaging;

namespace PSN
{
    /// <summary>
    /// Поддержка создания архивов
    /// </summary>
    public static class Archiving
    {
        private const string ERROR_UNKNOWN_FORMAT = "Unknown archive format!";
        private const string ERROR_NOT_IMPLEMENTED_YET = "Format realization not implemented yet!";

        private const long BUFFER_SIZE = 4096;
        
        /// <summary>
        /// Формат архивного файла.
        /// </summary>
        public enum ArchiveFormats
        {
            /// <summary>
            /// ZIP
            /// </summary>
            ZIP = 0,
        }

        /// <summary>
        /// Упаковка файла в архив нужного формата.
        /// </summary>
        /// <param name="file_path">Путь к упаковываемому файлу.</param>
        /// <param name="archive_path">Путь к создаваемому/дополняемому архиву.</param>
        /// <param name="format">Формат архива.</param>
        /// <param name="is_delete_after">Удалять ли файл после его архивирования.</param>
        public static void ArchiveFile(string file_path, string archive_path, ArchiveFormats format, bool is_delete_after)
        {
            switch (format)
            {
                case ArchiveFormats.ZIP:
                    AddFileToZip(archive_path, file_path);
                    break;
                default:
                    throw new Exception(ERROR_UNKNOWN_FORMAT);
            }
            if (is_delete_after)
            {
                try
                {
                    File.Delete(file_path);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Упаковка файла в архив нужного формата. Исходный файл не удаляется.
        /// </summary>
        /// <param name="file_path">Полный путь к упаковываемому файлу.</param>
        /// <param name="archive_path">Полный путь к создаваемому/дополняемому архиву.</param>
        /// <param name="format">Формат архива.</param>
        public static void ArchiveFile(string file_path, string archive_path, ArchiveFormats format)
        {
            ArchiveFile(file_path, archive_path, format, false);
        }

        /// <summary>
        /// Упаковка файлов в архив нужного формата.
        /// </summary>
        /// <param name="files_path">Список полных путей к файлам для упаковки.</param>
        /// <param name="archive_path">Полный путь к создаваемому/дополняемому архиву.</param>
        /// <param name="format">Формат архива.</param>
        public static void ArchiveFiles(List<string> files_path, string archive_path, ArchiveFormats format)
        {                        
            foreach (string fname in files_path)
            {
                ArchiveFile(fname, archive_path, format);
            }
        }

        private static void AddFileToZip(string zip_file_name, string adding_file_path)
        {
            try
            {
                using (Package zip = System.IO.Packaging.Package.Open(zip_file_name, FileMode.OpenOrCreate))
                {
                    string destFilename = ".\\" + Path.GetFileName(adding_file_path);

                    Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                    if (zip.PartExists(uri))
                    {
                        zip.DeletePart(uri);
                    }
                    PackagePart part = zip.CreatePart(uri, "", CompressionOption.Normal);
                    using (FileStream fileStream = new FileStream(adding_file_path, FileMode.Open, FileAccess.Read))
                    {
                        using (Stream dest = part.GetStream())
                        {
                            CopyStream(fileStream, dest);
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex;}
        }

        private static void CopyStream(System.IO.FileStream input_stream, System.IO.Stream output_stream)
        {
            long bufferSize = input_stream.Length < BUFFER_SIZE ? input_stream.Length : BUFFER_SIZE;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;
            long bytesWritten = 0;
            while ((bytesRead = input_stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                output_stream.Write(buffer, 0, bytesRead); bytesWritten += bufferSize;
            }
        }
    }
}
