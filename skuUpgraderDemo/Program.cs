using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace skuUpgraderDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            string initialSku = ConfigurationManager.AppSettings["InitialSku"];
            string targetSku = ConfigurationManager.AppSettings["TargetSku"];
            /* 
             *  When upgrading the tier it is important to know that you cannot downgrade from B1 back to D1
             *  And you cannot downgrade from S1 to B1
             */
            SkuChanger.LoginAndUpdateSKUAsync("B1").Wait();
            SkuChanger.EnsureAnalysisServicesReady();
            var sku = SkuChanger.GetCurrentAASSku();
            Console.WriteLine("The new sku is: " + sku);
        }
    }
}
