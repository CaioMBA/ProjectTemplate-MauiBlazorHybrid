using Domain.Enums;
using Domain.Extensions;
using Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.RequestModels;
using Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.ResponseModels;
using Grpc.Core;
using Grpc.Net.Client;

namespace Data.ApiRepositories;

public class GrpcApiAccess(IHttpClientFactory httpFactory)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;

    public async Task<GrpcApiResponseModel> Request(GrpcApiRequestModel ApiRequest, NamedHttpClient specificHttpClient = NamedHttpClient.DEFAULT)
    {
        var httpClient = specificHttpClient != NamedHttpClient.DEFAULT ? _httpFactory.CreateClient(specificHttpClient.ToString()) : _httpFactory.CreateClient();

        if (ApiRequest.TimeOut != null)
        {
            httpClient.Timeout = TimeSpan.FromSeconds((double)ApiRequest.TimeOut);
        }

        using var channel = GrpcChannel.ForAddress(ApiRequest.Url, new GrpcChannelOptions
        {
            HttpClient = httpClient
        });

        var marshaller = Marshallers.Create(
            (byte[] request) => request,
            data => data);

        var method = new Method<byte[], byte[]>(
            MethodType.Unary,
            ApiRequest.Service,
            ApiRequest.Method,
            marshaller,
            marshaller);

        Metadata metadata = [];

        if (ApiRequest.Authentication != null)
        {
            metadata.Add("authorization", $"{ApiRequest.Authentication.Type} {ApiRequest.Authentication.Authorization}");
        }

        if (ApiRequest.Headers != null && ApiRequest.Headers.Any())
        {
            foreach (var header in ApiRequest.Headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Value))
                {
                    metadata.Add(header.Key, header.Value);
                }
            }
        }

        DateTime? deadline = ApiRequest.TimeOut != null ? DateTime.UtcNow.AddSeconds((double)ApiRequest.TimeOut) : null;
        CallOptions callOptions = new(headers: metadata, deadline: deadline);

        try
        {
            AsyncUnaryCall<byte[]> call = channel.CreateCallInvoker().AsyncUnaryCall(
                method,
                null,
                callOptions,
                string.IsNullOrEmpty(ApiRequest.Body) ? [] : ApiRequest.Body.ToBytes());

            byte[] responseData = await call.ResponseAsync;
            Status status = call.GetStatus();

            IDictionary<string, string> trailers = call.GetTrailers()
                .ToDictionary(t => t.Key, t => t.Value);

            return new GrpcApiResponseModel
            {
                StatusCode = (int)status.StatusCode,
                Message = status.Detail,
                Data = responseData,
                Trailers = trailers
            };
        }
        catch (RpcException ex)
        {
            IDictionary<string, string> trailers = ex.Trailers
                .ToDictionary(t => t.Key, t => t.Value);

            return new GrpcApiResponseModel
            {
                StatusCode = (int)ex.StatusCode,
                Message = ex.Status.Detail,
                Data = null,
                Trailers = trailers
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }
}
