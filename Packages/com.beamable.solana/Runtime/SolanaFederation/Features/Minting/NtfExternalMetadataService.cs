using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using Newtonsoft.Json;

namespace Beamable.Microservices.SolanaFederation.Features.Minting
{
	internal static class NtfExternalMetadataService
	{
		private static readonly HttpClient HttpClient = new();

		public static async Task<string> SaveMetadata(NftExternalMetadata metadata)
		{
			//return "https://dev-content.beamable.com/1396602546537484/DE_1396602546537487/binary/public/ntf_metadata/2.json";

			var metadataJsonString = JsonConvert.SerializeObject(metadata);
			var metadataPayload = Encoding.UTF8.GetBytes(metadataJsonString);

			using (var md5 = MD5.Create())
			{
				var md5Bytes = md5.ComputeHash(metadataPayload);
				var payloadChecksum = BitConverter.ToString(md5Bytes).Replace("-", "");

				var saveBinaryResponse = await ServiceContext.Requester.Request<SaveBinaryResponse>(Method.POST,
					"/basic/content/binary", new SaveBinaryRequest
					{
						binary = new List<BinaryDefinition>
						{
							new()
							{
								id = $"token-metadata.{metadata.name}",
								checksum = payloadChecksum,
								uploadContentType = "text/plain"
							}
						}
					});

				var binaryResponse = saveBinaryResponse.binary.First();
				var signedUrl = binaryResponse.uploadUri;

				BeamableLogger.Log("Signed url: {SignedUrl}", signedUrl);

				var content = new ByteArrayContent(metadataPayload);
				content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
				content.Headers.ContentMD5 = md5Bytes;

				var putContentResponse = await HttpClient.PutAsync(signedUrl, content);
				
				BeamableLogger.Log("Put content resulted in {StatusCode}, body: {Body}",
					putContentResponse.StatusCode.ToString(), await putContentResponse.Content.ReadAsStringAsync());
				putContentResponse.EnsureSuccessStatusCode();

				return binaryResponse.uri;
			}
		}
	}
}