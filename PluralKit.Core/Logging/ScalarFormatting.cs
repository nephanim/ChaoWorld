using System;
using System.IO;

using Serilog.Formatting.Elasticsearch;
using Serilog.Formatting.Json;

namespace ChaoWorld.Core
{
    public class ScalarFormatting
    {
        private static bool Write(object value, TextWriter output)
        {
            if (value is GardenId si)
                output.Write(si.Value);
            else if (value is ChaoId mi)
                output.Write(mi.Value);
            else if (value is GroupId gi)
                output.Write(gi.Value);
            else
                return false;
            return true;
        }

        private static void WriteV(object value, TextWriter output) => Write(value, output);

        public class Elasticsearch: ElasticsearchJsonFormatter
        {
            public Elasticsearch(bool omitEnclosingObject = false, string closingDelimiter = null,
                                 bool renderMessage = true, IFormatProvider formatProvider = null,
                                 ISerializer serializer = null, bool inlineFields = false,
                                 bool renderMessageTemplate = true, bool formatStackTraceAsArray = false) : base(
                omitEnclosingObject, closingDelimiter, renderMessage, formatProvider, serializer, inlineFields,
                renderMessageTemplate, formatStackTraceAsArray)
            {
                AddLiteralWriter(typeof(GardenId), WriteV);
                AddLiteralWriter(typeof(ChaoId), WriteV);
                AddLiteralWriter(typeof(GroupId), WriteV);
            }
        }

        public class JsonValue: JsonValueFormatter
        {
            protected override void FormatLiteralValue(object value, TextWriter output)
            {
                if (Write(value, output))
                    return;
                base.FormatLiteralValue(value, output);
            }
        }
    }
}