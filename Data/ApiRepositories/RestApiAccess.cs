using Domain.Enums;
using Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.RequestModels;
using Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.ResponseModels;
using System.Text;
using System.Web;

namespace Data.ApiRepositories;

public class RestApiAccess(IHttpClientFactory httpFactory)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;

    public async Task<RestApiResponseModel> Request(RestApiRequestModel ApiRequest, NamedHttpClient specificHttpClient = NamedHttpClient.DEFAULT)
    {
        var httpClient = specificHttpClient != NamedHttpClient.DEFAULT ? _httpFactory.CreateClient(specificHttpClient.ToString()) : _httpFactory.CreateClient();
        HttpMethod Method = ApiRequest.TypeRequest switch
        {
            ApiRequestMethod.GET => HttpMethod.Get,
            ApiRequestMethod.POST => HttpMethod.Post,
            ApiRequestMethod.PUT => HttpMethod.Put,
            ApiRequestMethod.DELETE => HttpMethod.Delete,
            ApiRequestMethod.HEAD => HttpMethod.Head,
            ApiRequestMethod.OPTIONS => HttpMethod.Options,
            ApiRequestMethod.TRACE => HttpMethod.Trace,
            ApiRequestMethod.PATCH => HttpMethod.Patch,
            ApiRequestMethod.QUERY => HttpMethod.Query,
            ApiRequestMethod.CONNECT => HttpMethod.Connect,
            _ => throw new NotImplementedException()
        };
        var uriBuilder = new UriBuilder(ApiRequest.Url);

        if (ApiRequest.QueryParameters != null && ApiRequest.QueryParameters.Any())
        {
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var item in ApiRequest.QueryParameters)
            {
                query[item.Key] = item.Value;
            }
            uriBuilder.Query = query.ToString();
        }

        var request = new HttpRequestMessage(Method, uriBuilder.ToString());



        if (ApiRequest.TimeOut != null)
        {
            httpClient.Timeout = TimeSpan.FromSeconds((double)ApiRequest.TimeOut);
        }
        if (ApiRequest.Authentication != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(ApiRequest.Authentication.Type.ToString(), ApiRequest.Authentication.Authorization);
        }
        if (ApiRequest.Headers != null && ApiRequest.Headers.Any())
        {
            foreach (var header in ApiRequest.Headers!)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(ApiRequest.Body))
        {
            request.Content = new StringContent(ApiRequest.Body, Encoding.UTF8, "application/json");
        }

        try
        {
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();

            IDictionary<string, string> headers = response.Headers
                .Concat(response.Content.Headers)
                .ToDictionary(h => h.Key, h => string.Join(",", h.Value));

            return new RestApiResponseModel
            {
                StatusCode = (int)response.StatusCode,
                Message = response.ReasonPhrase,
                Data = content,
                Headers = headers
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }
}
