using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.UserPass;

namespace WebApplication
{
    public class VaultFileProvider : IFileProvider
    {
        private class VaultFile : IFileInfo
        {
            private readonly byte[] _data;
            public VaultFile(string json) => _data = Encoding.UTF8.GetBytes(json);
            public Stream CreateReadStream() => new MemoryStream(_data);
            public bool Exists { get; } = true;
            public long Length => _data.Length;
            public string PhysicalPath { get; } = string.Empty;
            public string Name { get; } = string.Empty;
            public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;
            public bool IsDirectory { get; } = false;
        }

        private readonly IFileInfo _fileInfo;
    
        public VaultFileProvider(
            string vaultAddress,
            string vaultUsername,
            string vaultPassword,
            string secretPath,
            string providerPath)
        {
            

            IAuthMethodInfo authMethod = new UserPassAuthMethodInfo(username: vaultUsername,password: vaultPassword);

            var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod)
            {
                ContinueAsyncTasksOnCapturedContext = false,
            };

            var vaultClient = new VaultClient(vaultClientSettings);

            var secrets = Task.Run(() => vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync<object>(path: secretPath, null, providerPath)).Result;


            _fileInfo = new VaultFile(secrets.Data.Data.ToString());

        }

        public VaultFileProvider(
            string vaultAddress,
            string vaultToken,
            string secretPath,
            string providerPath)
        {
            IAuthMethodInfo authMethod = new TokenAuthMethodInfo( vaultToken);

            var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod)
            {
                ContinueAsyncTasksOnCapturedContext = false,
            };

            var vaultClient = new VaultClient(vaultClientSettings);

            var secrets = Task.Run(() => vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync<object>(path: secretPath, null, providerPath)).Result;

            _fileInfo = new VaultFile(secrets.Data.Data.ToString());

        }
        public IFileInfo GetFileInfo(string _) => _fileInfo;
        public IDirectoryContents GetDirectoryContents(string _) => null;
        public IChangeToken Watch(string _) => NullChangeToken.Singleton;
    }
}
