using System;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Web;
using System.Diagnostics;
using System.Net.Sockets;
using System.Xml;

namespace Lithnet.Pan.RAProxy
{
    public abstract class Message
    {
        public abstract string ApiType { get; }

        private int retryAttempts = 0;

        public void Send()
        {
            string response;

            try
            {
                response = this.Submit();
            }
            catch (Exception ex)
            {
                Logging.WriteEntry($"The attempt to send the update failed\n{ex.Message}\n{ex.Source}\n", EventLogEntryType.Error, Logging.EventIDMessageSendException);
                throw;
            }

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

                    if (status.InnerText == "error")
                    {
                        XmlNode message = d.SelectSingleNode("/response/msg/line/uid-response/payload/logout/entry/@message");

                        if (message != null)
                        {
                            if (message.InnerText.Equals("delete mapping failed", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!Config.DebuggingEnabled)
                                {
                                    return;
                                }
                            }
                        }
                    }

                    Logging.WriteEntry($"The API called failed with status {status.InnerText}\n{response}", EventLogEntryType.Error, Logging.EventIDApiException);
                    throw new PanApiException($"The API called failed with status {status.InnerText}", response);

                }
                else
                {
                    Logging.WriteEntry($"The API called failed with an unknown result\n{response}", EventLogEntryType.Error, Logging.EventIDUnknownApiException);
                    throw new PanApiException($"The API called failed with an unknown result", response);
                }
            }
            catch
            {
                Logging.WriteEntry($"The API called failed with an unsupported response\n{response}", EventLogEntryType.Error, Logging.EventIDUnknownApiResponse);
                throw new PanApiException($"The API called failed with an unsupported response", response);
            }
        }

        /*\
         
             <response status="error">
                 <msg>
                  <line>
                    <uid-response>
                        <version>2.0</version>
                        <payload>
                            <logout>
                               <entry name="fim-dev5\acollins" ip="49.127.66.17" message="Delete mapping failed"/>
                            </logout>
                        </payload>
                    </uid-response>
</line></msg></response>

 */

        private string Submit()
        {
            PanApiEndpoint ep = Config.ActiveEndPoint;

            try
            {
                return this.Submit(ep);
            }
            catch (Exception ex)
            {
                this.retryAttempts++;

                if (this.retryAttempts >= Config.ApiEndpoints.Count)
                {
                    throw;
                }

                Logging.WriteEntry($"The attempt to send the update to endpoint {ep.ApiUri} failed with a communciations error\n{ex.Message}\n{ex.Source}\nThe service will attempt to fail over to the next endpoint", EventLogEntryType.Warning, Logging.EventIDApiEndpointExceptionWillFailover);
                Config.Failover();
                return this.Submit();
            }
        }

        private string Submit(PanApiEndpoint ep)
        {
            string messageText = this.SerializeObject();

            UriBuilder builder = new UriBuilder(ep.ApiUri);

            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);

            queryString["key"] = HttpUtility.UrlEncode(ep.ApiKey);
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