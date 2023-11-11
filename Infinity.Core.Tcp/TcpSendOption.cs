﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infinity.Core.Tcp
{
    /// <summary>
    ///     Extra public states for SendOption enumeration when using TCP.
    /// </summary>
    public class TcpSendOption
    {
        /// <summary>
        ///     Hello message for initiating communication.
        /// </summary>
        public const byte Connect = 8;

        /// <summary>
        ///     Message for discontinuing communication.
        /// </summary>
        public const byte Disconnect = 9;

        public const byte MessageUnordered = 15;
        public const byte MessageOrdered = 16;
    }
}