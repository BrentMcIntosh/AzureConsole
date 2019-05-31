using System;
using System.Configuration;
using System.IO;
using log4net;
using Microsoft.Azure; // Namespace for Azure Configuration Manager
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Model.Services
{

    public class FileStorageManager 
    {
        private readonly string _folderRoot;
        private readonly bool _cloudStorage;
        private readonly CloudBlobContainer _container = null;
        private readonly ILog _log;

        public bool UsingCloudStorage() => _cloudStorage;

        public FileStorageManager(string folderRoot, bool cloudStorage, ILog log)
        {
            _log = log;

            _folderRoot = folderRoot;

            _cloudStorage = cloudStorage;

            try
            {
                var connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

                var storageAccount = CloudStorageAccount.Parse(connectionString);

                CloudBlobClient fileClient = storageAccount.CreateCloudBlobClient();

                _container = fileClient.GetContainerReference("sentry");

                if (!_container.Exists())
                    throw new Exception("Azure Cloud Storage Share Not Found");
            }
            catch (Exception e)
            {
                _log.Error("Failed to initialize cloud storage", e);
            }

        }

        public bool Exists<T>(Document document)
        {
            if (_cloudStorage)
                return ExistsAzure<T>(document.FileName);
            return File.Exists(GetFullFileName<T>(document.FileName));
        }

        public string GetFilePath<T>(string baseFileName)
        {
            return GetFullFileName<T>(baseFileName);
        }

        public string CopyFile<TFrom, TTo>(string baseFileName)
        {
            var fromFullPath = GetFullFileName<TFrom>(baseFileName);
            if (File.Exists(fromFullPath))
            {
                string toFullPath = GetFullFileName<TTo>(baseFileName);
                File.Copy(fromFullPath, toFullPath, true);
            }
            return baseFileName;
        }

        public void DeleteFile<T>(Document document)
        {
            if (_cloudStorage || document.IsAzure)
                DeleteFileAzure<T>(document.FileName);
            DeleteFileBase<T>(document.FileName);
        }

        private void DeleteFileBase<T>(string baseFileName)
        {
            var filename = GetFullFileName<T>(baseFileName);
            if (File.Exists(filename))
                File.Delete(filename);
        }

        public MemoryStream GetFile<T>(Document document)
        {
            if (_cloudStorage || document.IsAzure)
                return GetFileAzure<T>(document.FileName);
            return GetFileBase<T>(document.FileName);
        }

        public MemoryStream GetLogoFile<T>(String fileName)
        {
            if (_cloudStorage)
                return GetFileAzure<T>(fileName);
            return GetFileBase<T>(fileName);
        }

        /// <remarks>
        /// This was throwing file not founds when I was debugging.
        /// </remarks>
        private MemoryStream GetFileBase<T>(string baseFileName)
        {
            var fullFileName = GetFullFileName<T>(baseFileName);

            MemoryStream memoryStream = new MemoryStream();

            if (File.Exists(fullFileName))
            {
                memoryStream = new MemoryStream(File.ReadAllBytes(fullFileName));
            }
            else
            {
                _log.Info($"{fullFileName} does not exist.");
            }

            return memoryStream;
        }

        public string StoreFile<T>(string originalName, Stream inputStream)
        {
            if (_cloudStorage)
                return StoreFileAzure<T>(originalName, inputStream);
            return StoreFileBase<T>(originalName, inputStream);
        }

        private string StoreFileBase<T>(string originalName, Stream inputStream)
        {
            string baseFileName = GenerateBaseFileName(originalName);
            string fullFileName = GetFullFileName<T>(baseFileName);

            var directoryRoot = Path.GetDirectoryName(fullFileName);
            if (!Directory.Exists(directoryRoot))
                Directory.CreateDirectory(directoryRoot);

            using (var fileStream = new FileStream(fullFileName, FileMode.Create))
                inputStream.CopyTo(fileStream);

            return baseFileName;
        }

        private static string GenerateBaseFileName(string originalName)
        {
            var fileName = Guid.NewGuid().ToString();
            var fileExt = Path.GetExtension(originalName);
            return fileName + fileExt;
        }

        private string GetFullFileName<T>(string baseFileName)
        {
            var folderBase = Path.Combine(_folderRoot, string.Format("Documents\\{0}", typeof(T).Name.ToLower()));
            return Path.Combine(folderBase, baseFileName);
        }

        public string GetBaseFilePath<T>()
        {
            return Path.Combine(_folderRoot, string.Format("Documents\\{0}", typeof(T).Name.ToLower()));
        }

        private string StoreFileAzure<T>(string originalName, Stream inputStream)
        {
            var typeDirName = typeof(T).Name.ToLower();
            string baseFileName = GenerateBaseFileName(originalName);
            // Ensure that the share exists.
            if (_container.Exists())
            {
                CloudBlockBlob blob = _container.GetBlockBlobReference(typeDirName + "/" + baseFileName);
                blob.UploadFromStream(inputStream);
            }

            return baseFileName;
        }

        private MemoryStream GetFileAzure<T>(string baseFileName)
        {
            var output = new MemoryStream();
            var typeDirName = typeof(T).Name.ToLower();
            // Ensure that the share exists.
            if (_container.Exists())
            {
                // Get a reference to the root directory for the share.
                CloudBlockBlob blob = _container.GetBlockBlobReference(typeDirName + "/" + baseFileName);
                if (blob.Exists())
                {
                    var fileStream = blob.OpenRead();
                    fileStream.CopyTo(output);
                    output.Seek(0, SeekOrigin.Begin);
                }
            }

            return output;
        }

        private void DeleteFileAzure<T>(string baseFileName)
        {
            var typeDirName = typeof(T).Name.ToLower();
            // Ensure that the share exists.
            if (!_container.Exists())
                return;
            CloudBlockBlob blob = _container.GetBlockBlobReference(typeDirName + "/" + baseFileName);
            if (blob.Exists())
            {
                blob.Delete();
            }
        }
        private bool ExistsAzure<T>(string baseFileName)
        {
            var typeDirName = typeof(T).Name.ToLower();
            // Ensure that the share exists.
            if (!_container.Exists())
                return false;
            // Get a reference to the root directory for the share.
            CloudBlockBlob blob = _container.GetBlockBlobReference(typeDirName + "/" + baseFileName);
            if (!blob.Exists())
                return false;
            return true;
        }

    }
}
