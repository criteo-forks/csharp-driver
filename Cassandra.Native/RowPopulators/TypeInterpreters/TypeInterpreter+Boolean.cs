﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Cassandra
{
    internal partial class TypeInterpreter
    {
        public static object ConvertFromBoolean(TableMetadata.ColumnInfo type_info, byte[] _buffer)
        {
            return _buffer[0] == 1;
        }

        public static Type GetTypeFromBoolean(TableMetadata.ColumnInfo type_info)
        {
            return typeof(bool);
        }

        public static byte[] InvConvertFromBoolean(TableMetadata.ColumnInfo type_info, object value)
        {
            CheckArgument<bool>(value);
            var buffer = new byte[1];
            buffer[0] = ((bool)value) ? (byte)0x01 : (byte)0x00;
            return buffer;
        }
    }
}
