using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Lithnet.Pan.RAProxy
{
    public class RadiusAttribute
    {
        // Type of attribute
        public RadiusAttributeType Type { get; set; }

        // Value of attribute received, could be String, Integer, or something else, depending on type
        public object Value { get; set; }

        // Native datatypes for value
        public string ValueAsString { get; set; }

        public uint ValueAsInt { get; set; }

        public byte[] ValueAsByteArray { get; set; }

        public IPAddress ValueAsIPAddress { get; set; }

        private RadiusAttribute()
        {
        }

        /// <summary>
        /// Given a block of attribute data including type byte and length byte, followed
        /// by raw value bytes, return an interpreted RadiusAttribute object.
        /// </summary>
        /// <param name="rawAttributeBlock">Block of bytes representing a single attribute, including type and length header. (Size >= 2)</param>
        /// <returns>Parsed RadiusAttribute object</returns>
        public static RadiusAttribute ParseAttributeBlock(byte[] rawAttributeBlock)
        {
            if (rawAttributeBlock.Length < 2)
                throw new InvalidRadiusAttributeException($"Invalid attribute block size. Expected 2 or more bytes, actual was {rawAttributeBlock.Length}.");

            try
            {
                int attributeType = Convert.ToUInt16(rawAttributeBlock[0]);
                int attributeLength = Convert.ToUInt16(rawAttributeBlock[1]);
                byte[] attributeValue = new byte[attributeLength - 2];
                if (attributeLength > 2 && attributeLength <= rawAttributeBlock.Length)
                {
                    Array.Copy(rawAttributeBlock, 2, attributeValue, 0, attributeLength - 2);
                }
                else
                {
                    attributeValue = null;
                }

                return ParseAttributeValue(attributeType, attributeValue);
            }
            catch (Exception e)
            {
                throw new InvalidRadiusAttributeException($"Invalid attribute block content: {e.Message}");
            }
        }

        public override string ToString()
        {
            return GetAttributeTypeString(this.Type) + ": " + this.ValueAsString;
        }

        /// <summary>
        /// Given a type code and value block as bytes, parse the value according to the data definition for
        /// RADIUS attributes (RFC2865/RFC2866) and return a new RadiusAttribute entity.
        /// </summary>
        /// <param name="type">Integer type code</param>
        /// <param name="value">Raw value bytes</param>
        /// <returns>New AttributeValue entity if parsed successfully</returns>
        public static RadiusAttribute ParseAttributeValue(int type, byte[] value)
        {
            RadiusAttribute newAttribute = new RadiusAttribute();
            newAttribute.Type = (RadiusAttributeType)type;

            if (value?.Length > 0)
            {
                try
                {
                    // Use the type to determine the expected data in the value
                    RadiusAttributeValueDatatype datatype = GetAttributeValueDatatype(newAttribute.Type);

                    // Populate the types as best as possible
                    newAttribute.ValueAsByteArray = value;
                    switch (datatype)
                    {
                        case RadiusAttributeValueDatatype.String:
                        case RadiusAttributeValueDatatype.EncryptedString:
                            newAttribute.Value = Encoding.ASCII.GetString(value);
                            newAttribute.ValueAsString = (string)newAttribute.Value;
                            break;

                        case RadiusAttributeValueDatatype.Integer:
                            Array.Reverse(value);
                            newAttribute.Value = BitConverter.ToUInt32(value, 0);
                            newAttribute.ValueAsInt = BitConverter.ToUInt32(value, 0);
                            newAttribute.ValueAsString = newAttribute.ValueAsInt.ToString();
                            break;

                        case RadiusAttributeValueDatatype.IP:
                            newAttribute.Value = new IPAddress(value);
                            newAttribute.ValueAsIPAddress = (IPAddress)newAttribute.Value;
                            newAttribute.ValueAsString = newAttribute.ValueAsIPAddress.ToString();
                            break;

                        case RadiusAttributeValueDatatype.ByteArray:
                        default:
                            newAttribute.Value = value;
                            newAttribute.ValueAsString = Convert.ToBase64String(value);
                            break;
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidRadiusAttributeException($"Unable to parse attribute value data: {e.Message}", e);
                }
            }

            return newAttribute;
        }

        /// <summary>
        /// Given a block of bytes that contains multiple RADIUS attributes within it, parse this into
        /// an array of useable RadiusAttribute entities.
        /// </summary>
        /// <param name="rawAttributeMessage">Raw bytes containing RADIUS attributes, as received</param>
        /// <param name="startIndex">Index of byte array marking start of attributes</param>
        /// <returns></returns>
        public static List<RadiusAttribute> ParseAttributeMessage(byte[] rawAttributeMessage, int startIndex = 0)
        {
            List<RadiusAttribute> result = new List<RadiusAttribute>();

            int byteIndex = startIndex;
            int attributeType;
            int attributeLength;
            byte[] attributeBytes;
            Dictionary<int, byte[]> attributes = new Dictionary<int, byte[]>();
            while (byteIndex + 2 <= rawAttributeMessage.Length)
            {
                attributeType = Convert.ToUInt16(rawAttributeMessage[byteIndex]);
                attributeLength = Convert.ToUInt16(rawAttributeMessage[byteIndex + 1]);
                if (attributeLength > 2 && attributeLength + byteIndex <= rawAttributeMessage.Length)
                {
                    attributeBytes = new byte[attributeLength - 2];
                    Array.Copy(rawAttributeMessage, byteIndex + 2, attributeBytes, 0, attributeLength - 2);
                    byteIndex += attributeLength;
                }
                else
                {
                    attributeBytes = null;
                    byteIndex += 2;
                }

                RadiusAttribute attribute = ParseAttributeValue(attributeType, attributeBytes);
                result.Add(attribute);
            }

            return result;
        }

        /// <summary>
        /// Indicates whether one or more attributes must be passed back in the accounting response
        /// if this attribute is found in the accounting request packet.
        /// </summary>
        /// <returns>True if this attribute triggers a response</returns>
        public bool IsRequiredInResponse()
        {
            switch (this.Type)
            {
                case RadiusAttributeType.ProxyState:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns any response attribute to be returned in an Accounting-Response packet
        /// based on this attribute being received in the Accounting-Request
        /// </summary>
        /// <returns>Byte array to be included in the response</returns>
        public byte[] GetResponse()
        {
            switch (this.Type)
            {
                // ProxyState needs to be added to the response unmodified
                case RadiusAttributeType.ProxyState:
                    byte[] responseData = new byte[this.ValueAsByteArray.Length + 2];
                    // Add the type code
                    responseData[0] = (byte)this.Type;

                    // Add the length of the attribute (value + type + length)
                    responseData[1] = (byte)(this.ValueAsByteArray.Length + 2);

                    // Add the data
                    Array.Copy(this.ValueAsByteArray, 0, responseData, 2, this.ValueAsByteArray.Length);

                    return responseData;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Determine the datatype based on the given attribute type.
        /// </summary>
        /// <param name="type">Attribute type integer</param>
        /// <returns>Datatype expected</returns>
        private static RadiusAttributeValueDatatype GetAttributeValueDatatype(RadiusAttributeType type)
        {
            switch (type)
            {
                case RadiusAttributeType.UserName:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.UserPassword:
                    return RadiusAttributeValueDatatype.EncryptedString;
                case RadiusAttributeType.CHAPPassword:
                    return RadiusAttributeValueDatatype.ByteArray;
                case RadiusAttributeType.NASIPAddress:
                    return RadiusAttributeValueDatatype.IP;
                case RadiusAttributeType.NASPort:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.ServiceType:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.FramedProtocol:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.FramedIPAddress:
                    return RadiusAttributeValueDatatype.IP;
                case RadiusAttributeType.FramedIPNetmask:
                    return RadiusAttributeValueDatatype.IP;
                case RadiusAttributeType.FramedRouting:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.FilterId:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.FramedMTU:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.FramedCompression:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.LoginIPHost:
                    return RadiusAttributeValueDatatype.IP;
                case RadiusAttributeType.LoginService:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.LoginTCPPort:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.ReplyMessage:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.CallbackNumber:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.CallbackId:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.FramedRoute:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.FramedIPXNetwork:
                    return RadiusAttributeValueDatatype.IP;
                case RadiusAttributeType.State:
                    return RadiusAttributeValueDatatype.ByteArray;
                case RadiusAttributeType.Class:
                    return RadiusAttributeValueDatatype.ByteArray;
                case RadiusAttributeType.VendorSpecific:
                    return RadiusAttributeValueDatatype.ByteArray;
                case RadiusAttributeType.SessionTimeout:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.IdleTimeout:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.TerminationAction:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.CalledStationId:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.CallingStationId:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.NASIdentifier:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.ProxyState:
                    return RadiusAttributeValueDatatype.ByteArray;
                case RadiusAttributeType.LoginLATService:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.LoginLATNode:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.LoginLATGroup:
                    return RadiusAttributeValueDatatype.ByteArray;
                case RadiusAttributeType.FramedAppleTalkLink:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.FramedAppleTalkNetwork:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.FramedAppleTalkZone:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.AcctStatusType:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctDelayTime:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctInputOctets:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctOutputOctets:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctSessionId:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.AcctAuthentic:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctSessionTime:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctInputPackets:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctOutputPackets:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctTerminateCause:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.AcctMultiSessionId:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.AcctLinkCount:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.CHAPChallenge:
                    return RadiusAttributeValueDatatype.ByteArray;
                case RadiusAttributeType.NASPortType:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.PortLimit:
                    return RadiusAttributeValueDatatype.Integer;
                case RadiusAttributeType.LoginLATPort:
                    return RadiusAttributeValueDatatype.String;
                case RadiusAttributeType.FramedIPv6Address:
                    return RadiusAttributeValueDatatype.IP;
                default:
                    return RadiusAttributeValueDatatype.ByteArray;
            }
        }

        public static string GetAttributeTypeString(RadiusAttributeType type)
        {
            switch (type)
            {
                case RadiusAttributeType.UserName:
                    return "User-Name";
                case RadiusAttributeType.UserPassword:
                    return "User-Password";
                case RadiusAttributeType.CHAPPassword:
                    return "CHAP-Password";
                case RadiusAttributeType.NASIPAddress:
                    return "NAS-IP-Address";
                case RadiusAttributeType.NASPort:
                    return "NAS-Port";
                case RadiusAttributeType.ServiceType:
                    return "Service-Type";
                case RadiusAttributeType.FramedProtocol:
                    return "Framed-Protocol";
                case RadiusAttributeType.FramedIPAddress:
                    return "Framed-IP-Address";
                case RadiusAttributeType.FramedIPNetmask:
                    return "Framed-IP-Netmask";
                case RadiusAttributeType.FramedRouting:
                    return "Framed-Routing";
                case RadiusAttributeType.FilterId:
                    return "Filter-Id";
                case RadiusAttributeType.FramedMTU:
                    return "Framed-MTU";
                case RadiusAttributeType.FramedCompression:
                    return "Framed-Compression";
                case RadiusAttributeType.LoginIPHost:
                    return "Login-IP-Host";
                case RadiusAttributeType.LoginService:
                    return "Login-Service";
                case RadiusAttributeType.LoginTCPPort:
                    return "Login-TCP-Port";
                case RadiusAttributeType.ReplyMessage:
                    return "Reply-Message";
                case RadiusAttributeType.CallbackNumber:
                    return "Callback-Number";
                case RadiusAttributeType.CallbackId:
                    return "Callback-Id";
                case RadiusAttributeType.FramedRoute:
                    return "Framed-Route";
                case RadiusAttributeType.FramedIPXNetwork:
                    return "Framed-IPX-Network";
                case RadiusAttributeType.State:
                    return "State";
                case RadiusAttributeType.Class:
                    return "Class";
                case RadiusAttributeType.VendorSpecific:
                    return "Vendor-Specific";
                case RadiusAttributeType.SessionTimeout:
                    return "Session-Timeout";
                case RadiusAttributeType.IdleTimeout:
                    return "Idle-Timeout";
                case RadiusAttributeType.TerminationAction:
                    return "Termination-Action";
                case RadiusAttributeType.CalledStationId:
                    return "Called-Station-Id";
                case RadiusAttributeType.CallingStationId:
                    return "Calling-Station-Id";
                case RadiusAttributeType.NASIdentifier:
                    return "NAS-Identifier";
                case RadiusAttributeType.ProxyState:
                    return "Proxy-State";
                case RadiusAttributeType.LoginLATService:
                    return "Login-LAT-Service";
                case RadiusAttributeType.LoginLATNode:
                    return "Login-LAT-Node";
                case RadiusAttributeType.LoginLATGroup:
                    return "Login-LAT-Group";
                case RadiusAttributeType.FramedAppleTalkLink:
                    return "Framed-AppleTalk-Link";
                case RadiusAttributeType.FramedAppleTalkNetwork:
                    return "Framed-AppleTalk-Network";
                case RadiusAttributeType.FramedAppleTalkZone:
                    return "Framed-AppleTalk-Zone";
                case RadiusAttributeType.AcctStatusType:
                    return "Acct-Status-Type";
                case RadiusAttributeType.AcctDelayTime:
                    return "Acct-Delay-Time";
                case RadiusAttributeType.AcctInputOctets:
                    return "Acct-Input-Octets";
                case RadiusAttributeType.AcctOutputOctets:
                    return "Acct-Output-Octets";
                case RadiusAttributeType.AcctSessionId:
                    return "Acct-Session-Id";
                case RadiusAttributeType.AcctAuthentic:
                    return "Acct-Authentic";
                case RadiusAttributeType.AcctSessionTime:
                    return "Acct-Session-Time";
                case RadiusAttributeType.AcctInputPackets:
                    return "Acct-Input-Packets";
                case RadiusAttributeType.AcctOutputPackets:
                    return "Acct-Output-Packets";
                case RadiusAttributeType.AcctTerminateCause:
                    return "Acct-Terminate-Cause";
                case RadiusAttributeType.AcctMultiSessionId:
                    return "Acct-Multi-Session-Id";
                case RadiusAttributeType.AcctLinkCount:
                    return "Acct-Link-Count";
                case RadiusAttributeType.CHAPChallenge:
                    return "CHAP-Challenge";
                case RadiusAttributeType.NASPortType:
                    return "NAS-Port-Type";
                case RadiusAttributeType.PortLimit:
                    return "Port-Limit";
                case RadiusAttributeType.LoginLATPort:
                    return "Login-LAT-Port";
                case RadiusAttributeType.FramedIPv6Address:
                    return "Framed-IPv6-Address";
                default:
                    return type.ToString();
            }
        }

        /// <summary>
        /// Datatypes known to be sent via RADIUS attributes
        /// </summary>
        public enum RadiusAttributeValueDatatype
        {
            String,
            Integer,
            ByteArray,
            IP,
            EncryptedString
        }

        /// <summary>
        /// Type codes known to be sent via RADIUS attributes
        /// </summary>
        public enum RadiusAttributeType
        {
            UserName = 1,
            UserPassword = 2,
            CHAPPassword = 3,
            NASIPAddress = 4,
            NASPort = 5,
            ServiceType = 6,
            FramedProtocol = 7,
            FramedIPAddress = 8,
            FramedIPNetmask = 9,
            FramedRouting = 10,
            FilterId = 11,
            FramedMTU = 12,
            FramedCompression = 13,
            LoginIPHost = 14,
            LoginService = 15,
            LoginTCPPort = 16,
            ReplyMessage = 18,
            CallbackNumber = 19,
            CallbackId = 20,
            FramedRoute = 22,
            FramedIPXNetwork = 23,
            State = 24,
            Class = 25,
            VendorSpecific = 26,
            SessionTimeout = 27,
            IdleTimeout = 28,
            TerminationAction = 29,
            CalledStationId = 30,
            CallingStationId = 31,
            NASIdentifier = 32,
            ProxyState = 33,
            LoginLATService = 34,
            LoginLATNode = 35,
            LoginLATGroup = 36,
            FramedAppleTalkLink = 37,
            FramedAppleTalkNetwork = 38,
            FramedAppleTalkZone = 39,
            AcctStatusType = 40,
            AcctDelayTime = 41,
            AcctInputOctets = 42,
            AcctOutputOctets = 43,
            AcctSessionId = 44,
            AcctAuthentic = 45,
            AcctSessionTime = 46,
            AcctInputPackets = 47,
            AcctOutputPackets = 48,
            AcctTerminateCause = 49,
            AcctMultiSessionId = 50,
            AcctLinkCount = 51,
            CHAPChallenge = 60,
            NASPortType = 61,
            PortLimit = 62,
            LoginLATPort = 63,
            FramedIPv6Address = 168
        }
    }
}
