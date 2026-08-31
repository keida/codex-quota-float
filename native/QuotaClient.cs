using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace QuotaFloat;

public sealed class QuotaClient : IDisposable
{
    const string UsageUrl="https://chatgpt.com/backend-api/wham/usage";
    const string CreditsUrl="https://chatgpt.com/backend-api/wham/rate-limit-reset-credits";
    const int ResponseLimit=1024*1024;
    readonly HttpClient http=new(new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false}){Timeout=TimeSpan.FromSeconds(12)};
    string? credentialFingerprint;
    public long SessionGeneration {get;private set;}
    sealed record Auth(string Token,string? Account);
    static async Task<byte[]> ReadLimitedAsync(Stream stream,int limit,CancellationToken ct)
    {
        using var output=new MemoryStream();var buffer=new byte[8192];
        while(true){int n=await stream.ReadAsync(buffer,ct);if(n==0)break;if(output.Length+n>limit)throw new InvalidDataException();await output.WriteAsync(buffer.AsMemory(0,n),ct);}
        return output.ToArray();
    }
    static async Task<Auth?> ReadAuthAsync(CancellationToken ct)
    {
        var codexDirectory=Environment.GetEnvironmentVariable("CODEX_HOME");
        if(string.IsNullOrWhiteSpace(codexDirectory))codexDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");
        using var stream=new FileStream(Path.Combine(codexDirectory,"auth.json"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        using var document=JsonDocument.Parse(await ReadLimitedAsync(stream,256*1024,ct),new JsonDocumentOptions{MaxDepth=32});
        var root=document.RootElement;var tokens=QuotaParser.Field(root,"tokens");if(tokens.ValueKind!=JsonValueKind.Object)tokens=root;
        var token=QuotaParser.Text(tokens,"access_token","accessToken");
        if(string.IsNullOrWhiteSpace(token)||token.Any(char.IsControl))return null;
        var account=QuotaParser.Text(tokens,"account_id","accountId");
        if(account is null)
        {
            // Decode only the routing claim; no JWT validity decision is made locally.
            var pieces=token.Split('.');
            if(pieces.Length==3)try
            {
                var body=pieces[1].Replace('-','+').Replace('_','/');body=body.PadRight((body.Length+3)/4*4,'=');
                using var claims=JsonDocument.Parse(Convert.FromBase64String(body));
                account=QuotaParser.Text(claims.RootElement,"chatgpt_account_id","https://api.openai.com/auth.chatgpt_account_id")
                    ??QuotaParser.Text(QuotaParser.Field(claims.RootElement,"https://api.openai.com/auth"),"chatgpt_account_id");
            }catch(FormatException){}catch(JsonException){}
        }
        if(account?.Any(char.IsControl)==true)return null;
        return new(token,account);
    }
    async Task<(HttpStatusCode Status,JsonElement Body,TimeSpan? Retry)> GetAsync(string url,Auth auth,CancellationToken ct)
    {
        using var request=new HttpRequestMessage(HttpMethod.Get,url);
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",auth.Token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("originator","Codex Desktop");request.Headers.Add("OAI-Product-Sku","CODEX");
        if(auth.Account is not null)request.Headers.Add("ChatGPT-Account-Id",auth.Account);
        using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);
        var retry=response.Headers.RetryAfter?.Delta;
        if(retry is null && response.Headers.RetryAfter?.Date is {} until)retry=until-DateTimeOffset.UtcNow;
        if(!response.IsSuccessStatusCode)return(response.StatusCode,default,retry);
        if(response.Content.Headers.ContentLength>ResponseLimit)throw new InvalidDataException();
        using var stream=await response.Content.ReadAsStreamAsync(ct);
        using var document=JsonDocument.Parse(await ReadLimitedAsync(stream,ResponseLimit,ct),new JsonDocumentOptions{MaxDepth=32});
        return(response.StatusCode,document.RootElement.Clone(),retry);
    }
    static string Fingerprint(Auth auth)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(auth.Token+"\n"+auth.Account)));
    void ObserveIdentity(string? fingerprint){if(fingerprint!=credentialFingerprint){credentialFingerprint=fingerprint;SessionGeneration++;}}
    public async Task<QuotaResult> FetchAsync(CancellationToken ct)
    {
        Auth? auth;
        try{auth=await ReadAuthAsync(ct);}catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException){ObserveIdentity(null);return new(null,"signedout");}
        if(auth is null){ObserveIdentity(null);return new(null,"signedout");}
        var fingerprint=Fingerprint(auth);ObserveIdentity(fingerprint);
        var result=await FetchForAuthAsync(auth,ct);
        // A response belongs to the credentials used when its request began.
        // Re-read before committing even a failure/cache fallback, never display an old account's in-flight result.
        Auth? current;
        try{current=await ReadAuthAsync(ct);}catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException){current=null;}
        var currentFingerprint=current is null?null:Fingerprint(current);
        if(currentFingerprint!=fingerprint){ObserveIdentity(currentFingerprint);return new(null,current is null?"signedout":"sessionchanged");}
        return result;
    }
    async Task<QuotaResult> FetchForAuthAsync(Auth auth,CancellationToken ct)
    {
        try
        {
            var usage=await GetAsync(UsageUrl,auth,ct);
            if(usage.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)return new(null,"signedout");
            if((int)usage.Status==429)return new(null,"ratelimit",usage.Retry);
            if((int)usage.Status is <200 or >=300)return new(null,"unavailable");
            JsonElement credits=default;
            try{var result=await GetAsync(CreditsUrl,auth,ct);if((int)result.Status is >=200 and <300)credits=result.Body;}
            catch(Exception e)when(e is HttpRequestException or IOException or InvalidDataException or JsonException or OperationCanceledException){ct.ThrowIfCancellationRequested();}
            var snapshot=QuotaParser.Parse(usage.Body,credits,DateTimeOffset.UtcNow);
            return snapshot is null?new(null,"format"):new(snapshot,snapshot.Partial?"partial":"ok");
        }
        catch(OperationCanceledException)when(!ct.IsCancellationRequested){return new(null,"offline");}
        catch(HttpRequestException){return new(null,"offline");}
        catch(Exception e)when(e is IOException or InvalidDataException or JsonException or ArgumentException){return new(null,"format");}
    }
    public void Dispose()=>http.Dispose();
}
