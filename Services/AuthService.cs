using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScoutsReporter.Services;

public class AuthService
{
    private const string ClientId = "5515f96e-3252-4efd-a2eb-7c6be1bba5aa";
    private const string TokenUrl = "https://prodscoutsb2c.b2clogin.com/prodscoutsb2c.onmicrosoft.com/B2C_1_signin_signup/oauth2/v2.0/token";
    private const string RedirectUri = "https://membership.scouts.org.uk/";

    private const string AuthorizeBaseUrl =
        "https://prodscoutsb2c.b2clogin.com/prodscoutsb2c.onmicrosoft.com/B2C_1_signin_signup/oauth2/v2.0/authorize";

    private readonly HttpClient _http;
    private string _tokenFilePath = "";

    public string IdToken { get; private set; } = "";
    public string ContactId { get; private set; } = "";
    public string UserName { get; private set; } = "";

    public static string DefaultTokenFilePath
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScoutsReporter");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "membership_refresh_token.dat");
        }
    }

    public AuthService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Generates a PKCE code_verifier and returns (authorizeUrl, codeVerifier).
    /// </summary>
    public static (string AuthorizeUrl, string CodeVerifier) BuildAuthorizeUrl()
    {
        // Generate code_verifier: 64 random bytes -> base64url (86 chars)
        var verifierBytes = RandomNumberGenerator.GetBytes(64);
        var codeVerifier = Base64UrlEncode(verifierBytes);

        // code_challenge = BASE64URL(SHA256(code_verifier))
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = Base64UrlEncode(challengeBytes);

        var url = AuthorizeBaseUrl
            + "?client_id=" + ClientId
            + "&redirect_uri=" + Uri.EscapeDataString(RedirectUri)
            + "&response_type=code"
            + "&scope=" + Uri.EscapeDataString("openid profile offline_access")
            + "&response_mode=query"
            + "&code_challenge=" + codeChallenge
            + "&code_challenge_method=S256";

        return (url, codeVerifier);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public void SetTokenFilePath(string path)
    {
        _tokenFilePath = path;
    }

    public void Logout()
    {
        IdToken = "";
        ContactId = "";
        UserName = "";

        try
        {
            if (!string.IsNullOrEmpty(_tokenFilePath) && File.Exists(_tokenFilePath))
                File.Delete(_tokenFilePath);
        }
        catch { /* best-effort */ }

        _tokenFilePath = "";
    }

    public async Task ExchangeCodeForTokensAsync(string authCode, string codeVerifier)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["code"] = authCode,
            ["redirect_uri"] = RedirectUri,
            ["scope"] = "openid profile offline_access",
            ["code_verifier"] = codeVerifier,
        });

        var resp = await _http.PostAsync(TokenUrl, content);
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<TokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to parse token response.");

        if (!string.IsNullOrEmpty(data.Error))
            throw new InvalidOperationException($"Token exchange failed: {data.ErrorDescription ?? data.Error}");

        // Save refresh token encrypted with DPAPI (only this Windows user can decrypt)
        if (!string.IsNullOrEmpty(data.RefreshToken))
        {
            var savePath = string.IsNullOrEmpty(_tokenFilePath) ? DefaultTokenFilePath : _tokenFilePath;
            SaveTokenEncrypted(savePath, data.RefreshToken);
            _tokenFilePath = savePath;
        }

        IdToken = data.IdToken ?? "";
        var claims = DecodeJwtPayload(IdToken);
        ContactId = GetClaim(claims, "extension_ContactId");
        UserName = GetClaim(claims, "name");
    }

    public async Task RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_tokenFilePath) || !File.Exists(_tokenFilePath))
            throw new InvalidOperationException("Token file path not set or file does not exist.");

        var rt = LoadTokenDecrypted(_tokenFilePath);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["refresh_token"] = rt,
            ["scope"] = "openid profile offline_access",
            ["redirect_uri"] = RedirectUri,
        });

        var resp = await _http.PostAsync(TokenUrl, content);
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<TokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to parse token response.");

        if (!string.IsNullOrEmpty(data.Error))
            throw new InvalidOperationException($"Token refresh failed: {data.ErrorDescription ?? data.Error}");

        if (!string.IsNullOrEmpty(data.RefreshToken))
            SaveTokenEncrypted(_tokenFilePath, data.RefreshToken);

        IdToken = data.IdToken ?? "";
        var claims = DecodeJwtPayload(IdToken);
        ContactId = GetClaim(claims, "extension_ContactId");
        UserName = GetClaim(claims, "name");
    }

    private static void SaveTokenEncrypted(string path, string token)
    {
        var plain = Encoding.UTF8.GetBytes(token);
        var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, encrypted);
    }

    private static string LoadTokenDecrypted(string path)
    {
        var encrypted = File.ReadAllBytes(path);
        var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }

    private static Dictionary<string, JsonElement> DecodeJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return new();

        var payload = parts[1];
        // Pad base64
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
        var jsonStr = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonStr) ?? new();
    }

    private static string GetClaim(Dictionary<string, JsonElement> claims, string key)
    {
        if (claims.TryGetValue(key, out var val))
            return val.ToString();
        return "";
    }

    private class TokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}
