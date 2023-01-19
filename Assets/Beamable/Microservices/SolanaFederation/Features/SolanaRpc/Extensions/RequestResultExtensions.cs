using Beamable.Microservices.SolanaFederation.Features.SolanaRpc.Exceptions;
using Solana.Unity.Rpc.Core.Http;

namespace Beamable.Microservices.SolanaFederation.Features.SolanaRpc.Extensions
{
    static class RequestResultExtensions
    {
        public static void ThrowIfError<T>(this RequestResult<T> requestResult)
        {
            if (!requestResult.WasSuccessful)
            {
                throw new SolanaRpcException(
                    $"Solana RPC ERROR. Raw response: {requestResult.RawRpcResponse}, HttpStatusCode: {requestResult.HttpStatusCode}, Reason: {requestResult.Reason}, ServerErrorCode: {requestResult.ServerErrorCode}, Request: {requestResult.RawRpcRequest}");
            }
        }
    }
}