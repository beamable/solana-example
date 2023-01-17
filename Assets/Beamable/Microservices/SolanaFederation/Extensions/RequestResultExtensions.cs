using Beamable.Microservices.SolanaFederation.Exceptions;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Messages;

namespace Beamable.Microservices.SolanaFederation.Extensions
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