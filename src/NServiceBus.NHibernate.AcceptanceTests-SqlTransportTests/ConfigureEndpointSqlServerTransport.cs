using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;

public partial class ConfigureEndpointSqlServerTransport : EndpointConfigurer
{
    public override Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        configuration.UsePersistence<NHibernatePersistence>()
            .ConnectionString(ConnectionString);

        transport = new TestingSqlTransport(ConnectionString);
        transport.Subscriptions.DisableCaching = true;

        configuration.UseTransport(transport);

        return Task.FromResult(0);
    }


    public override Task Cleanup()
    {
        if (transport.ReceiveAddresses != null)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                var queueAddresses = transport.ReceiveAddresses.Select(QueueAddress.Parse).ToList();
                foreach (var address in queueAddresses)
                {
                    TryDeleteTable(conn, address);
                    TryDeleteTable(conn, new QueueAddress(address.Table + ".Delayed", address.Schema, address.Catalog));
                }
            }
        }
        return Task.FromResult(0);
    }

    static void TryDeleteTable(SqlConnection conn, QueueAddress address)
    {
        try
        {
            using (var comm = conn.CreateCommand())
            {
                comm.CommandText = $"IF OBJECT_ID('{address.QualifiedTableName}', 'U') IS NOT NULL DROP TABLE {address.QualifiedTableName}";
                comm.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            if (!e.Message.Contains("it does not exist or you do not have permission"))
            {
                throw;
            }
        }
    }

    TestingSqlTransport transport;

    class QueueAddress
    {
        public QueueAddress(string table, string schemaName, string catalogName)
        {
            Table = table;
            Catalog = SafeUnquote(catalogName);
            Schema = SafeUnquote(schemaName);
        }

        public string Catalog { get; }
        public string Table { get; }
        public string Schema { get; }

        public static QueueAddress Parse(string address)
        {
            var firstAtIndex = address.IndexOf("@", StringComparison.Ordinal);

            if (firstAtIndex == -1)
            {
                return new QueueAddress(address, null, null);
            }

            var tableName = address.Substring(0, firstAtIndex);
            address = firstAtIndex + 1 < address.Length ? address.Substring(firstAtIndex + 1) : string.Empty;

            address = ExtractNextPart(address, out var schemaName);

            string catalogName = null;

            if (address != string.Empty)
            {
                ExtractNextPart(address, out catalogName);
            }
            return new QueueAddress(tableName, schemaName, catalogName);
        }

        public string QualifiedTableName => $"{Quote(Catalog)}.{Quote(Schema)}.{Quote(Table)}";

        static string ExtractNextPart(string address, out string part)
        {
            var noRightBrackets = 0;
            var index = 1;

            while (true)
            {
                if (index >= address.Length)
                {
                    part = address;
                    return string.Empty;
                }

                if (address[index] == '@' && (address[0] != '[' || noRightBrackets % 2 == 1))
                {
                    part = address.Substring(0, index);
                    return index + 1 < address.Length ? address.Substring(index + 1) : string.Empty;
                }

                if (address[index] == ']')
                {
                    noRightBrackets++;
                }

                index++;
            }
        }

        static string Quote(string name)
        {
            if (name == null)
            {
                return null;
            }
            return prefix + name.Replace(suffix, suffix + suffix) + suffix;
        }

        static string SafeUnquote(string name)
        {
            var result = Unquote(name);
            return string.IsNullOrWhiteSpace(result)
                ? null
                : result;
        }

        const string prefix = "[";
        const string suffix = "]";

        static string Unquote(string quotedString)
        {
            if (quotedString == null)
            {
                return null;
            }

            if (!quotedString.StartsWith(prefix) || !quotedString.EndsWith(suffix))
            {
                return quotedString;
            }

            return quotedString
                .Substring(prefix.Length, quotedString.Length - prefix.Length - suffix.Length).Replace(suffix + suffix, suffix);
        }
    }
}