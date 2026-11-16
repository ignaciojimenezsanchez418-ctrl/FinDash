namespace FinDash.Core.Models;

public enum Currency { CRC, USD }

public enum AssetKind { Fiat, Crypto }

public enum TxType { Buy, Sell }

public enum AlertCondition { Above, Below }

public enum AlertState { Active, Triggered, Rearmed }

public enum ChartRange { Day, Week, Month, Year }
