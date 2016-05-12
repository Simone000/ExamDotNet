using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
using ZedGraph;

namespace Esame8
{
    public partial class Form1 : Form
    {
        #region attributi
        private static int secondiFinestra = 5;  //secondi*hertz = numero records per finestra
        private static int HertzFinestra = 50;
        private static int K = 10; //2K = finestra di valori per lo smoothing
        private static int T = 10; //2T = finestra di valori per la derivazione standard
        private static int sogliaMaxStazionamento = 1; //soglia oltre la quale un'attività non è piu' stazionaria
        private static float durataMinimaStazionamento = 0.5F; //soglia minima per considerare un intervallo stazionario
        private float avanzamentoTempo; //di quanto avanza il tempo sull'asse delle x per ogni punto delle y.
        private SmartXLS.WorkBook foglio; //foglio per stampare i dati sul file excell (.cvs)
        private static float sogliaGirata = 10; //gradi minimi per considerare la girata
        #endregion

        #region disegna
        public void disegna(List<float> asseX, List<float> asseY, ZedGraphControl graphControl, int indiceCurva) //asseX=tempo, asseY=valori
        {
            for (int i = 0; i < asseX.Count; i++)
            {
                graphControl.GraphPane.CurveList[indiceCurva].AddPoint(asseX.ElementAt(i), asseY.ElementAt(i));
            }

            graphControl.AxisChange();
            graphControl.Invalidate();  //refresh
        }
        #endregion

        #region nuoveConnessioni
        public void ConnectionManager()
        {
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, 45555));
            tcpListener.Start();
            addText("server in ascolto");

