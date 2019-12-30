﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection.Repositories;

namespace WB.Core.BoundedContexts.Headquarters.Storage.AmazonS3
{
    public class S3FileStorage : IExternalFileStorage
    {
        private readonly AmazonS3Settings s3Settings;
        private readonly IAmazonS3 client;
        private readonly ITransferUtility transferUtility;
        private readonly string storageBasePath;
        private readonly ILogger log;

        public S3FileStorage(
            IOptions<AmazonS3Settings> s3Settings, 
            IOptions<HeadquarterOptions> headquarterOptions,
            IAmazonS3 amazonS3Client, 
            ITransferUtility transferUtility,
            ILoggerProvider loggerProvider)
        {
            log = loggerProvider.GetForType(GetType());
            this.s3Settings = s3Settings.Value;
            client = amazonS3Client;
            this.transferUtility = transferUtility;
            storageBasePath =  $"{s3Settings.Value.BasePath(headquarterOptions.Value.TenantName)}/";
        }

        private string GetKey(string key) => storageBasePath + key;

        public async Task<byte[]> GetBinaryAsync(string key)
        {
            try
            {
                var getObject = new GetObjectRequest
                {
                    BucketName = s3Settings.BucketName,
                    Key = GetKey(key)
                };

                using (var response = await client.GetObjectAsync(getObject).ConfigureAwait(false))
                {
                    using (var ms = new MemoryStream())
                    {
                        await response.ResponseStream.CopyToAsync(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                log.Trace($"Cannot get object from S3. [{e.StatusCode.ToString()}] {GetKey(key)}");
                return null;
            }
            catch (Exception e)
            {
                LogError($"Unable to get binary from {key}", e);
                throw;
            }
        }

        public async Task<List<FileObject>> ListAsync(string prefix)
        {
            try
            {
                var listObjects = new ListObjectsV2Request
                {
                    BucketName = s3Settings.BucketName,
                    Prefix = GetKey(prefix)
                };

                ListObjectsV2Response response = await client.ListObjectsV2Async(listObjects).ConfigureAwait(false);
                return response.S3Objects.Select(s3 => new FileObject
                {
                    Path = s3.Key.Substring(storageBasePath.Length),
                    Size = s3.Size,
                    LastModified = s3.LastModified
                }).ToList();
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                log.Trace($"Cannot list objects from S3. [{e.StatusCode.ToString()}] {GetKey(prefix)}");
                return null;
            }
            catch (Exception e)
            {
                LogError($"Unable to get list of object from S3 by prefix: {prefix}", e);
                throw;
            }
        }

        private void LogError(string message, Exception exception)
        {
            log.Error($"{message}. " +
                      $"Bucket: {s3Settings.BucketName}. " +
                      $"BasePath: {storageBasePath} " +
                      $"EndPoint: {s3Settings.Endpoint} ", exception);
        }

        public bool IsEnabled() => true;

        public string GetDirectLink(string key, TimeSpan expiration)
        {
            var protocol = string.IsNullOrWhiteSpace(s3Settings.Endpoint) 
                           || s3Settings.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? Protocol.HTTPS
                : Protocol.HTTP;

            return client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                Protocol = protocol,
                BucketName = s3Settings.BucketName,
                Key = GetKey(key),
                Expires = DateTime.UtcNow.Add(expiration)
            });
        }

        public FileObject Store(string key, Stream inputStream, string contentType, IProgress<int> progress = null)
        {
            try
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    BucketName = s3Settings.BucketName,
                    Key = GetKey(key),
                    ContentType = contentType,
                    AutoCloseStream = false,
                    AutoResetStreamPosition = false,
                    InputStream = inputStream
                };

                if (progress != null)
                {
                    uploadRequest.UploadProgressEvent += (sender, args) => { progress.Report(args.PercentDone); };
                }

                transferUtility.Upload(uploadRequest);

                return new FileObject
                {
                    Path = uploadRequest.Key,
                    Size = inputStream.Position,
                    LastModified = DateTime.UtcNow
                };
            }
            catch (Exception e)
            {
                LogError($"Unable to store object in S3. Path: {key}", e);
                throw;
            }
        }

        public async Task<FileObject> StoreAsync(string key, 
            Stream inputStream, 
            string contentType, 
            IProgress<int> progress = null)
        {
            try
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    BucketName = s3Settings.BucketName,
                    Key = GetKey(key),
                    ContentType = contentType,
                    AutoCloseStream = false,
                    AutoResetStreamPosition = false,
                    InputStream = inputStream
                };

                if (progress != null)
                {
                    uploadRequest.UploadProgressEvent += (sender, args) => { progress.Report(args.PercentDone); };
                }

                await transferUtility.UploadAsync(uploadRequest).ConfigureAwait(false);

                return new FileObject
                {
                    Path = uploadRequest.Key,
                    Size = inputStream.Position,
                    LastModified = DateTime.UtcNow
                };
            }
            catch (Exception e)
            {
                LogError($"Unable to store object in S3. Path: {key}", e);
                throw;
            }
        }

        public FileObject Store(string path, byte[] data, string contentType, IProgress<int> progress = null)
        {
            using (var ms = new MemoryStream(data))
            {
                return Store(path, ms, contentType, progress);
            }
        }

        public async Task RemoveAsync(string path)
        {
            try
            {
                await client.DeleteObjectAsync(s3Settings.BucketName, GetKey(path)).ConfigureAwait(false);
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // ignore
            }
            catch (Exception e)
            {
                LogError($"Unable to remove object in S3. Path: {path}", e);
                throw;
            }
        }
    }
}
