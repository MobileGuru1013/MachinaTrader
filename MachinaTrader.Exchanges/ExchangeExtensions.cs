using MachinaTrader.Globals.Structure.Enums;
using System;

namespace MachinaTrader.Exchanges
{
    public static class ExchangeExtensions
    {
        public static OrderStatus ToOrderStatus(this ExchangeSharp.ExchangeAPIOrderResult input)
        {
            switch (input)
            {
                case ExchangeSharp.ExchangeAPIOrderResult.Canceled:
                    return OrderStatus.Canceled;
                case ExchangeSharp.ExchangeAPIOrderResult.Error:
                    return OrderStatus.Error;
                case ExchangeSharp.ExchangeAPIOrderResult.Filled:
                    return OrderStatus.Filled;
                case ExchangeSharp.ExchangeAPIOrderResult.FilledPartially:
                    return OrderStatus.PartiallyFilled;
                case ExchangeSharp.ExchangeAPIOrderResult.Pending:
                    return OrderStatus.New;
                case ExchangeSharp.ExchangeAPIOrderResult.Unknown:
                    return OrderStatus.Unknown;
            }

            throw new ArgumentException($"{input} is an unknown OrderStatus");
        }
    }
}
