using System;
using System.IO;
using AElf.Contracts.TestKit;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Ballot.Tests
{
    [DependsOn(typeof(ContractTestModule))]
    public class BallotTestModule : ContractTestModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.PostConfigure(new Action<RunnerOptions>((o) =>
            {
                o.SdkDir = Path.GetDirectoryName(typeof(AElf.Sdk.CSharp.CSharpSmartContract).Assembly.Location);
            }));
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.ConfigureOptions<RunnerOptions>();
            context.Services.AddAssemblyOf<BallotTestModule>();
        }
    }
}