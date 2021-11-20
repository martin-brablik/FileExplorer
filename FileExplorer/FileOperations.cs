using System;
using System.IO;
using System.Collections.Specialized;
using System.Windows;
using System.Diagnostics;

namespace FileExplorer
{
    public class FileOperations
    {
        public enum PasteMode
        {
            COPY,
            CUT
        }

        public delegate void Cut(string[] filePaths);
        public delegate void Copy(string[] filePaths);
        public delegate void Paste(PasteMode pasteMode, string destinationPath);
        public delegate void Rename(string filePath, string newName);
        public delegate void Delete(string filePath);

        public static bool IsValidPath(string path)
        {
            string[] subdirectories = path.Split('\\');
            if (subdirectories.Length == 0)
                return false;
            return true;
        }

        public static bool IsAbsolutePath(string path)
        {
            if(path.Length > 1)
            {
                if (path[1].Equals(':'))
                    return true;
            }
            return false;
        }

        public static void CopyFiles(string[] filePaths)
        {
            StringCollection files = new StringCollection();
            foreach (string file in filePaths)
                files.Add(file);
            Clipboard.SetFileDropList(files);
        }

        public static void MoveFiles(string[] filePaths)
        {
            DataObject data = new DataObject();
            StringCollection files = new StringCollection();
            foreach (string file in filePaths)
                files.Add(file);
            data.SetFileDropList(files);
            data.SetData("Preferred DropEffect", DragDropEffects.Move);

            Clipboard.SetDataObject(data, false);
        }

        public static void RenameFile(string filePath, string newName)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Move(filePath, Path.Combine(Path.GetFullPath(Path.GetDirectoryName(filePath)), newName));
                else if (Directory.Exists(filePath))
                    Directory.Move(filePath, Path.Combine(Path.GetFullPath(Path.GetDirectoryName(filePath)), newName));
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void DeleteFile(string filePath)
        {
            try
            {
                if(File.Exists(filePath))
                    File.Delete(filePath);
                else if(Directory.Exists(filePath))
                    Directory.Delete(filePath, true);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void PasteFile(PasteMode pasteMode, string destinationPath)
        {
            IDataObject dataObject = Clipboard.GetDataObject();
            try
            {
                if (dataObject.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])dataObject.GetData(DataFormats.FileDrop);
                    foreach (string fileName in files)
                    {
                        if (File.Exists(fileName))
                        {
                            if (pasteMode == PasteMode.COPY)
                                File.Copy(fileName, Path.Combine(destinationPath, Path.GetFileName(fileName)));
                            else
                                File.Move(fileName, Path.Combine(destinationPath, Path.GetFileName(fileName)));
                        }
                        else if (Directory.Exists(fileName))
                        {
                            CopyDirectory(fileName, destinationPath);
                            if (pasteMode == PasteMode.CUT)
                                Directory.Delete(fileName, true);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void ExecuteFile(string path)
        {
            if (!IsValidPath(path))
                return;
            if (File.Exists(path))
            {
                try
                {
                    Process.Start(path);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        public static void CopyDirectory(string directoryName, string destinationPath)
        {
            DirectoryInfo dir = new DirectoryInfo(directoryName);
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(Path.Combine(destinationPath, Path.GetFileName(directoryName)));
            FileInfo[] dirFiles = dir.GetFiles();
            foreach (FileInfo file in dirFiles)
            {
                string tempPath = Path.Combine(destinationPath, Path.GetFileName(directoryName), file.Name);
                file.CopyTo(tempPath, false);
            }
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destinationPath, Path.GetFileName(directoryName), subdir.Name);
                CopyDirectory(subdir.FullName, tempPath);
            }                
        }
    }
}
