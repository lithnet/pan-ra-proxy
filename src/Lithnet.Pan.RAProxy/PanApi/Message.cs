using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Web;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.Pan.RAProxy
{
    using System.Configuration;
    using System.Net.Http.Headers;
    using System.Xml;


    public abstract class Message
    {
        public abstract string ApiType { get; }

        public void Send()
        {
            string response = this.Submit();

            try
            {
                XmlDocument d = new XmlDocument();
                d.LoadXml(response);

                XmlNode status = d.SelectSingleNode("/response/@status");

                if (status != null)
                {
                    if (status.InnerText == "success")
                    {
                        return;
                    }
                    else
                    {
                        throw new PanApiException($"The API called failed with status {status.InnerText}", response);
                    }
                }
                else
                {
                    throw new PanApiException($"The API called failed with an unknown result", response);
                }
            }
            catch
            {
                throw new PanApiException($"The API called failed with an unsupported response", response);
            }
        }

        private string Submit()
        {
            string messageText = this.SerializeObject();
            UriBuilder builder = new UriBuilder(Config.BaseUri);

            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);

            queryString["key"] = HttpUtility.UrlEncode(Config.ApiKey);
            queryString["type"] = this.ApiType;

            builder.Query = queryString.ToString();

            HttpWebRequest request = this.GetRequestContent(builder.Uri, messageText);

            using (WebResponse response = request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);

                    return reader.ReadToEnd();
                }
            }
        }

        private HttpWebRequest GetRequestContent(Uri uri, string fileContent)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");

            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";


            StringBuilder builder = new StringBuilder();
            builder.AppendLine("--" + boundary);
            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

            builder.AppendFormat(headerTemplate, "file", "content.xml", "application/octet-stream");
            builder.AppendLine(fileContent);
            builder.AppendLine("--" + boundary + "--");

            using (Stream rs = request.GetRequestStream())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
                rs.Write(bytes, 0, bytes.Length);
            }

            return request;
        }
    }
}