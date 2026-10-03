//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace SevenBoldPencil.MagicOpticMount;

[Injectable(TypePriority = OnLoadOrder.Preload + 2)]
public class Plugin(WTTServerCommonLib.WTTServerCommonLib wttCommon) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        await Task.CompletedTask;
    }
}
