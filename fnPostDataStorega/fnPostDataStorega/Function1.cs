using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace fnPostDataStorega
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("dataStorage")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processando a imagem no storage.");

            if(!req.Headers.TryGetValue("file-type", out var fileType))
            {
                return new BadRequestObjectResult("O cabeçalho 'file-type' é obrigatório.");
            }
            var fileTypeName = fileType.ToString();
            var form = await req.ReadFormAsync();
            var file = form.Files["file"];
                
            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult("O arquivo não foi enviado.");
            }

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = fileTypeName;

            BlobClient blobClient = new BlobClient(connectionString, containerName, file.FileName);
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            await containerClient.CreateIfNotExistsAsync();
            await containerClient.SetAccessPolicyAsync(PublicAccessType.BlobContainer);
                
            string blobName = file.FileName;
            var blob = containerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blob.UploadAsync(stream, true);
            }

            _logger.LogInformation($"Arquivo {file.FileName} salvo com sucesso no container {containerName}.");

            return new OkObjectResult(new 
            {
                Message = "Arquivo salvo com sucesso.",
                BlobUri = blob.Uri
            });
        }
    }
}
