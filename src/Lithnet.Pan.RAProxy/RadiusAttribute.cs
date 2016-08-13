using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Pan.RAProxy
{
    class RadiusAttribute
    {
        // Type of attribute
        public RadiusAttributeType Type { get; set; }

        // Value of attribute received, could be String, Integer, or something else, depending on type
        public object Value { get; set; }

        // Native datatypes for value
        public String ValueAsString { get; set; }
        public int ValueAsInt { get; set; }
        public byte[] ValueAsByteArray { get; set; }


        /// <summary>
        /// Default constructor
        /// </summary>
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

            int attributeType = Convert.ToUInt16(rawAttributeBlock[0]);
            int attributeLength = Convert.ToUInt16(rawAttributeBlock[1]);
            byte[] attributeValue = new byte[attributeLength - 2];
            if (attributeLength > 2 && attributeLength < rawAttributeBlock.Length)
            {
                Array.Copy(rawAttributeBlock, 2, attributeValue, 0, attributeLength - 2);
            }
            else
            {
                attributeValue = null;
            }

            return ParseAttributeValue(attributeType, attributeValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static RadiusAttribute ParseAttributeValue(int type, byte[] value)
        {
            RadiusAttribute newAttribute = new RadiusAttribute();

            return newAttribute;
        }

        public static List<RadiusAttribute> ParseAttributeMessage(byte[] rawAttributeMessage, int startIndex = 0)
        {
            List<RadiusAttribute> result = new List<RadiusAttribute>();

            int byteIndex = startIndex;
            int attributeType;
            int attributeLength;
            byte[] attributeBytes;
            Dictionary<int, byte[]> attributes = new Dictionary<int, byte[]>();
            while (byteIndex + 2 < rawAttributeMessage.Length)
            {
                attributeType = Convert.ToUInt16(rawAttributeMessage[byteIndex]);
                attributeLength = Convert.ToUInt16(rawAttributeMessage[byteIndex+1]);
                if (attributeLength > 2 && attributeLength + byteIndex < rawAttributeMessage.Length)
                {
                    attributeBytes = new byte[attributeLength - 2];
                    Array.Copy(rawAttributeMessage, byteIndex+2, attributeBytes, 0, attributeLength - 2);
                    byteIndex += attributeLength;
                }
                else
                {
                    attributeBytes = null;
                }

                RadiusAttribute attribute = ParseAttributeValue(attributeType, attributeBytes);
                result.Add(attribute);
            }

            return result;
        }

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
        }
    }
}
