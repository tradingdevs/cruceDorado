using apiBinance.Bussines;
using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;

internal static class Program
{
    private const string ApiKey = "";
    private const string Secret = "";
   
    static async Task Main(string[] args)
    {
        var credentials = new ApiCredentials(ApiKey, Secret);
        var reconnectInterval = TimeSpan.FromMinutes(30); // Intervalo de reconexión de 30 minutos
        using IBinanceRestClient binanceRestClient = new BinanceRestClient(options => { options.ApiCredentials = credentials; });
        var pingAsyncResult = await binanceRestClient.UsdFuturesApi.ExchangeData.PingAsync();

        while (true)
        {
            try
            {
                using IBinanceSocketClient binanceSocketClient = new BinanceSocketClient(options =>
                {
                    options.ApiCredentials = credentials;
                });

                using MarketBussines market = new MarketBussines(binanceSocketClient, binanceRestClient);

                // Iniciar la conexión al WebSocket
                await market.Start("ADAUSDT");

                Console.WriteLine("Presione Enter para terminar");

                // Inicia un bucle que intenta reconectar cada 30 minutos
                var reconnectionTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(reconnectInterval);
                        Console.WriteLine("Intentando reconectar...");

                        try
                        {
                            await binanceSocketClient.UsdFuturesApi.ReconnectAsync();
                            Console.WriteLine("Reconexión exitosa");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al intentar reconectar: {ex.Message}");
                        }
                    }
                });

                Console.ReadLine();
                break; // Salir del bucle si el usuario termina la aplicación
            }
            catch (SocketException ex) // Excepción específica de conexión
            {
                Console.WriteLine($"Error de conexión: {ex.Message}");
                Console.WriteLine($"Reconectando en {reconnectInterval.TotalSeconds} segundos...");
                await Task.Delay(reconnectInterval);
            }
            catch (Exception ex) // Otros errores generales
            {
                Console.WriteLine($"Error no relacionado con la conexión: {ex.Message}");
                Console.WriteLine("Finalizando sin reconexión.");
                break; // Salir del ciclo si el error no es de conexión
            }
        }
    }


}
