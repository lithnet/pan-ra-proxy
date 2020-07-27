using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Web;
using System.Diagnostics;
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
#if DEBUG
                Trace.WriteLine("-----------------");
                Trace.WriteLine("XML Response");
                Trace.WriteLine(response);
                Trace.WriteLine("-----------------");
#endif
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
                List<Exception> exceptions = new List<Exception>();

                if (status == null)
                {
                    Logging.WriteEntry($"The API called failed with an unknown result\n{response}", EventLogEntryType.Error, Logging.EventIDUnknownApiException);
                    throw new PanApiException($"The API called failed with an unknown result", response);
                }

                if (status.InnerText == "success")
                {
                    return;
                }

                if (status.InnerText == "error")
                {
                    XmlNodeList xmlNodeList = d.SelectNodes("/response/msg/line/uid-response/payload/logout/entry");

                    if (xmlNodeList != null)
                    {
                        foreach (UserMappingException ex in Message.GetExceptions(xmlNodeList))
                        {
                            exceptions.Add(ex);
                            Logging.WriteEntry($"The logout user mapping for {ex.Username} with ip {ex.IPAddress} failed with message '{ex.Message}'", EventLogEntryType.Error, Logging.EventIDApiUserIDMappingLogoutFailed);
                        }
                    }

                    xmlNodeList = d.SelectNodes("/response/msg/line/uid-response/payload/login/entry");

                    if (xmlNodeList != null)
                    {
                        foreach (UserMappingException ex in Message.GetExceptions(xmlNodeList))
                        {
                            exceptions.Add(ex);
                            Logging.WriteEntry($"The login user mapping for {ex.Username} with ip {ex.IPAddress} failed with message '{ex.Message}'", EventLogEntryType.Error, Logging.EventIDApiUserIDMappingLoginFailed);
                        }
                    }

                    XmlNode node = d.SelectSingleNode("/response/result/msg");

                    if (node != null)
                    {
                        throw new PanApiException($"The API call failed with the following message\r\n {node.InnerText}");
                    }
                }

                if (exceptions.Count == 1)
                {
                    throw exceptions[0];
                }

                if (exceptions.Count > 1)
                {
                    throw new AggregateUserMappingException("Multiple user mapping operations failed", exceptions);
                }

                throw new PanApiException($"The API call failed with an unknown response\r\n{response}");
            }
            catch (AggregateException)
            {
                throw;
            }
            catch (PanApiException)
            {
                throw;
            }
            catch
            {
                Logging.WriteEntry($"An error occurred parsing the API response\n{response}", EventLogEntryType.Error, Logging.EventIDUnknownApiResponse);
                throw new PanApiException($"An error occurred parsing the API response", response);
            }
        }


        private static IList<UserMappingException> GetExceptions(XmlNodeList xmlNodeList)
        {
            List<UserMappingException> exceptions = new List<UserMappingException>();

            foreach (XmlNode entry in xmlNodeList)
            {
                UserMappingException e = GetException(entry);

                if (e != null)
                {
                    exceptions.Add(e);
                }
            }

            return exceptions;
        }

        private static UserMappingException GetException(XmlNode entry)
        {
            XmlAttributeCollection attributes = entry.Attributes;

            if (attributes == null)
            {
                return null;
            }

            UserMappingException e = new UserMappingException(
                attributes["message"]?.InnerText,
                attributes["name"]?.InnerText,
                attributes["ip"]?.InnerText);

            if (e.Message.Equals("delete mapping failed", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!Config.DebuggingEnabled)
                {
                    return null;
                }
            }

            Logging.CounterFailedMappingsPerSecond.Increment();

            return e;
        }

        /*\

                //<response status = 'error' code = '403'><result><msg>Invalid Credential</msg></result>

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

                Logging.WriteEntry($"The attempt to send the update to endpoint {ep.ApiUri} failed with a communications error\n{ex}\nThe service will attempt to fail over to the next endpoint", EventLogEntryType.Warning, Logging.EventIDApiEndpointExceptionWillFailover);
                Config.Failover();
                return this.Submit();
            }
        }

        private string Submit(PanApiEndpoint ep)
        {
            string messageText = this.SerializeObject();

            UriBuilder builder = new UriBuilder(ep.ApiUri);

            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);

            if (Config.ActiveEndPoint.UrlEncodeKey)
            {
                queryString["key"] = HttpUtility.UrlEncode(ep.ApiKey);
            }
            else
            {
                queryString["key"] = ep.ApiKey;
            }

            queryString["type"] = this.ApiType;

            builder.Query = queryString.ToString();

            HttpWebRequest request = this.GetRequestContent(builder.Uri, messageText);
            request.ServicePoint.Expect100Continue = false;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new HttpException((int) response.StatusCode, "The API call failed");
                }
                
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

#if DEBUG
            Trace.WriteLine("-----------------");
            Trace.WriteLine("XML Request");
            Trace.WriteLine(builder.ToString());
            Trace.WriteLine("-----------------");

#endif
            using (Stream rs = request.GetRequestStream())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
                rs.Write(bytes, 0, bytes.Length);
            }

            return request;
        }
    }
}