using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace Esame8
{
    public class Store
    {
        private List<Sensore> sensori;
        private float _avanzamentoTempo;
        private SmartXLS.WorkBook foglio;
        private int contaRighe;

        public  float avanzamentoTempo
        {
            get { return _avanzamentoTempo; }
        }

        public Store(float avanzamentoTempo, SmartXLS.WorkBook foglio)
        {
            this._avanzamentoTempo = avanzamentoTempo;
            this.foglio = foglio;
            contaRighe = 0;
            sensori = new List<Sensore>();
            sensori.Add(new Sensore(avanzamentoTempo)); //creo i 5 sensori
            sensori.Add(new Sensore(avanzamentoTempo));
            sensori.Add(new Sensore(avanzamentoTempo));
            sensori.Add(new Sensore(avanzamentoTempo));
            sensori.Add(new Sensore(avanzamentoTempo));
        }

        public Sensore getSensore(int indice)
        {
            return this.sensori[indice];
        }

        public void acquisisciFinestra(Socket socket, int numeroRecords)
        {
            byte[] ricevuto = null;
            int contaRecords = 0;

            using (Stream stream = new NetworkStream(socket))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                try
                {
                    while (contaRecords < numeroRecords)
                    {
                        //2 byte comunicazione
                        ricevuto = reader.ReadBytes(2);

                        ricevuto = reader.ReadBytes(1);//LEN
                        int lunghezzaDati = ricevuto[0];
                        if (ricevuto[0] == 255) //se e' presente il campo ext len
                        {
                            ricevuto = reader.ReadBytes(2);//ext len
                            lunghezzaDati = (ricevuto[0] * 256) + ricevuto[1];
                        }

                        //2 byte contatore
                        ricevuto = reader.ReadBytes(2);
                        lunghezzaDati = lunghezzaDati - 2;

                        //52 byte dati(13*4)
                        int numeroSensori = lunghezzaDati / 52;
                        ricevuto = reader.ReadBytes(52 * numeroSensori);
                        List<byte> dati = ricevuto.ToList<byte>();

                        //riempio i sensori con i record
                        sensori[0].addRecord(new Record(dati.GetRange(0, 52)));
                        sensori[1].addRecord(new Record(dati.GetRange(52, 52)));
                        sensori[2].addRecord(new Record(dati.GetRange(104, 52)));
                        sensori[3].addRecord(new Record(dati.GetRange(156, 52)));
                        sensori[4].addRecord(new Record(dati.GetRange(208, 52)));
                        contaRecords += 5;

                        
                        //per salvare su excell
                        sensori[0].getLastRecord().stampa(foglio, contaRighe, 0);
                        sensori[1].getLastRecord().stampa(foglio, contaRighe, 14);
                        sensori[2].getLastRecord().stampa(foglio, contaRighe, 28);
                        sensori[3].getLastRecord().stampa(foglio, contaRighe, 42);
                        sensori[4].getLastRecord().stampa(foglio, contaRighe, 56);
                        contaRighe++;

                        //1 byte crc
                        ricevuto = reader.ReadBytes(1);
                        //stampa(ricevuto);
                    }
                }
                catch (Exception)
                {
                    //client disconnesso
                }
            }
        }
    }
}
