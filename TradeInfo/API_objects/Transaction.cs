using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeInfo { 
    public class Transaction
    {
        public UInt32 id;
        public DateTime time;
        public Globals.Coin coinSold;
        public double amountSold;
        public Globals.Coin coinBought;
        public double amountBought;
        public TransactionStatus status { get; set; } = TransactionStatus.PENDING;

        public override string ToString()
        {
            return ToString(8, 8);
        }

        public string ToString(int pricePrecision, int amountPrecision)
        {
            return id + " " + (time.ToString("MM/dd/yyyy-HH:mm:ss")) + " " + coinSold.marketSymbol + " " + Math.Round(amountSold, amountPrecision) + " " + coinBought.marketSymbol + " " + Math.Round(amountBought, amountPrecision);
        }


        public Transaction(UInt32 id, DateTime time, Globals.Coin coinSold, double amountSold, Globals.Coin coinBought, double amountBought)
        {
            this.id = id;
            this.time = time;
            this.coinSold = coinSold;
            this.coinBought = coinBought;
            this.amountSold = amountSold;
            this.amountBought = amountBought;
        }

    }
}
