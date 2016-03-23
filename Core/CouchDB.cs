// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;


namespace Framefield.Core
{
    public class CouchDB
    {
        public CouchDB()
        {
            ServerUrl = "http://localhost:5984";
        }

        public string ServerUrl { get; set; }

        public IEnumerable<string> GetDatabases()
        {
            string result = MakeRequest(ServerUrl + "/_all_dbs", "GET");

            var databases = JArray.Parse(result);
            foreach (var db in databases)
                yield return db.Value<string>();
        }

        public bool IsDBExisting(string dbName)
        {
            try
            {
                var databases = GetDatabases();
                databases.Single(s => s == dbName); // throws if not found
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void CreateDatabase(string db)
        {
            var result = MakeRequest(ServerUrl + "/" + db, "PUT");
            CheckIfResultIsOk(result, "Failed to create database: ");
        }

        public void DeleteDatabase(string db)
        {
            string result = MakeRequest(ServerUrl + "/" + db, "DELETE");
            CheckIfResultIsOk(result, "Failed to delete database: ");
        }

        private static void CheckIfResultIsOk(string result, string errorText)
        {
            var jsonResult = JObject.Parse(result);
            if (jsonResult["ok"].Value<bool>() == false)
                throw new ApplicationException(errorText + result);
        }

        /// <returns><id, rev> of created doc</returns>
        /// <throws>if creation was not successful</throws>
        public Tuple<string, string> StoreDocument(string db, string docID, string content)
        {
            var response = MakeRequest(GetDocumentParameterText(db, docID, string.Empty), "PUT", content, "application/json");
            ;
            var jsonResponse = JObject.Parse(response);
            docID = jsonResponse.Value<string>("id");
            var rev = jsonResponse.Value<string>("rev");
            return Tuple.Create(docID, rev);
        }

        public string GetDocument(string db, string docID, string docRev)
        {
            return MakeRequest(GetDocumentParameterText(db, docID, docRev), "GET");
        }

        private string GetDocumentParameterText(string db, string docID, string docRev)
        {
            return ServerUrl + "/" + db + "/" + docID + GetRevParameterText(docRev);
        }

        private static string GetRevParameterText(string docRev)
        {
            return (docRev != string.Empty ? ("?rev=" + docRev) : "");
        }

        public string DeleteDocument(string db, string docID, string docRev)
        {
            return MakeRequest(ServerUrl + "/" + db + "/" + docID + "?rev=" + docRev, "DELETE");
        }

        public string MakeRequest(string url, string method, string postData = null, string contentType = null)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = method;
            if (contentType != null)
                request.ContentType = contentType;

            if (postData != null)
            {
                byte[] bytes = UTF8Encoding.UTF8.GetBytes(postData);
                request.ContentLength = bytes.Length;
                using (Stream ps = request.GetRequestStream())
                {
                    ps.Write(bytes, 0, bytes.Length);
                }
            }

            var result = string.Empty;
            using (var response = request.GetResponse() as HttpWebResponse)
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }
    }
}
