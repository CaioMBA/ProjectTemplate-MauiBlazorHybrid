using Domain.Enums;
using Domain.Extensions;
using Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.RequestModels;
using Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.ResponseModels;
using System.Text;

namespace Data.ApiRepositories;

public class GraphApiAccess(IHttpClientFactory httpFactory)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    public async Task<GraphQlApiResponseModel> Request(GraphQlApiRequesModel GraphQlRequest, NamedHttpClient specificHttpClient = NamedHttpClient.DEFAULT)
    {
        var httpClient = specificHttpClient != NamedHttpClient.DEFAULT ? _httpFactory.CreateClient(specificHttpClient.ToString()) : _httpFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, GraphQlRequest.Url);

        if (GraphQlRequest.Authentication != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                GraphQlRequest.Authentication.Type.ToString(),
                GraphQlRequest.Authentication.Authorization);
        }

        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(new
        {
            Query = GraphQlRequest.Query,
            Variables = GraphQlRequest.Variables ?? new Dictionary<string, object?>()
        }.ToJson(), Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await httpClient.SendAsync(request);

            string content = await response.Content.ReadAsStringAsync();

            GraphQlApiResponseModel? returnObj = content.ToObject<GraphQlApiResponseModel>() ?? new GraphQlApiResponseModel();
            returnObj.StatusCode = (int)response.StatusCode;
            return returnObj;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }
}
