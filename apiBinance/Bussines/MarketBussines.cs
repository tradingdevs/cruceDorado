using apiBinance.Entities;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace apiBinance.Bussines
{
    public class MarketBussines : IDisposable
    {
        private readonly IBinanceSocketClient _webSocketClient;
        private readonly IBinanceRestClient _binanceRestClient;
        private UpdateSubscription _subscription;
        private MediaMovil _mediaMovil;  // Clase para calcular medias móviles
        private decimal Quantity = 15m;
        private static readonly TimeSpan ReceiveWindow = TimeSpan.FromMilliseconds(1000);
        private bool trading = false;

        public MarketBussines(IBinanceSocketClient webSocketClient, IBinanceRestClient binanceRestClient)
        {
            _webSocketClient = webSocketClient;
            _binanceRestClient = binanceRestClient;
            _mediaMovil = new MediaMovil();
        }

        public async Task Start(string symbol)
        {
            OrderFuturesRequest orderFuturesRequest;
             // Manejar suscripciones con control de errores
             var result = await _webSocketClient.UsdFuturesApi.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneMinute, async data =>
            {
                var kline = data.Data.Data;

                // Verificamos si la vela está cerrada antes de usar el precio de cierre
                if (kline.Final)
                {
                    Console.WriteLine($"Nuevo precio de cierre recibido para {symbol}: {kline.ClosePrice}");

                    // Agregar el precio de cierre a la clase de medias móviles
                    _mediaMovil.AgregarPrecio(kline.ClosePrice);

                    // Si hay suficientes datos, calcular el cruce dorado
                    if (_mediaMovil.MediaLarga > 0)  // Verificar si ya tenemos suficientes datos para la media larga
                    {
                        OrderType type= await CalcularCruceDorado();
                        if (type!=OrderType.Neutral)
                        {
                            orderFuturesRequest = new()
                            {
                                Symbol = symbol,
                                Side = type==OrderType.Buy ? OrderSide.Buy:OrderSide.Sell,
                                PositionSide = type == OrderType.Buy ? PositionSide.Long : PositionSide.Short,
                                OrderType = FuturesOrderType.Market,
                                Quantity = Quantity,
                                NewClientOrderId = $"market-bot-{Guid.NewGuid():N}".Substring(0, 36),
                                RecvWindow = (int)ReceiveWindow.TotalMilliseconds
                            };
                            try
                            {
                                ValidateTrading tarding = await ValidarPosición(symbol);
                                if (!tarding.Value)
                                {
                                    BinanceUsdFuturesOrder createOrderResponse = await CreateOrderAsync(orderFuturesRequest);
                                    if (createOrderResponse == null)
                                    {
                                        Console.WriteLine($"Error al intentar crear orden en el simbolo {orderFuturesRequest.Symbol}");
                                    }
                                }
                                else 
                                {
                                    if (tarding.PositionSide== PositionSide.Long && type==OrderType.Sell)
                                    {
                                        orderFuturesRequest.Quantity = tarding.Quantity;
                                        orderFuturesRequest.Side=OrderSide.Buy;
                                        BinanceUsdFuturesOrder createOrderResponse = await CreateOrderAsync(orderFuturesRequest);
                                        if (createOrderResponse == null)
                                        {
                                            Console.WriteLine($"Error al intentar cerrar posición en el simbolo {orderFuturesRequest.Symbol}");
                                        }
                                    }
                                    if (tarding.PositionSide == PositionSide.Short && type == OrderType.Buy)
                                    {
                                        orderFuturesRequest.Quantity = tarding.Quantity;
                                        orderFuturesRequest.Side = OrderSide.Sell;
                                        BinanceUsdFuturesOrder createOrderResponse = await CreateOrderAsync(orderFuturesRequest);
                                        if (createOrderResponse == null)
                                        {
                                            Console.WriteLine($"Error al intentar cerrar posición en el simbolo {orderFuturesRequest.Symbol}");
                                        }
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error al intentar crear orden en el simbolo {orderFuturesRequest.Symbol}: {ex.Message}");
                            }
                        }
                    }
                }
            });

            if (!result.Success)
            {
                // error de conexión
                if (result.Error is SocketException || result.Error?.Message.Contains("Network", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new SocketException(); // Disparar reconexión si es un error de red
                }
                else
                {
                    throw new Exception($"Error al suscribirse: {result.Error?.Message}");
                }
            }

            _subscription = result.Data;
            Console.WriteLine($"Suscripción exitosa a {symbol}");
        }

        private async Task<ValidateTrading> ValidarPosición(string symbol)
        {
            ValidateTrading validateTrading = new();
            var posiciones = await _binanceRestClient.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
            var data = posiciones.Data.ToList();

            if (Math.Abs(data[0].Quantity) > 0)
            {
                validateTrading.PositionSide = data[1].PositionSide;
                validateTrading.Quantity = Math.Abs(data[0].Quantity);
                validateTrading.Value = true;
            }
            if (Math.Abs(data[1].Quantity) > 0)
            {
                validateTrading.PositionSide = data[1].PositionSide;
                validateTrading.Quantity = Math.Abs(data[1].Quantity);
                validateTrading.Value = true;
            }
            return validateTrading;
        }
        public void Stop()
        {
            // Desuscribir si existe una suscripción activa
            if (_subscription != null)
            {
                _webSocketClient.UnsubscribeAsync(_subscription);
            }
        }

        public void Dispose()
        {
            // Detener el WebSocket y limpiar recursos
            Stop();
            _webSocketClient?.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<BinanceUsdFuturesOrder> CreateOrderAsync(OrderFuturesRequest order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));


            WebCallResult<BinanceUsdFuturesOrder> response = await _binanceRestClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                    // general
                    order.Symbol,
                    order.Side,
                    order.OrderType,
                    quantity: order.Quantity,
                    price: order.Price,
                    positionSide: order.PositionSide,
                    stopPrice: order.StopPrice,
                    // metadata
                    closePosition: order.ClosePosition,
                    reduceOnly: order.ReduceOnly,
                    newClientOrderId: order.NewClientOrderId,
                    timeInForce: order.TimeInForce,
                    receiveWindow: order.RecvWindow)
                .ConfigureAwait(false);


            if (response.Error != null || response.Success != true)
            {
                throw new ArgumentNullException(response.Error.Message);
            }

            return response.Data;
        }
        private async Task<OrderType> CalcularCruceDorado()
        {
            // Usamos las propiedades de la clase MediaMovil para calcular el cruce dorado
            var mediaCorta = _mediaMovil.MediaCorta;
            var mediaLarga = _mediaMovil.MediaLarga;

            Console.WriteLine($"Media Móvil de 50 periodos: {mediaCorta}, Media Móvil de 200 periodos: {mediaLarga}");

            // Verificamos si hay cruce dorado
            if (mediaCorta > mediaLarga)
            {
                Console.WriteLine("Señal de compra (Cruce Dorado)");
                return OrderType.Buy;
            }
            else if (mediaCorta < mediaLarga)
            {
                Console.WriteLine("Señal de venta");
                return OrderType.Sell;
            }
            else
            {
                Console.WriteLine("No hay cruce");
            }
            return OrderType.Neutral;
        }
    }

}

