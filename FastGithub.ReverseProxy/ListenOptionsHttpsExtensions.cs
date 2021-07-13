﻿using FastGithub.ReverseProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace FastGithub
{
    /// <summary>
    /// ListenOptions扩展
    /// </summary>
    public static class ListenOptionsHttpsExtensions
    {
        /// <summary>
        /// 应用fastGihub的https
        /// </summary>
        /// <param name="listenOptions"></param>
        /// <param name="caPublicCerPath"></param>
        /// <param name="caPrivateKeyPath"></param>
        /// <returns></returns>
        public static ListenOptions UseGithubHttps(this ListenOptions listenOptions, string caPublicCerPath, string caPrivateKeyPath)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var caCert = new X509Certificate2(caPublicCerPath);
                    using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadWrite);
                    if (store.Certificates.Find(X509FindType.FindByThumbprint, caCert.Thumbprint, true).Count == 0)
                    {
                        store.Add(caCert);
                        store.Close();
                    }
                }
                catch (Exception ex)
                {
                    var loggerFactory = listenOptions.ApplicationServices.GetRequiredService<LoggerFactory>();
                    var logger = loggerFactory.CreateLogger($"{nameof(FastGithub)}{nameof(ReverseProxy)}");
                    logger.LogError($"安装根证书{caPublicCerPath}失败：{ex.Message}");
                }
            }

            return listenOptions.UseHttps(https =>
            {
                var certs = new ConcurrentDictionary<string, X509Certificate2>();
                https.ServerCertificateSelector = (ctx, domain) => certs.GetOrAdd(domain, CreateCert);
            });

            X509Certificate2 CreateCert(string domain)
            {
                var domains = new[] { domain };
                var validFrom = DateTime.Today.AddYears(-1);
                var validTo = DateTime.Today.AddYears(10);
                return CertGenerator.Generate(domains, 2048, validFrom, validTo, caPublicCerPath, caPrivateKeyPath);
            }
        }
    }
}