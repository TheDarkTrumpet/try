﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Events
{
    public class PackageAdded : KernelEventBase
    {
        public PackageAdded(AddPackage addPackage): base(addPackage)
        {
            PackageReference = addPackage?.PackageReference;
        }

        public PackageReference PackageReference { get; internal set;  }
    }
}