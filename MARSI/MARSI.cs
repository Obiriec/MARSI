﻿/*  CTRADER GURU --> Template 1.0.4

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/channel/UCKkgbw09Fifj65W5t5lHeCQ
    GitHub      : https://github.com/cTraderGURU/
    TOS         : https://ctrader.guru/termini-del-servizio/

*/

using System;
using System.IO;
using cAlgo.API;
using System.Net;
using System.Text;
using cAlgo.API.Indicators;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

// --> Microsoft Visual Studio 2017 --> Strumenti --> Gestione pacchetti NuGet --> Gestisci pacchetti NuGet per la soluzione... --> Installa
using Newtonsoft.Json;

namespace cAlgo
{

    // --> AccessRights = AccessRights.FullAccess se si vuole controllare gli aggiornamenti
    [Levels(20, 30, 70, 80)]
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, ScalePrecision = 1, AccessRights = AccessRights.FullAccess)]
    public class MARSI : Indicator
    {

        #region Enums

        // --> Eventuali enumeratori li mettiamo qui

        #endregion

        #region Identity

        /// <summary>
        /// ID prodotto, identificativo, viene fornito da ctrader.guru, 74909 è il riferimento del template in uso
        /// </summary>
        public const int ID = 63902;

        /// <summary>
        /// Nome del prodotto, identificativo, da modificare con il nome della propria creazione
        /// </summary>
        public const string NAME = "MARSI";

        /// <summary>
        /// La versione del prodotto, progressivo, utilie per controllare gli aggiornamenti se viene reso disponibile sul sito ctrader.guru
        /// </summary>
        public const string VERSION = "1.0.3";

        #endregion

        #region Params

        /// <summary>
        /// Identità del prodotto nel contesto di ctrader.guru
        /// </summary>
        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = "https://ctrader.guru/product/marsi/")]
        public string ProductInfo { get; set; }

        [Parameter("MA Period", Group = "MA", DefaultValue = 20)]
        public int MAPeriods { get; set; }

        [Parameter("MA Type", Group = "MA", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MaType { get; set; }

        [Parameter("MA Source", Group = "MA")]
        public DataSeries Source { get; set; }

        [Parameter("RSI Period", Group = "RSI", DefaultValue = 20)]
        public int RSIPeriods { get; set; }

        [Parameter("Fast Period", Group = "RSI", DefaultValue = 10)]
        public int TPeriods { get; set; }

        [Output("RSI", LineColor = "Red")]
        public IndicatorDataSeries Rsi { get; set; }

        [Output("Slow", LineColor = "DodgerBlue")]
        public IndicatorDataSeries Trigger { get; set; }

        #endregion

        #region Property

        private RelativeStrengthIndex _rsi;
        private MovingAverage _ma;
        private ExponentialMovingAverage _ema;

        #endregion

        #region Indicator Events

        /// <summary>
        /// Viene generato all'avvio dell'indicatore, si inizializza l'indicatore
        /// </summary>
        protected override void Initialize()
        {

            // --> Stampo nei log la versione corrente
            Print("{0} : {1}", NAME, VERSION);

            // --> Se viene settato l'ID effettua un controllo per verificare eventuali aggiornamenti
            _checkProductUpdate();

            _ma = Indicators.MovingAverage(Source, MAPeriods, MaType);
            _rsi = Indicators.RelativeStrengthIndex(_ma.Result, RSIPeriods);
            _ema = Indicators.ExponentialMovingAverage(_rsi.Result, TPeriods);

        }

        /// <summary>
        /// Generato ad ogni tick, vengono effettuati i calcoli dell'indicatore
        /// </summary>
        /// <param name="index">L'indice della candela in elaborazione</param>
        public override void Calculate(int index)
        {

            Rsi[index] = _rsi.Result[index];
            Trigger[index] = _ema.Result[index];

        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Effettua un controllo sul sito ctrader.guru per mezzo delle API per verificare la presenza di aggiornamenti, solo in realtime
        /// </summary>
        private void _checkProductUpdate()
        {

            // --> Controllo solo se solo in realtime, evito le chiamate in backtest
            if (RunningMode != RunningMode.RealTime)
                return;

            // --> Organizzo i dati per la richiesta degli aggiornamenti
            Guru.API.RequestProductInfo Request = new Guru.API.RequestProductInfo
            {

                MyProduct = new Guru.Product
                {

                    ID = ID,
                    Name = NAME,
                    Version = VERSION

                },
                AccountBroker = Account.BrokerName,
                AccountNumber = Account.Number

            };

            // --> Effettuo la richiesta
            Guru.API Response = new Guru.API(Request);

            // --> Controllo per prima cosa la presenza di errori di comunicazioni
            if (Response.ProductInfo.Exception != "")
            {

                Print("{0} Exception : {1}", NAME, Response.ProductInfo.Exception);

            }
            // --> Chiedo conferma della presenza di nuovi aggiornamenti
            else if (Response.HaveNewUpdate())
            {

                string updatemex = string.Format("{0} : Updates available {1} ( {2} )", NAME, Response.ProductInfo.LastProduct.Version, Response.ProductInfo.LastProduct.Updated);

                // --> Informo l'utente con un messaggio sul grafico e nei log del cbot
                Chart.DrawStaticText(NAME + "Updates", updatemex, VerticalAlignment.Top, HorizontalAlignment.Left, Color.Red);
                Print(updatemex);

            }

        }

        #endregion

    }

}


/// <summary>
/// NameSpace che racchiude tutte le feature ctrader.guru
/// </summary>
namespace Guru
{
    /// <summary>
    /// Classe che definisce lo standard identificativo del prodotto nel marketplace ctrader.guru
    /// </summary>
    public class Product
    {

        public int ID = 0;
        public string Name = "";
        public string Version = "";
        public string Updated = "";

    }

    public class CookieInformation
    {

        public DateTime LastCheck = new DateTime();

    }

    /// <summary>
    /// Offre la possibilità di utilizzare le API messe a disposizione da ctrader.guru per verificare gli aggiornamenti del prodotto.
    /// Permessi utente "AccessRights = AccessRights.FullAccess" per accedere a internet ed utilizzare JSON
    /// </summary>
    public class API
    {
        /// <summary>
        /// Costante da non modificare, corrisponde alla pagina dei servizi API
        /// </summary>
        private const string Service = "https://ctrader.guru/api/product_info/";

        /// <summary>
        /// Costante da non modificare, utilizzata per filtrare le richieste
        /// </summary>
        private const string UserAgent = "cTrader Guru";

        /// <summary>
        /// Variabile dove verranno inserite le direttive per la richiesta
        /// </summary>
        private RequestProductInfo RequestProduct = new RequestProductInfo();

        /// <summary>
        /// Il percorso della cartella dove riporre i cookie
        /// </summary>
        private readonly string _mainpath = string.Format("{0}\\cAlgo\\cTrader GURU\\Cookie", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        /// <summary>
        /// Il percorso completo del file che verrà utilizzato per il controllo degli aggiornamenti
        /// </summary>
        private readonly string _pathsetup;

        /// <summary>
        /// Legge e rende disponibile i contenuti del cookie
        /// </summary>
        /// <returns></returns>
        private string _loadSetup()
        {

            try
            {

                using (StreamReader r = new StreamReader(_pathsetup))
                {
                    string json = r.ReadToEnd();

                    return json;
                }

            }
            catch
            {

                return null;

            }

        }

        /// <summary>
        /// Scrive i valori del cookie
        /// </summary>
        /// <param name="mysetup">I valori da registrare</param>
        /// <returns></returns>
        private bool _writeSetup(CookieInformation mysetup)
        {

            try
            {

                Directory.CreateDirectory(_mainpath);

                using (StreamWriter file = File.CreateText(_pathsetup))
                {

                    JsonSerializer serializer = new JsonSerializer();

                    serializer.Serialize(file, mysetup);

                }

                return true;

            }
            catch
            {

                return false;

            }

        }

        /// <summary>
        /// Variabile dove verranno inserite le informazioni identificative dal server dopo l'inizializzazione della classe API
        /// </summary>
        public ResponseProductInfo ProductInfo = new ResponseProductInfo();

        /// <summary>
        /// Classe che formalizza i parametri di richiesta, vengono inviate le informazioni del prodotto e di profilazione a fini statistici
        /// </summary>
        public class RequestProductInfo
        {

            /// <summary>
            /// Il prodotto corrente per il quale richiediamo le informazioni
            /// </summary>
            public Product MyProduct = new Product();

            /// <summary>
            /// Broker con il quale effettiamo la richiesta
            /// </summary>
            public string AccountBroker = "";

            /// <summary>
            /// Il numero di conto con il quale chiediamo le informazioni
            /// </summary>
            public int AccountNumber = 0;

        }

        /// <summary>
        /// Classe che formalizza lo standard per identificare le informazioni del prodotto
        /// </summary>
        public class ResponseProductInfo
        {

            /// <summary>
            /// Il prodotto corrente per il quale vengono fornite le informazioni
            /// </summary>
            public Product LastProduct = new Product();

            /// <summary>
            /// Eccezioni in fase di richiesta al server, da utilizzare per controllare l'esito della comunicazione
            /// </summary>
            public string Exception = "";

            /// <summary>
            /// La risposta del server
            /// </summary>
            public string Source = "";

        }

        /// <summary>
        /// Richiede le informazioni del prodotto richiesto
        /// </summary>
        /// <param name="Request"></param>
        public API(RequestProductInfo Request)
        {

            RequestProduct = Request;

            // --> Non controllo se non ho l'ID del prodotto
            if (Request.MyProduct.ID <= 0)
                return;

            // --> Rendo disponibile il file del cookie
            _pathsetup = string.Format("{0}\\{1}.json", _mainpath, Request.MyProduct.ID);

            CookieInformation MySetup = new CookieInformation();
            DateTime now = DateTime.Now;

            // --> Evito di chiamare il server se non sono passate almeno 24h
            try
            {

                string json = _loadSetup();

                if (json != null && json.Trim().Length > 0)
                {

                    json = json.Trim();

                    MySetup = JsonConvert.DeserializeObject<CookieInformation>(json);
                    DateTime ExpireDate = MySetup.LastCheck.AddDays(1);

                    // --> Impedisco di controllare se non è passato il tempo necessario
                    if (now < ExpireDate)
                    {

                        ProductInfo.Exception = string.Format("Check for updates scheduled for {0}", ExpireDate.ToString());
                        return;

                    }

                }

            }
            catch (Exception Exp)
            {

                // --> Setup corrotto ? resetto!
                _writeSetup(MySetup);

                // --> Se ci sono errori non controllo perchè non è gestito ed evito di sovraccaricare il server che mi bloccherebbe
                ProductInfo.Exception = Exp.Message;
                return;

            }

            // --> Dobbiamo supervisionare la chiamata per registrare l'eccexione
            try
            {

                // --> Strutturo le informazioni per la richiesta POST
                NameValueCollection data = new NameValueCollection
                {
                    {
                        "account_broker",
                        Request.AccountBroker
                    },
                    {
                        "account_number",
                        Request.AccountNumber.ToString()
                    },
                    {
                        "my_version",
                        Request.MyProduct.Version
                    },
                    {
                        "productid",
                        Request.MyProduct.ID.ToString()
                    }
                };

                // --> Autorizzo tutte le pagine di questo dominio
                Uri myuri = new Uri(Service);
                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                // -->> Richiedo le informazioni al server
                using (var wb = new WebClient())
                {

                    wb.Headers.Add("User-Agent", UserAgent);

                    var response = wb.UploadValues(myuri, "POST", data);
                    ProductInfo.Source = Encoding.UTF8.GetString(response);

                }

                // -->>> Nel cBot necessita l'attivazione di "AccessRights = AccessRights.FullAccess"
                ProductInfo.LastProduct = JsonConvert.DeserializeObject<Product>(ProductInfo.Source);

                // --> Salviamo la sessione
                MySetup.LastCheck = now;
                _writeSetup(MySetup);

            }
            catch (Exception Exp)
            {

                // --> Qualcosa è andato storto, registro l'eccezione
                ProductInfo.Exception = Exp.Message;

            }

        }

        /// <summary>
        /// Esegue un confronto tra le versioni per determinare la presenza di aggiornamenti
        /// </summary>
        /// <returns></returns>
        public bool HaveNewUpdate()
        {

            // --> Voglio essere sicuro che stiamo lavorando con le informazioni giuste
            return (ProductInfo.LastProduct.ID == RequestProduct.MyProduct.ID && ProductInfo.LastProduct.Version != "" && RequestProduct.MyProduct.Version != "" && new Version(RequestProduct.MyProduct.Version).CompareTo(new Version(ProductInfo.LastProduct.Version)) < 0);

        }

    }

}