﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Persistence;
using NServiceBus.Persistence.NHibernate;

public class ConfigureNHibernatePersistence : IConfigureTestExecution
{
    public Task Configure(BusConfiguration config, IDictionary<string, string> settings)
    {
        NHibernateSettingRetriever.AppSettings = () => new NameValueCollection
        {
            {"NServiceBus/Persistence/NHibernate/connection.driver_class", "NHibernate.Driver.OracleManagedDataClientDriver"},
            {"NServiceBus/Persistence/NHibernate/dialect", "NHibernate.Dialect.Oracle10gDialect"}
        };

        config.UsePersistence<NHibernatePersistence>()
            .ConnectionString(@"Data Source=XE;User Id=particular;Password=Welcome1;Enlist=dynamic")
            .SagaTableNamingConvention(type =>
            {
                var tablename = DefaultTableNameConvention(type);

                if (tablename.Length > 30) //Oracle limit and it has to start with an Alpha character
                {
                    return Create(tablename);
                }

                return tablename;
            });

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return Task.FromResult(0);
    }

    static string Create(params object[] data)
    {
        using (var provider = new MD5CryptoServiceProvider())
        {
            var inputBytes = Encoding.Default.GetBytes(string.Concat(data));
            var hashBytes = provider.ComputeHash(inputBytes);
            // generate a guid from the hash:
            return "A" + new Guid(hashBytes).ToString("N").Substring(0, 20);
        }
    }

    static string DefaultTableNameConvention(Type type)
    {
        //if the type is nested use the name of the parent
        if (type.DeclaringType == null)
        {
            return type.Name;
        }

        if (typeof(IContainSagaData).IsAssignableFrom(type))
        {
            return type.DeclaringType.Name;
        }

        return type.DeclaringType.Name + "_" + type.Name;
    }
}