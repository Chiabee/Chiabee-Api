namespace WebApi.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Renci.SshNet;
    using WebApi.Entities;
    using WebApi.Models;
    using static WebApi.Models.AppSettings;

    public static class ExtensionMethods
    {
        public static IEnumerable<User> WithoutPasswords(this IEnumerable<User> users)
        {
            return users.Select(x => x.WithoutPassword());
        }

        public static User WithoutPassword(this User user)
        {
            return user with { Password = null };
        }

        public static TargetMachine ToMachineClient(this SshEntity entity, ILogger logger)
        {
            var machine = entity.Port is int port
                ? new TargetMachine(
                    entity.ToProperty(), logger, entity.Hosts.First(), port, entity.Username, new PrivateKeyFile(entity.PrivateKeyFile))
                : new TargetMachine(
                    entity.ToProperty(), logger, entity.Hosts.First(), entity.Username, new PrivateKeyFile(entity.PrivateKeyFile));

            machine.ConnectionInfo.Timeout = new TimeSpan(0, 0, 5);
            machine.ConnectionInfo.RetryAttempts = 1;

            return machine;
        }

        public static IEnumerable<TargetMachine> ToMachineClients(this IEnumerable<SshEntity> entity, ILogger logger)
        {
            return entity.Select(_ => _.ToMachineClient(logger));
        }

        public static string Compress(this string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var bytes = Encoding.Unicode.GetBytes(s);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }
                return Convert.ToBase64String(mso.ToArray());
            }
        }

        public static string Decompress(this string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var bytes = Convert.FromBase64String(s);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return Encoding.Unicode.GetString(mso.ToArray());
            }
        }

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            var results = new List<T>();
            await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
                results.Add(item);
            return results;
        }

        public static string Sha256(this string inputString)
        {
            return Convert.ToHexString(Sha256ToBytes(inputString));

            static byte[] Sha256ToBytes(string inputString)
            {
                using var algorithm = SHA256.Create();
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }
    }
}