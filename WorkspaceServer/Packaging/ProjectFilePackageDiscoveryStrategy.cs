﻿using System.IO;
using System.Threading.Tasks;
using Clockwise;

namespace WorkspaceServer.Packaging
{
    public class ProjectFilePackageDiscoveryStrategy : IPackageDiscoveryStrategy
    {
        private readonly bool _createRebuildablePackage;

        public ProjectFilePackageDiscoveryStrategy(bool createRebuildablePackage)
        {
            _createRebuildablePackage = createRebuildablePackage;
        }

        public Task<PackageBuilder> Locate(
            PackageDescriptor packageDescriptor,
            Budget budget = null)
        {
            var projectFile = packageDescriptor.Name;

            if (Path.GetExtension(projectFile) == ".csproj" && File.Exists(projectFile))
            {
                PackageBuilder packageBuilder = new PackageBuilder(packageDescriptor.Name);
                packageBuilder.CreateRebuildablePackage = _createRebuildablePackage;
                packageBuilder.Directory = new DirectoryInfo(Path.GetDirectoryName(projectFile));
                return Task.FromResult(packageBuilder);
            }

            return Task.FromResult<PackageBuilder>(null);
        }
    }
}