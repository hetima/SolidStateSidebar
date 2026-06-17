using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using Newtonsoft.Json;

namespace SSS.Windows
{
    public enum WinOS : byte
    {
        Unknown,
        Other,
        Win7,
        Win8,
        Win8_1,
        Win10
    }

    public static class OS
    {
        private static WinOS _os = WinOS.Unknown;

        public static WinOS Get
        {
            get
            {
                if (_os != WinOS.Unknown)
                {
                    return _os;
                }

                Version _version = Environment.OSVersion.Version;

                if (_version.Major >= 10)
                {
                    _os = WinOS.Win10;
                }
                else if (_version.Major == 6 && _version.Minor == 3)
                {
                    _os = WinOS.Win8_1;
                }
                else if (_version.Major == 6 && _version.Minor == 2)
                {
                    _os = WinOS.Win8;
                }
                else if (_version.Major == 6 && _version.Minor == 1)
                {
                    _os = WinOS.Win7;
                }
                else
                {
                    _os = WinOS.Other;
                }

                return _os;
            }
        }

        public static bool SupportDPI
        {
            get
            {
                return OS.Get >= WinOS.Win8_1;
            }
        }

        public static bool SupportVirtualDesktop
        {
            get
            {
                return OS.Get >= WinOS.Win10;
            }
        }
    }
}
