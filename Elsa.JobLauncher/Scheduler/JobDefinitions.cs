using System;
using System.Collections.Generic;

namespace Elsa.JobLauncher.Scheduler
{
    internal static class JobDefinitions
    {
        public static readonly ElsaJob CustomersImport = new ElsaJob("ZAKAZNICI_IMPORT", 1, TimeSpan.FromMinutes(20), eval => eval.DidntRunMoreThan(0, 15, 0));
        public static readonly ElsaJob QuickImport = new ElsaJob("IMPORT_FLOX", 2, TimeSpan.FromMinutes(30), eval => eval.DidntRunMoreThan(0, 30, 0));
        public static readonly ElsaJob LoadPayments = new ElsaJob("STAHOVANI_PLATEB", 3, TimeSpan.FromMinutes(10), eval => eval.DidntRunMoreThan(0, 10, 0));
        public static readonly ElsaJob PayOnDeliveryProcessing = new ElsaJob("ZPRAC_DOBIRKY", 4, TimeSpan.FromMinutes(10), eval => eval.DidntRunMoreThan(0, 10, 0));

        public static readonly ElsaJob BigImport = new ElsaJob("VELKY_IMPORT_FLOX", -10, TimeSpan.FromHours(2), eval => eval.DidntRunMoreThan(6, 0, 0) && eval.NowIsBetween(0, 5));
        public static readonly ElsaJob Currencies = new ElsaJob("MENOVE_KURZY", -9, TimeSpan.FromMinutes(10), eval => eval.DidntRunMoreThan(6, 0, 0) && eval.NowIsBetween(0, 5));
        public static readonly ElsaJob Geocoding = new ElsaJob("GEOCODING", -8, TimeSpan.FromMinutes(10), eval => eval.DidntRunMoreThan(6, 0, 0) && eval.NowIsBetween(0, 5));

        public static readonly ElsaJob DbBackup = new ElsaJob("DB_BACKUP", 5, TimeSpan.FromMinutes(20), eval => eval.DidntRunMoreThan(6, 0, 0) && eval.NowIsBetween(0, 5));

        public static readonly ElsaJob FinReports = new ElsaJob("GENEROVANI_UCT_REPORTU", 1, TimeSpan.FromHours(3), eval => eval.DidntRunMoreThan(8,0,0) && eval.NowIsBetween(0, 5));

        public static readonly ElsaJob AutoQueries = new ElsaJob("AUTOMATICKE_DOTAZY", 2, TimeSpan.FromMinutes(20), eval => eval.DidntRunMoreThan(8, 0, 0) && eval.NowIsBetween(0, 5));

        public static readonly ElsaJob Inspector = new ElsaJob("INSPEKTOR", 2, TimeSpan.FromHours(1), eval => eval.DidntRunMoreThan(8,0,0) && eval.NowIsBetween(5, 7));

        public static readonly ElsaJob LogReader =
            new ElsaJob("Log_Reader", 3, TimeSpan.FromHours(1), eval => eval.DidntRunMoreThan(4, 0, 0) && eval.NowIsBetween(5, 22));

        public static readonly ElsaJob DataPush = new ElsaJob("DATA_PUSH", 1, TimeSpan.FromHours(1), eval => eval.DidntRunMoreThan(8, 0, 0) && eval.NowIsBetween(0, 5));

        public static readonly ElsaJob StoreMap = new ElsaJob("STORE_MAP", 1, TimeSpan.FromHours(1), eval => eval.DidntRunMoreThan(8, 0, 0) && eval.NowIsBetween(0, 5));

        //public static readonly ElsaJob OrderDataValidation = new ElsaJob("ORDER_DATA_VALIDATION", 1, TimeSpan.FromHours(1), eval => eval.DidntRunMoreThan(8, 0, 0) && eval.NowIsBetween(0, 5));

        public static IEnumerable<ElsaJob> All
        {
            get
            {
                yield return CustomersImport;
                yield return QuickImport;
                yield return LoadPayments;
                yield return PayOnDeliveryProcessing;

                yield return BigImport;
                yield return Currencies;
                
                yield return DbBackup;
                yield return FinReports;

                yield return AutoQueries;
                //yield return OrderDataValidation;
                
                yield return Inspector;

                yield return LogReader;
                yield return DataPush;
                yield return StoreMap;
            }
        }
    }
}
