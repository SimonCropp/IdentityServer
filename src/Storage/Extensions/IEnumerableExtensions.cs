// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable 1591

namespace Duende.IdentityServer.Extensions;

internal static class IEnumerableExtensions
{
    [DebuggerStepThrough]
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? list)
    {
        if (list == null)
        {
            return true;
        }

        return !list.Any();
    }
}