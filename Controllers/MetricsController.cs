using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Prometheus.Client;
using Prometheus.Client.Collectors;

namespace IRIPrometheusExporter.Controllers
{
    [Route("[controller]")]
    public class MetricsController : Controller
    {
        private static readonly Uri IriApiUri = new Uri(Environment.GetEnvironmentVariable("IRI_API_URI") ?? "http://localhost:14265");

        private static bool TryGetNeighbors(out NeighborsResponse response)
        {
            response = PostToIri<NeighborsResponse>(
                new StringContent(
                    "{\"command\":\"getNeighbors\"}",
                    Encoding.UTF8,
                    "application/json"));
            return response != null;
        }

        private static bool TryGetNodeInfo(out NodeInfoResponse response)
        {
            response = PostToIri<NodeInfoResponse>(
                new StringContent(
                    "{\"command\":\"getNodeInfo\"}",
                    Encoding.UTF8,
                    "application/json"));
            return response != null;
        }

        private static T PostToIri<T>(HttpContent content)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var neighborsRequest = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = IriApiUri,
                        Content = content
                    };

                    neighborsRequest.Headers.Add("X-IOTA-API-Version", "1");

                    var neighborsResponse = httpClient.SendAsync(neighborsRequest).GetAwaiter().GetResult();

                    var neighborResponseBody = neighborsResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return JsonConvert.DeserializeObject<T>(neighborResponseBody);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return default(T);
            }
        }

        [HttpGet]
        public void Get()
        {
            var registry = CollectorRegistry.Instance;
            try
            {
                var acceptHeaders = Request.Headers["Accept"];
                var contentType = ScrapeHandler.GetContentType(acceptHeaders);
                Response.ContentType = contentType;
                Response.StatusCode = 200;

                var neighborLabels = new[] {"address", "connectionType"};

                var numberOfAllTransactions = Metrics.CreateCounter("numberOfAllTransactions", "", neighborLabels);
                var numberOfRandomTransactionRequests = Metrics.CreateCounter("numberOfRandomTransactionRequests", "", neighborLabels);
                var numberOfNewTransactions = Metrics.CreateCounter("numberOfNewTransactions", "", neighborLabels);
                var numberOfInvalidTransactions = Metrics.CreateCounter("numberOfInvalidTransactions", "", neighborLabels);
                var numberOfSentTransactions = Metrics.CreateCounter("numberOfSentTransactions", "", neighborLabels);

                if (TryGetNodeInfo(out var nodeInfo))
                {
                    var latestMilestoneIndex = Metrics.CreateCounter("latestMilestoneIndex", "");
                    var latestSolidSubtangleMilestoneIndex = Metrics.CreateCounter("latestSolidSubtangleMilestoneIndex", "");
                    var numberOfNeighbors = Metrics.CreateCounter("numberOfNeighbors", "");
                    var numberOfTips = Metrics.CreateCounter("numberOfTips", "");
                    var numberOfTransactionsToRequest = Metrics.CreateCounter("numberOfTransactionsToRequest", "");

                    latestMilestoneIndex.Inc(nodeInfo.LatestMilestoneIndex);
                    latestSolidSubtangleMilestoneIndex.Inc(nodeInfo.LatestSolidSubtangleMilestoneIndex);
                    numberOfNeighbors.Inc(nodeInfo.Neighbors);
                    numberOfTips.Inc(nodeInfo.Tips);
                    numberOfTransactionsToRequest.Inc(nodeInfo.TransactionsToRequest);
                }

                if (TryGetNeighbors(out var neighborsResponse))
                {
                    foreach (var neighbor in neighborsResponse.Neighbors)
                    {
                        numberOfAllTransactions.Labels(neighbor.Address, neighbor.ConnectionType)
                            .Inc(neighbor.NumberOfAllTransactions);
                        numberOfRandomTransactionRequests.Labels(neighbor.Address, neighbor.ConnectionType)
                            .Inc(neighbor.NumberOfRandomTransactionRequests);
                        numberOfNewTransactions.Labels(neighbor.Address, neighbor.ConnectionType)
                            .Inc(neighbor.NumberOfNewTransactions);
                        numberOfInvalidTransactions.Labels(neighbor.Address, neighbor.ConnectionType)
                            .Inc(neighbor.NumberOfInvalidTransactions);
                        numberOfSentTransactions.Labels(neighbor.Address, neighbor.ConnectionType)
                            .Inc(neighbor.NumberOfSentTransactions);
                    }
                }

                using (var outputStream = Response.Body)
                {
                    var collected = registry.CollectAll();
                    ScrapeHandler.ProcessScrapeRequest(collected, contentType, outputStream);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
            finally
            {
                registry.Clear();
            }
        }
    }

    [DataContract]
    public class NodeInfoResponse
    {
        [DataMember]
        public string AppName { get; set; }

        [DataMember]
        public string AppVersion { get; set; }

        [DataMember]
        public long Duration { get; set; }

        [DataMember]
        public long JreAvailableProcessors { get; set; }

        [DataMember]
        public long JreFreeMemory { get; set; }

        [DataMember]
        public long JreMaxMemory { get; set; }

        [DataMember]
        public long JreTotalMemory { get; set; }

        [DataMember]
        public string JreVersion { get; set; }

        [DataMember]
        public string LatestMilestone { get; set; }

        [DataMember]
        public long LatestMilestoneIndex { get; set; }

        [DataMember]
        public string LatestSolidSubtangleMilestone { get; set; }

        [DataMember]
        public long LatestSolidSubtangleMilestoneIndex { get; set; }

        [DataMember]
        public long Neighbors { get; set; }

        [DataMember]
        public long PacketsQueueSize { get; set; }

        [DataMember]
        public long Time { get; set; }

        [DataMember]
        public long Tips { get; set; }

        [DataMember]
        public long TransactionsToRequest { get; set; }
    }

    [DataContract]
    public class NeighborsResponse
    {
        [DataMember]
        public List<Neighbor> Neighbors { get; set; }

        [DataMember]
        public long Duration { get; set; }
    }

    [DataContract]
    public class Neighbor
    {
        [DataMember]
        public string Address { get; set; }

        [DataMember]
        public long NumberOfAllTransactions { get; set; }

        [DataMember]
        public long NumberOfRandomTransactionRequests { get; set; }

        [DataMember]
        public long NumberOfNewTransactions { get; set; }

        [DataMember]
        public long NumberOfInvalidTransactions { get; set; }

        [DataMember]
        public long NumberOfSentTransactions { get; set; }

        [DataMember]
        public string ConnectionType { get; set; }
    }
}