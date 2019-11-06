﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;

namespace WB.UI.Shared.Web.Modules
{
    public static class ApiControllerExtensions
    {
        public static HttpResponseMessage BinaryResponseMessageWithEtag(this Controller controller, byte[] resultFile, string contentType = "image/png")
        {
            var stringEtag = GetEtagValue(resultFile);
            var etag = $"\"{stringEtag}\"";

            var incomingEtag = controller.Request.Headers[@"If-None-Match"];

            if (string.Compare(incomingEtag, etag, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return new HttpResponseMessage(HttpStatusCode.NotModified);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(resultFile)
            };

            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            response.Headers.ETag = new EntityTagHeaderValue(etag);
            return response;
        }

        private static string GetEtagValue(byte[] bytes)
        {
            using (var hasher = SHA1.Create())
            {
                var computeHash = hasher.ComputeHash(bytes);
                string hash = BitConverter.ToString(computeHash).Replace("-", "");
                return hash;
            }
        }
    }
}
