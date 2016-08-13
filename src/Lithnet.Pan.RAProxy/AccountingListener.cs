using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Pan.RAProxy
{
    class AccountingListener
    {

        /// <summary>
        /// Instantiate the listener service on the designated host and port
        /// </summary>
        public AccountingListener(int usePort = 1813)
        {
            // Structure for received data
            byte[] receiveByteArray;

            // Flag to indicate server is still receiving
            bool shutdown = false;

            // Create a TCP/IP socket for listener and responses
            UdpClient listener = new UdpClient(usePort);
            Socket sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // End point
            IPEndPoint sourceEP = new IPEndPoint(IPAddress.Any, usePort);

            // Listen for incoming connections.
            try
            {
                while (!shutdown)
                {
                    Debug.WriteLine("Server listening on port {0}.", usePort);
                    
                    receiveByteArray = listener.Receive(ref sourceEP);
                    Debug.WriteLine("Received packet.", sourceEP.Address.ToString());

                    // If this is a valid sized RADIUS packet, try to parse, otherwise silently ignore
                    if (receiveByteArray.Length >= 20)
                    {
                        byte[] response = ParseMessage(receiveByteArray, sourceEP.Address);
                        if (response.Length > 0)
                        {
                            sendSocket.SendTo(response, sourceEP);
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            listener.Close();
        }

        /// <summary>
        /// Handle a received AccountingRequest packet, passing any attributes to the necessary
        /// recording function and creating an acknowledgement response if the request was
        /// valid. If the request cannot be parsed or fails authentication, no response will
        /// be returned
        /// </summary>
        /// <param name="data">Incoming data packet</param>
        /// <param name="sender">Source IP address</param>
        /// <returns>Acknowledgement response data, if successfully parsed</returns>
        private byte[] ParseMessage(byte[] data, IPAddress sender)
        {
            byte requestType = data[0];                         // Type code is first 8 bits, 4 = AccountingRequest, 5 = AccountingResponse
            byte requestIdentifier = data[1];                   // Identifier is next 8 bits, representing sequence of message
            int requestLength = (data[2] << 8) | data[3];       // Length is next 16 bits, representing packet length

            // Determine if the packet contains Accounting-Request type code (4), otherwise do nothing
            if (data[0] != (byte)4)
            {
                Debug.WriteLine(" - Ignored: Not AccountingRequest type.");
                return null;
            }
            Debug.WriteLine(" - AccountingRequest #{0} with length {1}.", requestIdentifier, requestLength);

            // Check the authenticator token matches the shared secret, otherwise do nothing
            if (!AuthenticateRequest(data, sender))
            {
                Debug.WriteLine(" - Ignored: Invalid Authenticator Token.");
                return null;
            }

            // We're all good, store the attributes.
            int requestPosition = 20;
            int attributeType;
            int attributeLength;
            byte[] attributeBytes;
            Dictionary<int, byte[]> attributes = new Dictionary<int, byte []>();
            while (requestPosition+2 < requestLength-20)
            {
                attributeType = Convert.ToUInt16(data[requestPosition]);
                requestPosition++;
                attributeLength = Convert.ToUInt16(data[requestPosition]);
                requestPosition++;
                if (attributeLength > 0)
                {
                    attributeBytes = new byte[attributeLength - 2];
                    Array.Copy(data, requestPosition, attributeBytes, 0, attributeLength-2);
                    requestPosition += attributeLength - 2;
                }
                else
                {
                    attributeBytes = null;
                }

                attributes.Add(attributeType, attributeBytes);
                
            }

            // Send the attributes array on to the necessary interface
            AccountingRequest(sender, attributes);

            // Send a response acknowledgement
            byte[] responsePacket = new byte[20];
            responsePacket[0] = (byte)5;                      // Type code is 5 for response
            responsePacket[1] = requestIdentifier;            // Identifier is the same as sent in request
            short responseLength = 20;                        // Length of response message is 2 bytes

            responsePacket[3] = (byte)(responseLength & 0xff); ;
            responsePacket[2] = (byte)((responseLength >> 8) & 0xff);

            // Use the request authenticator initially to authenticate the response
            Array.Copy(data, 4, responsePacket, 4, 16);
            AuthenticateResponse(responsePacket, sender);

            return responsePacket;
        }

        /// <summary>
        /// Given an AccountingRequest packet, and the sender IP address, determine if the 16 byte authenticator token
        /// included after the 4 byte header is valid.
        /// </summary>
        /// <param name="data">AccountingRequest packet</param>
        /// <param name="sender">Source IP address</param>
        /// <returns>True if the authenticator token is valid</returns>
        private static bool AuthenticateRequest(byte[] data, IPAddress sender)
        {
            // Authenticator is 16 bit MD5 sum, starting at 5th byte
            byte[] requestAuthenticator = new byte[16];         
            Array.Copy(data, 4, requestAuthenticator, 0, 16);

            // Use the sender's IP to obtain the shared secret
            String secret = GetSecretByIP(sender);
            if (String.IsNullOrEmpty(secret))
                return false;

            // To obtain the MD5 authentication hash, we need to blank out the authenticator bits with zeros
            byte[] secretBytes = Encoding.ASCII.GetBytes(secret);
            byte[] hashableRequest = new byte[data.Length+secretBytes.Length];
            hashableRequest.Initialize();
            Array.Copy(data, 0, hashableRequest, 0, 4);
            Array.Copy(data, 20, hashableRequest, 20, data.Length - 20);
            Array.Copy(secretBytes, 0, hashableRequest, data.Length, secretBytes.Length);

            // Now apply the MD5 algorithm
            using (MD5 md5Hash = MD5.Create())
            {
                return requestAuthenticator.SequenceEqual(md5Hash.ComputeHash(hashableRequest));
            }
        }

        /// <summary>
        /// Modifies the AccountingResponse packet supplied so that the 16 byte request authenticator token
        /// following the 4 byte header is replaced with the calculated response authenticator token.
        /// </summary>
        /// <param name="response">Proposed response packet, with authenticator token from associated AccountingResponse packet</param>
        /// <param name="sender">Source IP address</param>
        private static void AuthenticateResponse(byte[] response, IPAddress sender)
        {
            // Authenticator token for response will be replaced based on the calculated hash
            byte[] responseAuthenticator;

            // Determine the shared secret to use from the sender's IP
            String secret = GetSecretByIP(sender);
            byte[] secretBytes = Encoding.ASCII.GetBytes(secret);

            // Obtain the MD5 authentication hash
            byte[] hashableResponse = new byte[response.Length + secretBytes.Length];
            Array.Copy(response, 0, hashableResponse, 0, response.Length);
            Array.Copy(secretBytes, 0, hashableResponse, response.Length, secretBytes.Length);

            using (MD5 md5Hash = MD5.Create())
            {
                responseAuthenticator = md5Hash.ComputeHash(hashableResponse);
            }

            // Replace the response authenticator token with the calculated result
            Array.Copy(responseAuthenticator, 0, response, 4, 16);
            
        }

        /// <summary>
        /// Boilerplate function to obtain the secret string per IP address
        /// </summary>
        /// <param name="sender">Source IP address</param>
        /// <returns>String representing the shared secret, if this is a valid IP</returns>
        private static String GetSecretByIP(IPAddress sender)
        {
            return "secret";
        }

        public static void AccountingRequest(IPAddress sender, Dictionary<int, byte[]> attributes)
        {
            foreach (var item in attributes)
            {
                // Get the string description of the attribute type
                String typeString = GetAttributeType(item.Key);
                if (String.IsNullOrEmpty(typeString))
                    typeString = "Type #" + item.Key.ToString();

                // Convert the attribute value to human-readable output
                String valueString;
                switch (item.Key)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 31:
                    case 44:
                        // String attributes
                        valueString = Encoding.ASCII.GetString(item.Value);
                        break;
                    case 5:
                    case 46:
                    case 41:
                        // Numeric attributes
                        valueString = BitConverter.ToUInt16(item.Value, 0).ToString();
                        break;
                    default:
                        // Unknown attributes
                        StringBuilder valueBuilder = new StringBuilder();
                        for (int i = 0; i < item.Value.Length; i++)
                        {
                            valueBuilder.Append(Convert.ToUInt16(item.Value[i]).ToString());
                        }
                        valueString = valueBuilder.ToString();
                        break;
                }
                Debug.WriteLine(" | Attribute: {0}, Value: {1}", typeString, valueString);
            }
        }

        public static String GetAttributeType(int attr)
        {
            switch (attr)
            {
                case 1:
                    return "User-Name";
                case 2:
                    return "User-Password";
                case 3:
                    return "CHAP-Password";
                case 4:
                    return "NAS-IP-Address";
                case 5:
                    return "NAS-Port";
                case 6:
                    return "Service-Type";
                case 7:
                    return "Framed-Protocol";
                case 8:
                    return "Framed-IP-Address";
                case 9:
                    return "Framed-IP-Netmask";
                case 10:
                    return "Framed-Routing";
                case 11:
                    return "Filter-Id";
                case 12:
                    return "Framed-MTU";
                case 13:
                    return "Framed-Compression";
                case 14:
                    return "Login-IP-Host";
                case 15:
                    return "Login-Service";
                case 16:
                    return "Login-TCP-Port";
                case 18:
                    return "Reply-Message";
                case 19:
                    return "Callback-Number";
                case 20:
                    return "Callback-Id";
                case 22:
                    return "Framed-Route";
                case 23:
                    return "Framed-IPX-Network";
                case 24:
                    return "State";
                case 25:
                    return "Class";
                case 26:
                    return "Vendor-Specific";
                case 27:
                    return "Session-Timeout";
                case 28:
                    return "Idle-Timeout";
                case 29:
                    return "Termination-Action";
                case 30:
                    return "Called-Station-Id";
                case 31:
                    return "Calling-Station-Id";
                case 32:
                    return "NAS-Identifier";
                case 33:
                    return "Proxy-State";
                case 34:
                    return "Login-LAT-Service";
                case 35:
                    return "Login-LAT-Node";
                case 36:
                    return "Login-LAT-Group";
                case 37:
                    return "Framed-AppleTalk-Link";
                case 38:
                    return "Framed-AppleTalk-Network";
                case 39:
                    return "Framed-AppleTalk-Zone";
                case 40:
                    return "Acct-Status-Type";
                case 41:
                    return "Acct-Delay-Time";
                case 42:
                    return "Acct-Input-Octets";
                case 43:
                    return "Acct-Output-Octets";
                case 44:
                    return "Acct-Session-Id";
                case 45:
                    return "Acct-Authentic";
                case 46:
                    return "Acct-Session-Time";
                case 47:
                    return "Acct-Input-Packets";
                case 48:
                    return "Acct-Output-Packets";
                case 49:
                    return "Acct-Terminate-Cause";
                case 50:
                    return "Acct-Multi-Session-Id";
                case 51:
                    return "Acct-Link-Count";
                case 60:
                    return "CHAP-Challenge";
                case 61:
                    return "NAS-Port-Type";
                case 62:
                    return "Port-Limit";
                case 63:
                    return "Login-LAT-Port";
                default:
                    return null;
            }
        }
        
    }
}
