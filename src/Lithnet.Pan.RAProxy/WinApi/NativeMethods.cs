using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.Pan.RAProxy
{
    internal static class NativeMethods
    {
        [DllImport("secur32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int TranslateName(string accountName, ExtendedNameFormat nameFormat, ExtendedNameFormat desiredFormat, StringBuilder translatedName, ref int userNameSize);

        public static string TranslateName(string incomingName, ExtendedNameFormat incomingNameFormat, ExtendedNameFormat desiredNameFormat)
        {
            int usernameSize = 0;

            int result = TranslateName(incomingName, incomingNameFormat, desiredNameFormat, null, ref usernameSize);

            if (result == 0)
            {
                result = Marshal.GetLastWin32Error();

                throw new Win32Exception(result);
            }

            if (usernameSize == 0)
            {
                throw new ApplicationException("The API for parsing the username returned an unexpected result");
            }

            StringBuilder builder = new StringBuilder(usernameSize);

            result = TranslateName(incomingName, incomingNameFormat, desiredNameFormat, builder, ref usernameSize);

            if (result == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return builder.ToString();
        }
    }
}
