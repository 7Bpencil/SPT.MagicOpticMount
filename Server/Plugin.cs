//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace SevenBoldPencil.ModularSights;

[Injectable(TypePriority = OnLoadOrder.Preload + 90000)]
public class Plugin(
    ISptLogger<Plugin> logger
) : IOnLoad
{
    public static Plugin Instance;
    public ISptLogger<Plugin> Logger = logger;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Instance = this;

        return Task.CompletedTask;
    }
}
