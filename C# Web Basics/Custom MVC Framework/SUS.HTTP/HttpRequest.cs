﻿using SUS.HTTP.Enums;
using SUS.HTTP.GlobalConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SUS.HTTP
{
    public class HttpRequest
    {
        public static IDictionary<string, Dictionary<string, string>> Sessions = new Dictionary<string, Dictionary<string, string>>();

        public HttpRequest(string requestString)
        {
            this.Headers = new List<Header>();
            this.Cookies = new List<Cookie>();
            this.FormData = new Dictionary<string, string>();

            //We split the entire request to chunks
            var lines = requestString.Split(new string[] { HTTPConstants.NewLine }, System.StringSplitOptions.None);
            var headerLine = lines[0];

            var headerParts = headerLine.Split(' ');
            this.Method = (HttpMethod)Enum.Parse(typeof(HttpMethod), headerParts[0], true); //true -> ignore case makes it case insensitive GET = Get (from the Enum keys)
            this.Path = headerParts[1];

            var bodyBuilder = new StringBuilder();

            var index = 1;
            var isInHeader = true;

            while (index < lines.Length)
            {
                var currentLine = lines[index];

                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    isInHeader = false;
                    index++;
                    continue;
                }

                if (isInHeader)
                {
                    this.Headers.Add(new Header(currentLine));
                }
                else
                //Body
                {
                    bodyBuilder.AppendLine(currentLine);
                }

                index++;
            }

            if (this.Headers.Any(x => x.Name == HTTPConstants.RequestCookieHeader))
            {
                var coockiesAsString = this.Headers.FirstOrDefault(x => x.Name == HTTPConstants.RequestCookieHeader).Value;

                var cookies = coockiesAsString.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var cookie in cookies)
                {
                    this.Cookies.Add(new Cookie(cookie));
                }

            }

            //Session
            var sessionCookie = this.Cookies.FirstOrDefault(x => x.Name == HTTPConstants.SessionCookieName);

            if (sessionCookie == null)
            {
                var sessionId = Guid.NewGuid().ToString();
                var session = new Dictionary<string, string>();
                Sessions.Add(sessionId, session);

                this.Cookies.Add(new Cookie(HTTPConstants.SessionCookieName, sessionId));
            }
            //in case we close our page and then run it again
            else if (!Sessions.ContainsKey(sessionCookie.Value))
            {
                this.Session = new Dictionary<string, string>();
                Sessions.Add(sessionCookie.Value, this.Session as Dictionary<string, string>);
            }
            else
            {
                this.Session = Sessions[sessionCookie.Value];
            }

            //name = qqqqq & keyword = Tough & attack = 2 & health = 4 & description = qwq
            this.Body = bodyBuilder.ToString().TrimEnd('\n', '\r');
            var parameters = this.Body.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var parameter in parameters)
            {
                var parameterParts = parameter.Split(new[] { '=' }, 2);
                var name = parameterParts[0];
                var value = WebUtility.UrlDecode(parameterParts[1]);
                if (!this.FormData.ContainsKey(name))
                {
                    this.FormData.Add(name, value);
                }
            }
        }

        public string Path { get; set; }

        public HttpMethod Method { get; set; }

        public ICollection<Header> Headers { get; set; }

        public IDictionary<string, string> FormData { get; set; }

        public ICollection<Cookie> Cookies { get; set; }

        public IDictionary<string, string> Session { get; set; }

        public string Body { get; set; }

    }
}