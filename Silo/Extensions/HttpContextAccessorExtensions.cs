// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Orleans.ShoppingCart.Silo.Extensions;

internal static class HttpContextAccessorExtensions
{
    internal static string? TryGetUserId(
        this IHttpContextAccessor? httpContextAccessor)
    {
        var userId = httpContextAccessor
            ?.HttpContext
            ?.User
            .FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "demo-shared-user";

        return userId;
    }
}
