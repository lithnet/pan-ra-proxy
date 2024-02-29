namespace Lithnet.Pan.RAProxy
{
    public class UserMappingException : PanApiException
    {
        public string Username { get; }

        public string IPAddress { get; }

        public UserMappingException(string message, string username, string ipAddress)
            : base(message)
        {
            this.Username = username;
            this.IPAddress = ipAddress;
        }

        public UserMappingException() : base()
        {
        }

        public UserMappingException(string message) : base(message)
        {
        }

        public UserMappingException(string message, string detail) : base(message, detail)
        {
        }

        public UserMappingException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
