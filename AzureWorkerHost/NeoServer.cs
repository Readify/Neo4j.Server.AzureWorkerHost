﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using AzureWorkerHost.AzureMocks;
using AzureWorkerHost.Diagnostics;
using AzureWorkerHost.Legacy;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureWorkerHost
{
    public class NeoServer
    {
        readonly NeoServerConfiguration configuration;
        readonly IRoleEnvironment roleEnvironment;
        readonly ICloudBlobClient cloudBlobClient;
        readonly IFileSystem fileSystem;
        readonly IZipHandler zipHandler;

        internal readonly NeoRuntimeContext Context = new NeoRuntimeContext();

        public IList<ILogger> Loggers { get; private set; }

        public NeoServer(
            NeoServerConfiguration configuration,
            IRoleEnvironment roleEnvironment,
            ICloudBlobClient cloudBlobClient,
            IFileSystem fileSystem,
            IZipHandler zipHandler)
        {
            this.configuration = configuration;
            this.roleEnvironment = roleEnvironment;
            this.cloudBlobClient = cloudBlobClient;
            this.fileSystem = fileSystem;
            this.zipHandler = zipHandler;

            Loggers = new List<ILogger>(new[] { new TraceLogger() });
        }

        public NeoServer(NeoServerConfiguration configuration, CloudStorageAccount storageAccount)
            : this(configuration,
                new RoleEnvironmentWrapper(),
                new CloudBlobClientWrapper(storageAccount.CreateCloudBlobClient()),
                new FileSystem(),
                new ZipHandler())
        {}

        public NeoServer(CloudStorageAccount storageAccount)
            : this(
                new NeoServerConfiguration(),
                storageAccount)
        {}

        public void DownloadAndInstall()
        {
            InitializeLocalResource();
            DownloadJava();
        }

        internal void InitializeLocalResource()
        {
            Loggers.WriteLine("Initializing local resource: {0}", configuration.NeoLocalResourceName);

            ILocalResource localResource;
            try
            {
                localResource = roleEnvironment.GetLocalResource(configuration.NeoLocalResourceName);
            }
            catch (RoleEnvironmentException ex)
            {
                var exceptionToThrow = new ApplicationException(
                    string.Format(ExceptionMessages.NeoLocalResourceNotFound, configuration.NeoLocalResourceName),
                    ex);
                Loggers.Fail(exceptionToThrow, "Local resource initialization failed");
                throw exceptionToThrow;
            }
            Context.LocalResourcePath = localResource.RootPath;
            Loggers.WriteLine("Local resource path for '{0}' is: {1}", configuration.NeoLocalResourceName, Context.LocalResourcePath);
        }

        internal void DownloadJava()
        {
            DownloadArtifact(
                "Java Runtime Environment",
                ExceptionMessages.JavaArtifactPreparationHint,
                configuration.JavaBlobName,
                configuration.JavaDirectoryName);
        }

        internal void DownloadArtifact(
            string friendlyName,
            string artifactPreparationHint,
            string blobName,
            string targetDirectory)
        {
            Loggers.WriteLine("Downloading {0} from {1}", friendlyName, blobName);
            var blobAddress = cloudBlobClient.BaseUri.Append(blobName).AbsoluteUri;
            Loggers.WriteLine("Full blob URI is {0}", blobAddress);
            
            var blob = cloudBlobClient.GetBlobReference(blobAddress);

            var fileNameComponent = Path.GetFileName(blob.Uri.LocalPath);
            if (fileNameComponent == null)
                throw new InvalidOperationException(string.Format(
                    ExceptionMessages.PathMissingFileNameComponent,
                    blob.Uri));

            var filePathOnDisk = Path.Combine(Context.LocalResourcePath, fileNameComponent);
            Loggers.WriteLine("File path on disk will be {0}", filePathOnDisk);

            if (fileSystem.File.Exists(filePathOnDisk))
            {
                Loggers.WriteLine("File exists; deleting");
                fileSystem.File.Delete(filePathOnDisk);
            }

            Loggers.WriteLine("Downloading {0} to disk", friendlyName);
            try
            {
                blob.DownloadToFile(filePathOnDisk);
            }
            catch (StorageClientException ex)
            {
                var exceptionToThrow = new ApplicationException(
                    string.Format(ExceptionMessages.ArtifactBlobDownloadFailed, friendlyName, blobAddress, artifactPreparationHint),
                    ex);
                Loggers.Fail(exceptionToThrow, "Failed to download {0}", friendlyName);
                throw exceptionToThrow;
            }
            Loggers.WriteLine("Downloaded {0} to disk", friendlyName);

            var directoryPathOnDisk = Path.Combine(Context.LocalResourcePath, targetDirectory);
            Loggers.WriteLine("Unzipping artifact to {0}", directoryPathOnDisk);
            zipHandler.Extract(filePathOnDisk, directoryPathOnDisk);
            Loggers.WriteLine("Unzipped artifact to {0}", directoryPathOnDisk);
        }
    }
}
