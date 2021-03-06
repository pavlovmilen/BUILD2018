﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LiveSample
{
    class Program
    {
        private static string liveEventName;

        private static void Main(string[] args)
        {
            try
            {
                ConfigWrapper config = new ConfigWrapper();

                IAzureMediaServicesClient client = CreateMediaServicesClient(config);
                // Set the polling interval for long running operations to 2 seconds.
                // The default value is 30 seconds for the .NET client SDK
                client.LongRunningOperationRetryTimeout = 2;

                // CleanupAccount(client, config.ResourceGroup, config.AccountName);

                // Getting the mediaServices account so that we can use the location to create the
                // LiveEvent and StreamingEndpoint
                MediaService mediaService = client.Mediaservices.Get(config.ResourceGroup, config.AccountName);

                // Creating a unique suffix so that we don't have name collisions if you run the sample
                // multiple times without cleaning up.
                string uniqueness = Guid.NewGuid().ToString().Substring(0, 13);
                liveEventName = "liveevent-" + uniqueness;

                Console.WriteLine($"Creating a live event named {liveEventName}");
                Console.WriteLine();

                LiveEventPreview liveEventPreview = new LiveEventPreview
                {
                    AccessControl = new LiveEventPreviewAccessControl(
                        ip: new IPAccessControl(
                            allow: new IPRange[]
                            {
                                new IPRange (
                                    name: "AllowAll",
                                    address: "0.0.0.0",
                                    subnetPrefixLength: 0
                                )
                            }
                        )
                    )
                };

                // This can sometimes take awhile. Be patient.
                LiveEvent liveEvent = new LiveEvent(
                    location: mediaService.Location, 
                    description:"Sample LiveEvent for testing",
                    vanityUrl:false,
                    encoding: new LiveEventEncoding(
                                // Set this to Basic to enable a transcoding LiveEvent, and None to enable a pass-through LiveEvent
                                encodingType:LiveEventEncodingType.None, 
                                presetName:null
                            ),
                    input: new LiveEventInput(LiveEventInputProtocol.RTMP), 
                    preview: liveEventPreview,
                    streamOptions: new List<StreamOptionsFlag?>()
                    {
                        // Set this to Default or Low Latency
                        StreamOptionsFlag.Default
                    }
                );

                Console.WriteLine($"Creating the LiveEvent, be patient this can take time...");
                liveEvent = client.LiveEvents.Create(config.ResourceGroup, config.AccountName, liveEventName, liveEvent, autoStart:true);

            

                // Get the input endpoint to configure the on premise encoder with
                string ingestUrl = liveEvent.Input.Endpoints.First().Url;
                Console.WriteLine($"The ingest url to configure the on premise encoder with is:");
                Console.WriteLine($"\t{ingestUrl}");
                Console.WriteLine();

                // Use the previewEndpoint to preview and verify
                // that the input from the encoder is actually being received
                string previewEndpoint = liveEvent.Preview.Endpoints.First().Url;
                Console.WriteLine($"The preview url is:");
                Console.WriteLine($"\t{previewEndpoint}");
                Console.WriteLine();

                Console.WriteLine($"Open the live preview in your browser and use the Azure Media Player to monitor the preview playback:");
                Console.WriteLine($"\thttps://ampdemo.azureedge.net/?url={previewEndpoint}");
                Console.WriteLine();

                Console.WriteLine("Start the live stream now, sending the input to the ingest url and verify that it is arriving with the preview url.");
                Console.WriteLine("IMPORTANT TIP!: Make ABSOLUTLEY CERTAIN that the video is flowing to the Preview URL before continuing!");
                Console.WriteLine("Press enter to continue...");
                Console.Out.Flush();
                var ignoredInput = Console.ReadLine();

                // Create an Asset for the LiveOutput to use
                string assetName = "archiveAsset" + uniqueness;
                Console.WriteLine($"Creating an asset named {assetName}");
                Console.WriteLine();
                Asset asset = client.Assets.CreateOrUpdate(config.ResourceGroup, config.AccountName, assetName, new Asset());

                // Create the LiveOutput
                string manifestName = "output";
                string liveOutputName = "liveOutput" + uniqueness;
                Console.WriteLine($"Creating a live output named {liveOutputName}");
                Console.WriteLine();

                LiveOutput liveOutput = new LiveOutput(assetName: asset.Name, manifestName: manifestName, archiveWindowLength: TimeSpan.FromMinutes(10));
                liveOutput = client.LiveOutputs.Create(config.ResourceGroup, config.AccountName, liveEventName, liveOutputName, liveOutput);

                // Create the StreamingLocator
                string streamingLocatorName = "streamingLocator" + uniqueness;

                Console.WriteLine($"Creating a streaming locator named {streamingLocatorName}");
                Console.WriteLine();

                StreamingLocator locator = new StreamingLocator(assetName: assetName, streamingPolicyName: PredefinedStreamingPolicy.ClearStreamingOnly);
                locator = client.StreamingLocators.Create(config.ResourceGroup, config.AccountName, streamingLocatorName, locator);

                // Get the default Streaming Endpoint on the account
                StreamingEndpoint streamingEndpoint = client.StreamingEndpoints.Get(config.ResourceGroup, config.AccountName, "default");

                // If it's not running, Start it. 
                if (streamingEndpoint.ResourceState != StreamingEndpointResourceState.Running)
                {
                    Console.WriteLine("Streaming Endpoint was Stopped, restarting now..");
                    client.StreamingEndpoints.Start(config.ResourceGroup, config.AccountName, "default");
                }

                // Get the url to stream the output
                var paths = client.StreamingLocators.ListPaths(config.ResourceGroup, config.AccountName, streamingLocatorName);

                Console.WriteLine("The urls to stream the output from a client:");
                Console.WriteLine();
                StringBuilder stringBuilder = new StringBuilder();
                string playerPath = string.Empty;

                for (int i = 0; i < paths.StreamingPaths.Count; i++)
                {
                    UriBuilder uriBuilder = new UriBuilder();
                    uriBuilder.Scheme = "https";
                    uriBuilder.Host = streamingEndpoint.HostName;

                    if (paths.StreamingPaths[i].Paths.Count > 0)
                    {
                        uriBuilder.Path = paths.StreamingPaths[i].Paths[0];
                        stringBuilder.AppendLine($"\t{paths.StreamingPaths[i].StreamingProtocol}-{paths.StreamingPaths[i].EncryptionScheme}");
                        stringBuilder.AppendLine($"\t\t{uriBuilder.ToString()}");
                        stringBuilder.AppendLine();
                    
                        if (paths.StreamingPaths[i].StreamingProtocol == StreamingPolicyStreamingProtocol.Dash){
                            playerPath = uriBuilder.ToString();
                        }
                    }                
                }

                if (stringBuilder.Length > 0)
                {
                    Console.WriteLine(stringBuilder.ToString());
                    Console.WriteLine("Open the following URL to playback the published,recording LiveOutput in the Azure Media Player");
                    Console.WriteLine($"\t https://ampdemo.azureedge.net/?url={playerPath}");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("No Streaming Paths were detected.  Has the Stream been started?");
                    Console.WriteLine("Cleaning up and Exiting...");

                    CleanupLiveEventAndOutput(client, config.ResourceGroup, config.AccountName, liveEventName, liveOutputName);
                    CleanupLocatorAssetAndStreamingEndpoint(client, config.ResourceGroup, config.AccountName, streamingLocatorName, assetName);

                    return;
                }

                Console.WriteLine("Continue experimenting with the stream until you are ready to finish.");
                Console.WriteLine("Press enter to stop the LiveOutput...");
                Console.Out.Flush();
                ignoredInput = Console.ReadLine();

                CleanupLiveEventAndOutput(client, config.ResourceGroup, config.AccountName, liveEventName, liveOutputName);

                Console.WriteLine("The LiveOutput and LiveEvent are now deleted.  The event is available as an archive and can still be streamed.");
                Console.WriteLine("Press enter to finish cleanup...");
                Console.Out.Flush();
                ignoredInput = Console.ReadLine();

                CleanupLocatorAssetAndStreamingEndpoint(client, config.ResourceGroup, config.AccountName, streamingLocatorName, assetName);
            }
            catch (ApiErrorException e)
            {
                Console.WriteLine("Hit ApiErrorException");
                Console.WriteLine($"\tCode: {e.Body.Error.Code}");
                Console.WriteLine($"\tCode: {e.Body.Error.Message}");
                Console.WriteLine();
                Console.WriteLine("Exiting, cleanup may be necessary...");
                Console.ReadLine();
            }
        }

        private static void CleanupLiveEventAndOutput(IAzureMediaServicesClient client, string resourceGroup, string accountName, string liveEventName, string liveOutputName)
        {
            // Delete the LiveOutput
            client.LiveOutputs.Delete(resourceGroup, accountName, liveEventName, liveOutputName);

            // Stop and delete the LiveEvent
            client.LiveEvents.Stop(resourceGroup, accountName, liveEventName);
            client.LiveEvents.Delete(resourceGroup, accountName, liveEventName);
        }

        private static void CleanupLocatorAssetAndStreamingEndpoint(IAzureMediaServicesClient client, string resourceGroup, string accountName, string streamingLocatorName, string assetName)
        {
            // Delete the Streaming Locator
            client.StreamingLocators.Delete(resourceGroup, accountName, streamingLocatorName);

            // Delete the Archive Asset
            client.Assets.Delete(resourceGroup, accountName, assetName);

            // Stop and delete the StreamingEndpoint
            // client.StreamingEndpoints.Stop(resourceGroup, accountName, endpointName);
            // client.StreamingEndpoints.Delete(resourceGroup, accountName, endpointName);

        }

        private static IAzureMediaServicesClient CreateMediaServicesClient(ConfigWrapper config)
        {
            ArmClientCredentials credentials = new ArmClientCredentials(config);

            return new AzureMediaServicesClient(config.ArmEndpoint, credentials)
            {
                SubscriptionId = config.SubscriptionId,
            };
        }

        
        private static void CleanupAccount(IAzureMediaServicesClient client, string resourceGroup, string accountName)
        {
            try{
                
                Console.WriteLine("Cleaning up the resources used, stopping the LiveEvent. This can take a few minutes to complete.");
                Console.WriteLine();

                var events = client.LiveEvents.List(resourceGroup, accountName);
                
                foreach (LiveEvent l in events)
                {
                    if (l.Name == liveEventName){
                        var outputs = client.LiveOutputs.List(resourceGroup, accountName, l.Name);

                        foreach (LiveOutput o in outputs)
                        {
                            client.LiveOutputs.Delete(resourceGroup, accountName, l.Name, o.Name);
                             Console.WriteLine($"LiveOutput: {o.Name} deleted from LiveEvent {l.Name}. The archived Asset and Streaming URLs are still retained for on-demand viewing.");
                        }

                        if (l.ResourceState == LiveEventResourceState.Running){
                            client.LiveEvents.Stop(resourceGroup, accountName, l.Name);
                            Console.WriteLine($"LiveEvent: {l.Name} Stopped.");
                            client.LiveEvents.Delete(resourceGroup, accountName, l.Name);
                            Console.WriteLine($"LiveEvent: {l.Name} Deleted.");
                            Console.WriteLine();
                        }
                    }
                }

            } 
            catch(ApiErrorException e)
            {
                Console.WriteLine("Hit ApiErrorException");
                Console.WriteLine($"\tCode: {e.Body.Error.Code}");
                Console.WriteLine($"\tCode: {e.Body.Error.Message}");
                Console.WriteLine();

            }
        }
    }
}

