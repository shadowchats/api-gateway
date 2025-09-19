// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using System.Diagnostics;

namespace Shadowchats.ApiGateway.Presentation.Extensions;

public static class ExceptionExtensions
{
    public static string GetLocation(this Exception exception)
    {
        foreach (var frame in new StackTrace(exception, true).GetFrames())
            if (frame.GetMethod()?.DeclaringType?.Assembly.FullName?.Contains(SolutionPrefix) ?? false)
                return $"{frame.GetFileName()} : {frame.GetFileLineNumber()}";

        throw new BugException("Exception location not found.");
    }

    private const string SolutionPrefix = "Shadowchats.ApiGateway.";
}