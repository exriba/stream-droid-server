using Mapster;
using Microsoft.Extensions.Configuration;
using StreamDroid.Domain.DTOs;
using StreamDroid.Shared;
using System.Reflection;

namespace StreamDroid.Domain.Tests.Common
{
    public abstract class TestFixture : IDisposable
    {
        private static bool Initialized;
        private readonly ConfigurationManager _configurationManager;

        protected TestFixture()
        {
            if (!Initialized)
            {
                var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
                Assembly applicationAssembly = typeof(BaseDto<,>).Assembly;
                typeAdapterConfig.Scan(applicationAssembly);
                Initialized = true;
            }

            _configurationManager = new ConfigurationManager();
            _configurationManager.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Common/appsettings.Test.json")
                .Build();
            _configurationManager.Configure();
        }

        public void Dispose() => _configurationManager.Dispose();
    }
}
