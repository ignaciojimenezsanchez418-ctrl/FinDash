using FinDash.Core.Models;

namespace FinDash.Api.Data;

/// <summary>
/// Catálogo inicial. La búsqueda (US-204) se hace sobre esta tabla, sin llamar a CoinGecko
/// (contrato §2). Agregar un activo = agregar una fila aquí y crear una migración.
/// </summary>
public static class SeedData
{
    public static readonly AssetEntity[] Assets =
    [
        Fiat(Asset.UsdId, "USD", "Dólar"),
        Crypto("bitcoin", "BTC", "Bitcoin"),
        Crypto("ethereum", "ETH", "Ethereum"),
        Crypto("tether", "USDT", "Tether"),
        Crypto("usd-coin", "USDC", "USD Coin"),
        Crypto("binancecoin", "BNB", "BNB"),
        Crypto("solana", "SOL", "Solana"),
        Crypto("ripple", "XRP", "XRP"),
        Crypto("cardano", "ADA", "Cardano"),
        Crypto("dogecoin", "DOGE", "Dogecoin"),
        Crypto("tron", "TRX", "TRON"),
        Crypto("avalanche-2", "AVAX", "Avalanche"),
        Crypto("chainlink", "LINK", "Chainlink"),
        Crypto("polkadot", "DOT", "Polkadot"),
        Crypto("litecoin", "LTC", "Litecoin"),
        Crypto("bitcoin-cash", "BCH", "Bitcoin Cash"),
        Crypto("stellar", "XLM", "Stellar"),
    ];

    static AssetEntity Fiat(string id, string symbol, string name) =>
        new() { Id = id, Symbol = symbol, Name = name, Kind = AssetKind.Fiat, Source = "BCCR" };

    static AssetEntity Crypto(string id, string symbol, string name) =>
        new() { Id = id, Symbol = symbol, Name = name, Kind = AssetKind.Crypto, Source = "CoinGecko" };
}
