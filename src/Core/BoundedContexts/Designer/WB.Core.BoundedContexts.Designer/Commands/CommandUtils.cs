﻿using CsQuery.ExtensionMethods.Internal;
using Ganss.XSS;

namespace WB.Core.BoundedContexts.Designer.Commands
{
    internal class CommandUtils
    {
        public static string SanitizeHtml(string html, bool removeAllTags = false)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            var sanitizer = new HtmlSanitizer {KeepChildNodes = true};

            if (!removeAllTags)
            {
                sanitizer.AllowedTags.Clear();
                sanitizer.AllowedTags.AddRange(new[]
                {
                    "u", "s", "i", "b", "br", "font", "tt", "big", "strong", "small", "sup", "sub", "blockquote",
                    "cite", "dfn", "p", "em"
                });
                sanitizer.AllowedAttributes.Clear();
                sanitizer.AllowedAttributes.AddRange(new[] {"color", "size"});
            }
            else
            {
                sanitizer.AllowedTags.Clear();
                sanitizer.AllowedAttributes.Clear();
            }

            string sanitizedHtml = html;
            bool wasChanged = true;
            while (wasChanged)
            {
                var temp = System.Web.HttpUtility.HtmlDecode(sanitizer.Sanitize(sanitizedHtml)).Trim();
                wasChanged = sanitizedHtml != temp;
                sanitizedHtml = temp;
            }
            return sanitizedHtml;
        }
    }
}