            Socket socket;
            while (true)
            {
                socket = tcpListener.AcceptSocket();
                addText("client connesso");

                new Thread(delegate() { GestisciSocket(socket); }).Start();
            }
        }

        public void GestisciSocket(Socket socket)
        {
            Store store = new Store(avanzamentoTempo, foglio);
            gestisciPreamble(socket);
            new Thread(delegate() { GestisciDati(socket, store); }).Start();
        }

        public void gestisciPreamble(Socket socket)
        {
            byte[] ricevuto = null;

            using (Stream stream = new NetworkStream(socket))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                ricevuto = reader.ReadBytes(14);

                string nomeTrasmettitore = "" + Convert.ToChar(ricevuto[0]) + Convert.ToChar(ricevuto[1]) + Convert.ToChar(ricevuto[2]) + Convert.ToChar(ricevuto[3]) + Convert.ToChar(ricevuto[4]) + Convert.ToChar(ricevuto[5]) + Convert.ToChar(ricevuto[6]) + Convert.ToChar(ricevuto[7]) + Convert.ToChar(ricevuto[8]) + Convert.ToChar(ricevuto[9]);
                addText(nomeTrasmettitore);

                int frequenzaEmulazione = BitConverter.ToInt32(ricevuto, 10);
                addText("frequenza di emulazione: " + frequenzaEmulazione);
            }
        }
        #endregion

        #region main
        public void GestisciDati(Socket socket, Store store)
        {
            //Y = valori, X = tempi

            //tempi
            List<float> tempiX0, tempiX1, tempiX2, tempiX3, tempiX4;

            //accellerazioni
            List<float> moduliAccellerazioniY0, accellerazioniSmoothed0;
            List<float> moduliAccellerazioniY1, accellerazioniSmoothed1;
            List<float> moduliAccellerazioniY2, accellerazioniSmoothed2;
            List<float> moduliAccellerazioniY3, accellerazioniSmoothed3;
            List<float> moduliAccellerazioniY4, accellerazioniSmoothed4;
            Finestra finestra0, finestra1, finestra2, finestra3, finestra4;

            //derivate numeriche delle accellerazioni
            List<float> derivataNumerica0, derivataNumerica1, derivataNumerica2, derivataNumerica3, derivataNumerica4;

            //deviazioni standard
            List<float> deviazioneStandard0, deviazioneStandard1, deviazioneStandard2, deviazioneStandard3, deviazioneStandard4;

            //giroscopi
            List<float> moduliGiroscopiY0, giroscopiSmoothed0;
            List<float> moduliGiroscopiY1, giroscopiSmoothed1;
            List<float> moduliGiroscopiY2, giroscopiSmoothed2;
            List<float> moduliGiroscopiY3, giroscopiSmoothed3;
            List<float> moduliGiroscopiY4, giroscopiSmoothed4;

            //vettori con i tetha
            List<float> thetaVettore0, thetaVettore1, thetaVettore2, thetaVettore3, thetaVettore4;

            //vettore con le accellerazioni Y (per analisi dello spostamento)
            List<float> accellerazioniX0 = new List<float>();

            //analizzo la posizione
            LogRaw posizioneAttuale;

            //per salvare il log su un file txt
            Log log = new Log("log.txt");

            try
            {
                while (true)
                {
                    //acquisisco e salvo i dati in arrivo dai sensori
                    store.acquisisciFinestra(socket, secondiFinestra * HertzFinestra);

                    //creo le finestre
                    //accellerazioni
                    finestra0 = store.getSensore(0).getFinestra(secondiFinestra * HertzFinestra / 5);
                    finestra1 = store.getSensore(1).getFinestra(secondiFinestra * HertzFinestra / 5);
                    finestra2 = store.getSensore(2).getFinestra(secondiFinestra * HertzFinestra / 5);
                    finestra3 = store.getSensore(3).getFinestra(secondiFinestra * HertzFinestra / 5);
                    finestra4 = store.getSensore(4).getFinestra(secondiFinestra * HertzFinestra / 5);

                    //tempi
                    tempiX0 = finestra0.alTempo;
                    tempiX1 = finestra1.alTempo;
                    tempiX2 = finestra2.alTempo;
                    tempiX3 = finestra3.alTempo;
                    tempiX4 = finestra4.alTempo;

                    //moduli accellerazioni
                    moduliAccellerazioniY0 = getModuliAccellerazioni(finestra0.records);
                    moduliAccellerazioniY1 = getModuliAccellerazioni(finestra1.records);
                    moduliAccellerazioniY2 = getModuliAccellerazioni(finestra2.records);
                    moduliAccellerazioniY3 = getModuliAccellerazioni(finestra3.records);
                    moduliAccellerazioniY4 = getModuliAccellerazioni(finestra4.records);

                    //moduli giroscopi
                    moduliGiroscopiY0 = getModuliGiroscopi(finestra0.records);
                    moduliGiroscopiY1 = getModuliGiroscopi(finestra1.records);
                    moduliGiroscopiY2 = getModuliGiroscopi(finestra2.records);
                    moduliGiroscopiY3 = getModuliGiroscopi(finestra3.records);
                    moduliGiroscopiY4 = getModuliGiroscopi(finestra4.records);

                    //smoothing accellerazioni
                    accellerazioniSmoothed0 = smoothing(moduliAccellerazioniY0);
                    accellerazioniSmoothed1 = smoothing(moduliAccellerazioniY1);
                    accellerazioniSmoothed2 = smoothing(moduliAccellerazioniY2);
                    accellerazioniSmoothed3 = smoothing(moduliAccellerazioniY3);
                    accellerazioniSmoothed4 = smoothing(moduliAccellerazioniY4);

                    //smoothing giroscopi
                    giroscopiSmoothed0 = smoothing(moduliGiroscopiY0);
                    giroscopiSmoothed1 = smoothing(moduliGiroscopiY1);
                    giroscopiSmoothed2 = smoothing(moduliGiroscopiY2);
                    giroscopiSmoothed3 = smoothing(moduliGiroscopiY3);
                    giroscopiSmoothed4 = smoothing(moduliGiroscopiY4);

                    //disegno moduli accellerazioni smoothed
                    disegna(tempiX0, accellerazioniSmoothed0, zedGraphControl1, 0);
                    disegna(tempiX1, accellerazioniSmoothed1, zedGraphControl1, 1);
                    disegna(tempiX2, accellerazioniSmoothed2, zedGraphControl1, 2);
                    disegna(tempiX3, accellerazioniSmoothed3, zedGraphControl1, 3);
                    disegna(tempiX4, accellerazioniSmoothed4, zedGraphControl1, 4);

                    //disegno moduli giroscopi smoothed
                    disegna(tempiX0, giroscopiSmoothed0, zedGraphControl2, 0);
                    disegna(tempiX1, giroscopiSmoothed1, zedGraphControl2, 1);
                    disegna(tempiX2, giroscopiSmoothed2, zedGraphControl2, 2);
                    disegna(tempiX3, giroscopiSmoothed3, zedGraphControl2, 3);
                    disegna(tempiX4, giroscopiSmoothed4, zedGraphControl2, 4);

                    //calcolo le derivate numeriche
                    derivataNumerica0 = getRapportoIncrementale(accellerazioniSmoothed0);
                    derivataNumerica1 = getRapportoIncrementale(accellerazioniSmoothed1);
                    derivataNumerica2 = getRapportoIncrementale(accellerazioniSmoothed2);
                    derivataNumerica3 = getRapportoIncrementale(accellerazioniSmoothed3);
                    derivataNumerica4 = getRapportoIncrementale(accellerazioniSmoothed4);

                    //calcolo le deviazioni standard
                    deviazioneStandard0 = getDeviazioneStandard(accellerazioniSmoothed0);
                    deviazioneStandard1 = getDeviazioneStandard(accellerazioniSmoothed1);
                    deviazioneStandard2 = getDeviazioneStandard(accellerazioniSmoothed2);
                    deviazioneStandard3 = getDeviazioneStandard(accellerazioniSmoothed3);
                    deviazioneStandard4 = getDeviazioneStandard(accellerazioniSmoothed4);

                    //calcolo gli intervalli stazionari
                    log.addLogRawRange(getIntervalliStazionari(deviazioneStandard0, tempiX0));
                    log.addLogRawRange(getIntervalliStazionari(deviazioneStandard1, tempiX1));
                    log.addLogRawRange(getIntervalliStazionari(deviazioneStandard2, tempiX2));
                    log.addLogRawRange(getIntervalliStazionari(deviazioneStandard3, tempiX3));
                    log.addLogRawRange(getIntervalliStazionari(deviazioneStandard4, tempiX4));

                    //calcolo le theta
                    thetaVettore0 = funzioneOrientamentoNoDiscontinuita(finestra0.records);
                    thetaVettore1 = funzioneOrientamentoConDiscontinuita(finestra1.records);
                    thetaVettore2 = funzioneOrientamentoConDiscontinuita(finestra2.records);
                    thetaVettore3 = funzioneOrientamentoConDiscontinuita(finestra3.records);
                    thetaVettore4 = funzioneOrientamentoConDiscontinuita(finestra4.records);

                    //disegno le theta
                    disegna(tempiX0, thetaVettore0, zedGraphControl3, 0);
                    disegna(tempiX1, thetaVettore1, zedGraphControl3, 1);
                    disegna(tempiX2, thetaVettore2, zedGraphControl3, 2);
                    disegna(tempiX3, thetaVettore3, zedGraphControl3, 3);
                    disegna(tempiX4, thetaVettore4, zedGraphControl3, 4);


                    //riempio il vettore delle accellerazioniX del primo sensore (quello sul bacino)
                    accellerazioniX0.Clear();
                    foreach (Record item in finestra0.records)
                        accellerazioniX0.Add(Math.Abs(item.getAccellerazione().getDato().ElementAt(0))); //le x delle accellerazioni

                    //analizzo le posizioni (lay / stand /sit)
                    posizioneAttuale = analizzaPosizione(accellerazioniX0, tempiX0);
                    log.addLogRaw(posizioneAttuale);
                }
            }
            catch (Exception)
            {
                addText("client disconnesso, salvo permanentemente i dati:");

                log.riorganizza(); //unifica gli intervalli degli stessi eventi che si intersecano fra di loro
                log.print();   //scrivo il log su un txt
                addText("salvataggio log su txt effettuato");

                foglio.write("dati.csv");   //alla disconnessione salvo tutti i dati sul file excell
                addText("salvataggio dati su excell effettuato");
            }
        } 
        #endregion

        #region analizza
        public bool isStazionario(float deviazioneStandard, float valoreMedio) //il valore medio glielo passo tramite deviazioniStandard.Average()
        {
            if (Math.Abs(deviazioneStandard - valoreMedio) <= sogliaMaxStazionamento)
                return true;
            return false;
        }

        public List<LogRaw> getIntervalliStazionari(List<float> deviazioniStandard, List<float> tempi)
        {
            bool precedenteStazionario = true;
            float tempoAlPuntoPrecedente = tempi.ElementAt(0);
            List<LogRaw> intervalliStazionari = new List<LogRaw>();
            for (int i = 0; i < deviazioniStandard.Count; i++)
            {
                if (precedenteStazionario && !isStazionario(deviazioniStandard.ElementAt(i), getMedia(deviazioniStandard, i)))    //se il precedente era stazionario, e questo non lo e'
                {
                    if (tempi.ElementAt(i) - tempoAlPuntoPrecedente > durataMinimaStazionamento) //se lo stazionamento dura abbastanza lo considero
                    {
                        intervalliStazionari.Add(new LogRaw(tempoAlPuntoPrecedente, tempi.ElementAt(i), "stazionario"));
                        addText("stazionario da " + tempoAlPuntoPrecedente + " a " + tempi.ElementAt(i));
                        tempoAlPuntoPrecedente = tempi.ElementAt(i);
                        precedenteStazionario = false;
                    }
                }
                else if (!precedenteStazionario && isStazionario(deviazioniStandard.ElementAt(i), getMedia(deviazioniStandard, i)))  //se il precedente non era stazionario ma questo lo è
                {
                    tempoAlPuntoPrecedente = tempi.ElementAt(i);
                    precedenteStazionario = true;
                }
            }
            return intervalliStazionari;
        }

        public float getMedia(List<float> intervallo, int intorno)
        {
            if (intorno + T > intervallo.Count)
                return intervallo.GetRange(intervallo.Count - T, T).Average();
            return intervallo.GetRange(intorno, T).Average();
        }

        private float utileOrientamento2 = 5000;
        public List<float> funzioneOrientamentoConDiscontinuita(List<Record> finestra)
        {
            List<float> thetaVettore = new List<float>();

            if (utileOrientamento2 == 5000) //se true => la finestra è la prima in assoluto
                utileOrientamento2 = finestra.First().getMagnetometro().getTheta();

            thetaVettore.Add(utileOrientamento);
            for (int i = 1; i < finestra.Count; i++)
            {
                thetaVettore.Add(finestra.ElementAt(i).getMagnetometro().getTheta());
            }
            utileOrientamento2 = thetaVettore.Last();
            return thetaVettore;
        }

        private float utileOrientamento = 5000;
        public List<float> funzioneOrientamentoNoDiscontinuita(List<Record> finestra)
        {
            List<float> thetaVettore = new List<float>();

            if (utileOrientamento == 5000) //se true => la finestra è la prima in assoluto
                utileOrientamento = finestra.First().getMagnetometro().getTheta();

            thetaVettore.Add(utileOrientamento);
            for (int i = 1; i < finestra.Count; i++)
            {
                thetaVettore.Add(smorzaTheta(finestra.ElementAt(i).getMagnetometro().getTheta(), thetaVettore.ElementAt(i - 1)));
            }
            utileOrientamento = thetaVettore.Last();
            return thetaVettore;
        }

        public float smorzaTheta(float theta, float thetaPrecedente)
        {
            float maxScarto = (float)Math.PI / 2;
            bool check = true;

            while (check)
            {
                check = false;

                if (thetaPrecedente - theta > maxScarto)
                {
                    theta += (float)Math.PI;
                    check = true;
                }
                else if (thetaPrecedente - theta < -maxScarto)
                {
                    theta -= (float)Math.PI;
                    check = true;
                }
            }

            return theta;
        }

        public float convertiInGradi(float radianti)
        {
            return (float)(360 / (2 * Math.PI) * radianti);
        }

        public LogRaw analizzaPosizione(List<float> accY, List<float> tempo)
        {
            float mediaIntervallo = accY.Average();

            switch (getPosizione(mediaIntervallo))
            {
                case 0: return new LogRaw(tempo.First(), tempo.Last(), "Lay");
                case 1: return new LogRaw(tempo.First(), tempo.Last(), "LaySit");
                case 2: return new LogRaw(tempo.First(), tempo.Last(), "Sit");
                default: return new LogRaw(tempo.First(), tempo.Last(), "Stand");
            }
        }
        
        public int getPosizione(float accY)
        {
            if (accY <= 2.7)
                return 0; //Lay

            if (accY > 2.7 && accY <= 3.7)
                return 1; //LaySit

            if (accY > 3.7 && accY <= 7)
                return 2;  //Sit

            return 3;  //Stand
        }

        public void analizzaGirata(List<float> theta, List<float> fuIlTempo)
        {
            float variazione = 0;
            float fisso = theta.ElementAt(0);
            float tot = 0.005F;
            float t_iniziale = theta.ElementAt(1);
            float o_iniziale = fuIlTempo.ElementAt(1);
            for (int i = 0; i < theta.Count(); i++)
            {

                t_iniziale = theta.ElementAt(i);
                o_iniziale = fuIlTempo.ElementAt(i);
                if (i > 0)
                {
                    if (theta.ElementAt(i) - theta.ElementAt(i - 1) < 0.05F)
                    {
                        variazione = variazione + (theta.ElementAt(i - 1) - theta.ElementAt(i));
                    }
                }
                if (variazione > 0.15F)
                {
                    addText("si è girato a destra di " + ((variazione * 114.64) + "  gradi"));
                    variazione = 0;
                }

                if (-variazione > 0.15F)
                {
                    addText("si è girato a sinistra di " + ((variazione * 114.64) + "  gradi"));
                    variazione = 0;
                }
            }

        }

        public bool inGirata(float radianti)
        {
            if (convertiInGradi(Math.Abs(radianti)) > sogliaGirata) //considero significative solo le girate > 10 gradi
                return true;
            return false;
        }
        #endregion

        #region utility
        public Form1()
        {
            InitializeComponent();

            avanzamentoTempo = (float)1.0 / HertzFinestra;

            foglio = new SmartXLS.WorkBook();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            zedGraphControl1.GraphPane.AddCurve("Sensore0",
                    new PointPairList(), Color.Red, SymbolType.None);
            zedGraphControl1.GraphPane.AddCurve("Sensore1",
                    new PointPairList(), Color.Black, SymbolType.None);
            zedGraphControl1.GraphPane.AddCurve("Sensore2",
                    new PointPairList(), Color.Aquamarine, SymbolType.None);
            zedGraphControl1.GraphPane.AddCurve("Sensore3",
                    new PointPairList(), Color.Blue, SymbolType.None);
            zedGraphControl1.GraphPane.AddCurve("Sensore4",
                    new PointPairList(), Color.Violet, SymbolType.None);
            zedGraphControl1.GraphPane.Title.Text = "ACCELLEROMETRO";
            zedGraphControl1.GraphPane.XAxis.Title.Text = "TEMPO";
            zedGraphControl1.GraphPane.YAxis.Title.Text = "ACCELLERAZIONE";
            zedGraphControl1.GraphPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);

            zedGraphControl2.GraphPane.AddCurve("Sensore0",
                    new PointPairList(), Color.Red, SymbolType.None);
            zedGraphControl2.GraphPane.AddCurve("Sensore1",
                    new PointPairList(), Color.Black, SymbolType.None);
            zedGraphControl2.GraphPane.AddCurve("Sensore2",
                    new PointPairList(), Color.Aquamarine, SymbolType.None);
            zedGraphControl2.GraphPane.AddCurve("Sensore3",
                    new PointPairList(), Color.Blue, SymbolType.None);
            zedGraphControl2.GraphPane.AddCurve("Sensore4",
                    new PointPairList(), Color.Violet, SymbolType.None);
            zedGraphControl2.GraphPane.Title.Text = "GIROSCOPIO";
            zedGraphControl2.GraphPane.XAxis.Title.Text = "TEMPO";
            zedGraphControl2.GraphPane.YAxis.Title.Text = "ROTAZIONE";
            zedGraphControl2.GraphPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);

            zedGraphControl3.GraphPane.AddCurve("Sensore0",
                    new PointPairList(), Color.Red, SymbolType.None);
            zedGraphControl3.GraphPane.AddCurve("Sensore1",
                    new PointPairList(), Color.Black, SymbolType.None);
            zedGraphControl3.GraphPane.AddCurve("Sensore2",
                    new PointPairList(), Color.Aquamarine, SymbolType.None);
            zedGraphControl3.GraphPane.AddCurve("Sensore3",
                    new PointPairList(), Color.Blue, SymbolType.None);
            zedGraphControl3.GraphPane.AddCurve("Sensore4",
                    new PointPairList(), Color.Violet, SymbolType.None);
            zedGraphControl3.GraphPane.Title.Text = "MAGNETOMETRO";
            zedGraphControl3.GraphPane.XAxis.Title.Text = "TEMPO";
            zedGraphControl3.GraphPane.YAxis.Title.Text = "CAMPO MAGNETICO";
            zedGraphControl3.GraphPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);

            new Thread(delegate() { ConnectionManager(); }).Start();
        }

        private delegate void aggiungiTesto(string txt);
        public void addText(string txt)
        {
            if (InvokeRequired)
            {
                Invoke(new aggiungiTesto(addText), new object[] { txt });
            }
            else
                textBox1.AppendText(txt + " ||| ");
        }

        public List<float> getModuliAccellerazioni(List<Record> finestra)
        {
            List<float> moduliAccellerazioni = new List<float>();
            foreach (Record item in finestra)
            {
                moduliAccellerazioni.Add(item.getAccellerazione().getModulo());
            }
            return moduliAccellerazioni;
        }

        public List<float> smoothing(List<float> X)
        {
            List<float> utile = new List<float>();
            for (int i = 0; i < K; i++)     //gestisco i primi K elementi
            {
                utile.Add(X.GetRange(0, K).Average());
            }

            for (int i = K; i < X.Count - K; i++)
            {
                utile.Add(X.GetRange(i - K, 2 * K).Average());
            }

            for (int i = 0; i < K; i++)     //gestisco gli ultimi K elementi
            {
                utile.Add(X.GetRange(X.Count - K, K).Average());
            }
            return utile;
        }

        public List<float> getModuliGiroscopi(List<Record> finestra)
        {
            List<float> moduliGiroscopi = new List<float>();
            foreach (Record item in finestra)
            {
                moduliGiroscopi.Add(item.getGiroscopio().getModulo());
            }
            return moduliGiroscopi;
        }

        public List<float> getRapportoIncrementale(List<float> X)
        {
            List<float> utile = new List<float>();
            utile.Add(0);
            for (int i = 0; i < X.Count - 1; i++)
            {
                utile.Add(X[i + 1] - X[i]);
            }
            return utile;
        }

        public List<float> getDeviazioneStandard(List<float> X)
        {
            List<float> utile = new List<float>();
            for (int i = 0; i < T; i++)     //gestisco i primi K elementi
            {
                utile.Add(calcolaDeviazioneStandard(X.GetRange(0, T), X.GetRange(0, T).Average()));
            }

            for (int i = T; i < X.Count - T; i++)
            {
                utile.Add(calcolaDeviazioneStandard(X.GetRange(i - T, 2 * T), X.GetRange(i - T, 2 * T).Average()));
            }

            for (int i = 0; i < T; i++)     //gestisco gli ultimi K elementi
            {
                utile.Add(calcolaDeviazioneStandard(X.GetRange(X.Count - T, T), X.GetRange(X.Count - T, T).Average()));
            }
            return utile;
        }

        public float calcolaDeviazioneStandard(List<float> daDevianzionare, float media)
        {
            float ris = 0;

            foreach (float item in daDevianzionare)
            {
                ris += (float)Math.Pow(item - media, 2);
            }
            ris = (float)Math.Sqrt(ris / 2 * T);

            return ris;
        }
        #endregion
    }
}
