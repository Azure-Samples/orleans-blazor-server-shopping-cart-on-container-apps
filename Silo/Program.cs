// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

await Host.CreateDefaultBuilder(args)
    .UseOrleans(
        (context, builder) =>
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                builder.UseLocalhostClustering()
                    .AddMemoryGrainStorage("shopping-cart")
                    .AddStartupTask<SeedProductStoreTask>();
            }
            else
            {
                var siloPort = 11111;
                var gatewayPort = 30000;

                // Is running as an App Service?
                if (context.Configuration["WEBSITE_PRIVATE_IP"] is string ip &&
                    context.Configuration["WEBSITE_PRIVATE_PORTS"] is string ports)
                {
                    var endpointAddress = IPAddress.Parse(ip);
                    var splitPorts = ports.Split(',');
                    if (splitPorts.Length < 2)
                    {
                        throw new Exception("Insufficient private ports configured.");
                    }

                    siloPort = int.Parse(splitPorts[0]);
                    gatewayPort = int.Parse(splitPorts[1]);

                    builder.ConfigureEndpoints(endpointAddress, siloPort, gatewayPort);
                }
                else // Assume Azure Container Apps.
                {
                    builder.ConfigureEndpoints(siloPort, gatewayPort);
                }

                var connectionString =
                    context.Configuration["ORLEANS_AZURE_STORAGE_CONNECTION_STRING"];

                builder
                    .Configure<ClusterOptions>(
                        options =>
                        {
                            options.ClusterId = "ShoppingCartCluster";
                            options.ServiceId = nameof(ShoppingCartService);
                        })
                    .UseAzureStorageClustering(
                        options => options.ConfigureTableServiceClient(connectionString))
                    .AddAzureTableGrainStorage("shopping-cart",
                        options => options.ConfigureTableServiceClient(connectionString));
            }
        })
    .ConfigureWebHostDefaults(
        webBuilder => webBuilder.UseStartup<Startup>())
    .RunConsoleAsync();