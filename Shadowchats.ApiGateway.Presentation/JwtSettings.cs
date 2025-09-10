// Shadowchats — Copyright (C) 2025
// Dorovskoy Alexey Vasilievich (One290 / 0ne290) <lenya.dorovskoy@mail.ru>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version. See the LICENSE file for details.
// For full copyright and authorship information, see the COPYRIGHT file.

using System.ComponentModel.DataAnnotations;

namespace Shadowchats.ApiGateway.Presentation;

public record JwtSettings
{
    [Required]
    public required string SecretKeyBase64
    {
        get => _secretKeyBase64;
        init
        {
            _secretKeyBase64 = value;
            SecretKeyBytes = Convert.FromBase64String(value);
        }
    }

    public byte[] SecretKeyBytes { get; private init; } = null!;
    
    [Required]
    public required string Issuer { get; init; }
    
    [Required]
    public required string Audience { get; init; }

    private readonly string _secretKeyBase64 = null!;
}