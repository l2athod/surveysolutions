﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Views.BinaryData;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Repositories
{
    // TODO: Make it private
    public class ImageFileStorage : IImageFileStorage
    {
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly string basePath;
        private const string DataDirectoryName = "InterviewData";

        public ImageFileStorage(IFileSystemAccessor fileSystemAccessor, string rootDirectoryPath)
        {
            this.fileSystemAccessor = fileSystemAccessor;

            this.basePath = this.fileSystemAccessor.CombinePath(rootDirectoryPath, DataDirectoryName);

            if (!this.fileSystemAccessor.IsDirectoryExists(this.basePath))
                this.fileSystemAccessor.CreateDirectory(this.basePath);
        }

        public byte[] GetInterviewBinaryData(Guid interviewId, string fileName)
        {
            var filePath = this.GetPathToFile(interviewId, fileName);
            if (!fileSystemAccessor.IsFileExists(filePath))
                return null;
            return fileSystemAccessor.ReadAllBytes(filePath);
        }

        public List<InterviewBinaryDataDescriptor> GetBinaryFilesForInterview(Guid interviewId)
        {
            var directoryPath = this.GetPathToInterviewDirectory(interviewId);
            
            if (!fileSystemAccessor.IsDirectoryExists(directoryPath))
                return new List<InterviewBinaryDataDescriptor>();

            return fileSystemAccessor.GetFilesInDirectory(directoryPath)
                .Select(
                    fileName =>
                        new InterviewBinaryDataDescriptor(
                            interviewId, 
                            fileSystemAccessor.GetFileName(fileName),
                            null,
                            () => fileSystemAccessor.ReadAllBytes(fileName))).ToList();
        }

        public void StoreInterviewBinaryData(Guid interviewId, string fileName, byte[] data, string contentType)
        {
            var directoryPath = this.GetPathToInterviewDirectory(interviewId);
            
            if (!fileSystemAccessor.IsDirectoryExists(directoryPath))
                fileSystemAccessor.CreateDirectory(directoryPath);

            var filePath = this.GetPathToFile(interviewId, fileName);

            if (fileSystemAccessor.IsFileExists(filePath))
                fileSystemAccessor.DeleteFile(filePath);

            fileSystemAccessor.WriteAllBytes(filePath, data);
        }

        public void RemoveInterviewBinaryData(Guid interviewId, string fileName)
        {
            var filePath = this.GetPathToFile(interviewId, fileName);
            if (!fileSystemAccessor.IsFileExists(filePath))
                return;

            fileSystemAccessor.DeleteFile(filePath);
        }

        public string GetPath(Guid interviewId, string filename = null) => this.GetPathToFile(interviewId, filename);

        public void RemoveAllBinaryDataForInterview(Guid interviewId)
        {
            var directoryPath = this.GetPathToInterviewDirectory(interviewId);
            if (!fileSystemAccessor.IsDirectoryExists(directoryPath))
                return;

            fileSystemAccessor.DeleteDirectory(directoryPath);
        }

        private string GetPathToFile(Guid interviewId, string fileName)
        {
            return fileSystemAccessor.CombinePath(GetPathToInterviewDirectory(interviewId), fileName);
        }

        private string GetPathToInterviewDirectory(Guid interviewId, string baseDirectory=null)
        {
            return fileSystemAccessor.CombinePath(baseDirectory ?? basePath, interviewId.FormatGuid());
        }
    }
}